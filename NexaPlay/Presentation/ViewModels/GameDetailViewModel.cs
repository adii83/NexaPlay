using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Presentation.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

/// <summary>
/// ViewModel for the game detail page.
/// Loads base metadata (GameEntry) from IMetadataService (already in memory),
/// then fetches rich detail (GameDetailEntry) from ISteamStoreService on-demand.
///
/// SOLID — Single Responsibility:
///   Only orchestrates data loading and action commands for the detail page.
///   Does not perform IO directly — delegates to service interfaces.
/// </summary>
public sealed partial class GameDetailViewModel : ObservableObject
{
    private readonly IMetadataService _metadata;
    private readonly ISteamStoreService _storeService;
    private readonly IOnlineFixService _onlineFix;
    private readonly IAddGameService _addGame;
    private readonly ISteamService _steam;
    private readonly IBypassGamesDataService _fixData;
    private readonly IAppLogService _log;
    private readonly INavigationService _nav;

    // —— Base metadata (from steam_data.json.gz — always available) ———————————
    [ObservableProperty] private GameEntry? _game;

    // —— Rich detail (from Steam Store API — loaded on-demand) ————————————————
    [ObservableProperty] private GameDetailEntry? _detail;
    [ObservableProperty] private bool _isDetailLoading;
    [ObservableProperty] private bool _isDetailAvailable;

    // —— Fix catalog entry (from fix_games.json) ——————————————————————————————
    [ObservableProperty] private FixEntry? _fixEntry;
    [ObservableProperty] private bool _hasFixAvailable;

    // —— Applied state ————————————————————————————————————————————————————————
    [ObservableProperty] private bool _isFixApplied;
    [ObservableProperty] private bool _isGameInstalled;

    // —— Action states ————————————————————————————————————————————————————————
    [ObservableProperty] private bool _isApplyingFix;
    [ObservableProperty] private bool _isAddingGame;
    [ObservableProperty] private int _actionPercent;
    [ObservableProperty] private string _actionStatus = string.Empty;
    [ObservableProperty] private BypassStatus _currentBypassStatus = BypassStatus.Unknown;

    // —— Selected screenshot index (for the strip) ————————————————————————————
    [ObservableProperty] private int _selectedScreenshotIndex;
    [ObservableProperty] private string _currentScreenshotUrl = string.Empty;
    [ObservableProperty] private string _heroBackgroundUrl = string.Empty;
    [ObservableProperty] private string _gameIconUrl = string.Empty;
    [ObservableProperty] private bool _showRecommendedRequirements;

    [ObservableProperty] private ObservableCollection<RichBlock> _detailedDescriptionBlocks = new();

    public IReadOnlyList<ScreenshotEntry> Screenshots => Detail?.Screenshots ?? Array.Empty<ScreenshotEntry>();
    public IReadOnlyList<MovieEntry> Movies => Detail?.Movies ?? Array.Empty<MovieEntry>();
    public IReadOnlyList<string> Categories => Detail?.Categories ?? Array.Empty<string>();
    public IReadOnlyList<string> GenreTags => BuildGenreTags(Game?.Genre);
    public string DisplayShortDescription => Detail?.ShortDescription ?? Game?.ShortDescription ?? string.Empty;
    public string OverviewScreenshotUrl1 => Detail?.Screenshots?.ElementAtOrDefault(0)?.FullUrl ?? string.Empty;
    public string OverviewScreenshotUrl2 => Detail?.Screenshots?.ElementAtOrDefault(1)?.FullUrl ?? string.Empty;
    public string OverviewScreenshotUrl3 => Detail?.Screenshots?.ElementAtOrDefault(2)?.FullUrl ?? string.Empty;
    public bool HasOverviewScreenshots => !string.IsNullOrEmpty(OverviewScreenshotUrl1);
    public string DisplayDetailedDescription 
    {
        get 
        {
            var detailed = Detail?.DetailedDescription ?? string.Empty;
            var about = Detail?.AboutTheGame ?? string.Empty;
            if (string.Equals(detailed.Trim(), about.Trim(), StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return detailed;
        }
    }
    public string DisplayDeveloper => Detail?.Developers.Count > 0 ? string.Join(", ", Detail.Developers) : Game?.DeveloperDisplay ?? string.Empty;
    public string DisplayPublisher => Detail?.Publishers.Count > 0 ? string.Join(", ", Detail.Publishers) : Game?.PublisherDisplay ?? string.Empty;
    public string DisplayReleaseDate => Detail?.ReleaseDate ?? Game?.ReleaseDate ?? string.Empty;
    public string DisplayPrice => Game?.PriceNormalized > 0 ? $"Rp {Game.PriceNormalized:N0}" : "Free";
    public string DisplayWebsite => Detail?.Website ?? string.Empty;
    public string DisplaySupport => BuildSupportDisplay(Detail?.SupportUrl, Detail?.SupportEmail);
    public string DisplayLanguages => Detail?.SupportedLanguages ?? string.Empty;
    public string DisplayDrmNotice => Detail?.DrmNotice ?? string.Empty;
    public string DisplayLegalNotice => Detail?.LegalNotice ?? string.Empty;
    public bool IsPremiumGame => Game?.IsPremium == true;
    public bool HasDenuvo => Game?.HasDenuvo == true;
    public string PlanLabel => IsPremiumGame ? "PREMIUM" : "STANDARD";
    public string AddGameButtonText => IsGameInstalled ? "Installed" : IsAddingGame ? "Adding..." : "Add Game";
    public string OnlineFixButtonText => IsApplyingFix ? "Applying..." : "Online-Fix";
    public bool CanAddGame => Game is not null && !IsAddingGame && !IsGameInstalled;
    public bool CanApplyOnlineFix => HasFixAvailable && !IsApplyingFix;
    public bool CanRestartSteam => !IsAddingGame && !IsApplyingFix;

    private CancellationTokenSource? _fixCts;

    partial void OnGameChanged(GameEntry? value) => NotifyStatusProperties();
    partial void OnHasFixAvailableChanged(bool value) => NotifyStatusProperties();
    partial void OnIsFixAppliedChanged(bool value) => NotifyStatusProperties();
    partial void OnIsGameInstalledChanged(bool value) => NotifyStatusProperties();
    partial void OnIsApplyingFixChanged(bool value) => NotifyStatusProperties();
    partial void OnIsAddingGameChanged(bool value) => NotifyStatusProperties();


    partial void OnShowRecommendedRequirementsChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMinimumRequirementsSelected));
        OnPropertyChanged(nameof(IsRecommendedRequirementsSelected));
    }

    public bool IsMinimumRequirementsSelected => !ShowRecommendedRequirements;
    public bool IsRecommendedRequirementsSelected => ShowRecommendedRequirements;

    public GameDetailViewModel(
        IMetadataService metadata,
        ISteamStoreService storeService,
        IOnlineFixService onlineFix,
        IAddGameService addGame,
        ISteamService steam,
        IBypassGamesDataService fixData,
        IAppLogService log,
        INavigationService nav)
    {
        _metadata     = metadata;
        _storeService = storeService;
        _onlineFix    = onlineFix;
        _addGame      = addGame;
        _steam        = steam;
        _fixData      = fixData;
        _log          = log;
        _nav          = nav;
    }

    // —— Load ———————————————————————————————————————————————————————————————————

    /// <summary>
    /// Called by the page when navigated to with an AppId parameter.
    /// Step 1: resolve base metadata (instant — already in memory index).
    /// Step 2: check fix catalog and applied state.
    /// Step 3: fetch rich detail from Steam API (async, shows loading ring).
    /// </summary>
    public async Task LoadAsync(int appId, CancellationToken ct = default)
    {

        // Step 1 — base metadata (O(1) dictionary lookup)
        Game = await _metadata.GetMetadataAsync(appId, ct);
        GameIconUrl = Game?.IconImageUrl
            ?? Game?.HeaderImageUrl
            ?? string.Empty;
        OnPropertyChanged(nameof(GenreTags));
        NotifyDisplayProperties();

        // Step 2 — fix catalog + applied state
        var fixes = await _fixData.GetAllFixesAsync(ct);
        FixEntry = fixes.FirstOrDefault(f => f.AppId == appId);
        HasFixAvailable = FixEntry is not null;
        IsFixApplied    = _onlineFix.IsApplied(appId);
        IsGameInstalled = _addGame.IsGameInstalled(appId.ToString());

        // Step 3 — rich detail (network / disk cache)
        IsDetailLoading   = true;
        IsDetailAvailable = false;
        try
        {
            var fetched = await _storeService.GetDetailAsync(appId, ct);
            CurrentScreenshotUrl = fetched?.Screenshots.FirstOrDefault()?.FullUrl
                ?? Game?.HeaderImageUrl
                ?? string.Empty;
                
            if (fetched?.Screenshots != null && !string.IsNullOrWhiteSpace(CurrentScreenshotUrl))
            {
                foreach (var s in fetched.Screenshots)
                {
                    s.IsSelected = string.Equals(s.FullUrl, CurrentScreenshotUrl, StringComparison.OrdinalIgnoreCase);
                }
            }
            
            Detail = fetched;
            IsDetailAvailable = Detail is not null;

            var blocks = await Task.Run(() => ParseDetailedDescriptionBlocks(Detail?.DetailedDescription));
            DetailedDescriptionBlocks = new ObservableCollection<RichBlock>(blocks);

            NotifyDisplayProperties();
            HeroBackgroundUrl = ReadFirstAssetUrl(Detail?.RawMetadataJson, "library_hero_2x")
                ?? Game?.LibraryHero2xUrl
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "background_raw")
                ?? Game?.BackgroundRawImageUrl
                ?? Detail?.BackgroundImageUrl
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "header")
                ?? Game?.HeaderImageUrl
                ?? string.Empty;
            GameIconUrl = ReadFirstAssetUrl(Detail?.RawMetadataJson, "icon")
                ?? Game?.IconImageUrl
                ?? Game?.HeaderImageUrl
                ?? string.Empty;
        }
        finally
        {
            IsDetailLoading = false;
        }
    }

    // —— Commands ———————————————————————————————————————————————————————————————

    [RelayCommand]
    private async Task ApplyFixAsync()
    {
        if (FixEntry is null || IsApplyingFix) return;
        IsApplyingFix   = true;
        ActionPercent   = 0;
        ActionStatus    = "Checking availability...";
        CurrentBypassStatus = BypassStatus.Downloading;

        _fixCts = new CancellationTokenSource();

        var available = await _onlineFix.CheckAvailabilityAsync(FixEntry.AppId, _fixCts.Token);
        if (!available)
        {
            ActionStatus    = "Fix not available for this game.";
            CurrentBypassStatus = BypassStatus.NotAvailable;
            IsApplyingFix   = false;
            return;
        }

        var progress = new Progress<BypassProgressState>(state =>
        {
            ActionPercent       = state.Percent < 0 ? ActionPercent : state.Percent;
            CurrentBypassStatus = state.Status;
            ActionStatus        = state.Phase switch
            {
                "download" => $"Downloading... {state.Percent}%",
                "extract"  => "Extracting files...",
                "done"     => state.Status == BypassStatus.Applied
                              ? "Fix applied successfully." : state.Error ?? "Failed.",
                _          => state.Message ?? state.Status.ToString()
            };
        });

        await _onlineFix.ApplyAsync(FixEntry.AppId, progress, _fixCts.Token);
        IsFixApplied  = _onlineFix.IsApplied(FixEntry.AppId);
        IsApplyingFix = false;
    }

    [RelayCommand]
    private void CancelFix()
    {
        _fixCts?.Cancel();
        IsApplyingFix       = false;
        ActionStatus        = "Cancelled.";
        CurrentBypassStatus = BypassStatus.Cancelled;
    }

    [RelayCommand]
    private async Task RemoveFixAsync()
    {
        if (FixEntry is null) return;
        await _onlineFix.UnfixAsync(FixEntry.AppId);
        IsFixApplied        = false;
        ActionStatus        = "Fix removed.";
        CurrentBypassStatus = BypassStatus.Unknown;
    }

    [RelayCommand]
    private async Task AddGameAsync()
    {
        if (Game is null || IsAddingGame) return;
        IsAddingGame  = true;
        ActionPercent = 0;
        ActionStatus  = "Starting...";

        var progress = new Progress<BypassProgressState>(state =>
        {
            ActionPercent = state.Percent;
            ActionStatus  = state.Phase switch
            {
                "download" => $"Downloading... {state.Percent}%",
                "validate" => "Validating...",
                "install" or "done" => state.Status == BypassStatus.Applied
                                       ? "Installed." : state.Error ?? "Failed.",
                _ => state.Message ?? state.Status.ToString()
            };
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _addGame.AddGameAsync(Game.AppId.ToString(), progress, cts.Token);
        IsGameInstalled = _addGame.IsGameInstalled(Game.AppId.ToString());
        IsAddingGame    = false;
    }

    [RelayCommand]
    private async Task RemoveGameAsync()
    {
        if (Game is null) return;
        await _addGame.RemoveGameAsync(Game.AppId.ToString());
        IsGameInstalled = false;
        ActionStatus    = "Game script removed.";
    }

    [RelayCommand]
    private async Task RestartSteamAsync()
    {
        ActionStatus = "Restarting Steam...";
        await _steam.RestartAsync();
        ActionStatus = "Steam restarted.";
    }

    [RelayCommand]
    private void SelectMinimumRequirements() => ShowRecommendedRequirements = false;

    [RelayCommand]
    private void SelectRecommendedRequirements() => ShowRecommendedRequirements = true;

    [RelayCommand]
    private void CheckBypass()
    {
        if (Game is not null)
        {
            _nav.Navigate<BypassGamesPage>();
            // If BypassGamesPage handles parameters (like searching for the specific game), we can pass Game.AppId:
            // _nav.Navigate<BypassGamesPage>(Game.AppId);
        }
    }

    public void SelectScreenshot(string? fullUrl)
    {
        if (!string.IsNullOrWhiteSpace(fullUrl) && Detail?.Screenshots != null)
        {
            CurrentScreenshotUrl = fullUrl;
            foreach (var screenshot in Detail.Screenshots)
            {
                screenshot.IsSelected = string.Equals(screenshot.FullUrl, fullUrl, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static IReadOnlyList<string> BuildGenreTags(string? rawGenre)
    {
        if (string.IsNullOrWhiteSpace(rawGenre))
            return Array.Empty<string>();

        return rawGenre
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();
    }

    private void NotifyDisplayProperties()
    {
        OnPropertyChanged(nameof(DisplayShortDescription));
        OnPropertyChanged(nameof(OverviewScreenshotUrl1));
        OnPropertyChanged(nameof(OverviewScreenshotUrl2));
        OnPropertyChanged(nameof(OverviewScreenshotUrl3));
        OnPropertyChanged(nameof(HasOverviewScreenshots));
        OnPropertyChanged(nameof(DisplayDetailedDescription));
        OnPropertyChanged(nameof(DisplayDeveloper));
        OnPropertyChanged(nameof(DisplayPublisher));
        OnPropertyChanged(nameof(DisplayReleaseDate));
        OnPropertyChanged(nameof(DisplayPrice));
        OnPropertyChanged(nameof(DisplayWebsite));
        OnPropertyChanged(nameof(DisplaySupport));
        OnPropertyChanged(nameof(DisplayLanguages));
        OnPropertyChanged(nameof(DisplayDrmNotice));
        OnPropertyChanged(nameof(DisplayLegalNotice));
        NotifyStatusProperties();
    }

    private void NotifyStatusProperties()
    {
        OnPropertyChanged(nameof(IsPremiumGame));
        OnPropertyChanged(nameof(HasDenuvo));
        OnPropertyChanged(nameof(PlanLabel));
        OnPropertyChanged(nameof(AddGameButtonText));
        OnPropertyChanged(nameof(OnlineFixButtonText));
        OnPropertyChanged(nameof(CanAddGame));
        OnPropertyChanged(nameof(CanApplyOnlineFix));
        OnPropertyChanged(nameof(CanRestartSteam));
    }

    private static string BuildSupportDisplay(string? url, string? email)
    {
        var items = new[] { url, email }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToArray();

        return items.Length == 0 ? string.Empty : string.Join("\n", items);
    }

    private static string? ReadFirstAssetUrl(string? rawMetadataJson, string assetKey)
    {
        if (string.IsNullOrWhiteSpace(rawMetadataJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawMetadataJson);
            if (!doc.RootElement.TryGetProperty("assets", out var assets) ||
                !assets.TryGetProperty(assetKey, out var node))
                return null;

            if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("url", out var url) &&
                        url.ValueKind == JsonValueKind.String)
                        return url.GetString();
                }
            }
        }
        catch { }

        return null;
    }

    private static List<RichBlock> ParseDetailedDescriptionBlocks(string? html)
    {
        var blocks = new List<RichBlock>();
        if (string.IsNullOrWhiteSpace(html))
        {
            return blocks;
        }

        var pattern = @"(<img[^>]+src=[""']([^""']+)[""'][^>]*>|<source[^>]+src=[""']([^""']+)[""'][^>]*>|<h[12][^>]*>(.*?)</h[12]>|<div[^>]*class=[""'][^""']*bb_center[^""']*[""'][^>]*>(.*?)</div>)";
        int currentIndex = 0;
        var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        foreach (Match match in matches)
        {
            if (match.Index > currentIndex)
            {
                var textHtml = html.Substring(currentIndex, match.Index - currentIndex);
                var text = CleanHtmlText(textHtml);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    blocks.Add(new RichBlock { Type = RichBlockType.Text, Content = text });
                }
            }
            
            var fullTag = match.Groups[1].Value;
            if (fullTag.StartsWith("<img", StringComparison.OrdinalIgnoreCase))
            {
                blocks.Add(new RichBlock { Type = RichBlockType.Image, Content = match.Groups[2].Value });
            }
            else if (fullTag.StartsWith("<source", StringComparison.OrdinalIgnoreCase))
            {
                var url = match.Groups[3].Value;
                if (url.Contains(".webm", StringComparison.OrdinalIgnoreCase) || url.Contains(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    blocks.Add(new RichBlock { Type = RichBlockType.Video, Content = url });
                }
            }
            else if (fullTag.StartsWith("<h", StringComparison.OrdinalIgnoreCase))
            {
                var headerText = CleanHtmlText(match.Groups[4].Value);
                if (!string.IsNullOrWhiteSpace(headerText) && 
                    !headerText.Equals("About the Game", StringComparison.OrdinalIgnoreCase) &&
                    !headerText.Equals("Detailed Description", StringComparison.OrdinalIgnoreCase))
                {
                    blocks.Add(new RichBlock { Type = RichBlockType.Header, Content = headerText });
                }
            }
            else if (fullTag.StartsWith("<div", StringComparison.OrdinalIgnoreCase))
            {
                var centerText = CleanHtmlText(match.Groups[5].Value);
                if (!string.IsNullOrWhiteSpace(centerText))
                {
                    blocks.Add(new RichBlock { Type = RichBlockType.CenteredText, Content = centerText });
                }
            }
            
            currentIndex = match.Index + match.Length;
        }
        
        if (currentIndex < html.Length)
        {
            var textHtml = html.Substring(currentIndex);
            var text = CleanHtmlText(textHtml);
            if (!string.IsNullOrWhiteSpace(text))
            {
                blocks.Add(new RichBlock { Type = RichBlockType.Text, Content = text });
            }
        }

        var deduplicated = new List<RichBlock>();
        foreach (var b in blocks)
        {
            if (deduplicated.Count > 0)
            {
                var last = deduplicated.Last();
                
                if (last.Type == RichBlockType.Video && b.Type == RichBlockType.Video)
                    continue;

                if (last.Type == RichBlockType.Image && b.Type == RichBlockType.Image && last.Content == b.Content)
                    continue;
            }
            deduplicated.Add(b);
        }
        
        return deduplicated;
    }

    private static string CleanHtmlText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Clean up tabs and excessive spaces first
        input = Regex.Replace(input, @"[ \t]+", " ");

        input = Regex.Replace(input, @"\s*<br\s*/?>\s*", "\n", RegexOptions.IgnoreCase);
        input = Regex.Replace(input, @"\s*</?p[^>]*>\s*", "\n", RegexOptions.IgnoreCase);
        
        input = Regex.Replace(input, @"\s*<li[^>]*>\s*", "\n• ", RegexOptions.IgnoreCase);
        input = Regex.Replace(input, @"\s*</li>\s*", "\n", RegexOptions.IgnoreCase);
        input = Regex.Replace(input, @"\s*</?[uo]l[^>]*>\s*", "\n\n", RegexOptions.IgnoreCase);

        input = Regex.Replace(input, @"<.*?>", string.Empty);
        
        input = System.Net.WebUtility.HtmlDecode(input);

        input = Regex.Replace(input, @"\n{3,}", "\n\n");
        // Also clean up bullet points that got double newlined for some reason
        input = Regex.Replace(input, @"\n+•", "\n•");
        
        return input.Trim();
    }
}
