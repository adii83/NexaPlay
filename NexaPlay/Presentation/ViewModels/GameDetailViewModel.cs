using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Presentation.Views.Pages;
using System;
using System.Collections.Generic;
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
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private readonly INavigationService _nav;

    // —— Base metadata (from steam_data.json.gz — always available) ———————————
    [ObservableProperty] public partial GameEntry? Game { get; set; }

    // —— Rich detail (from Steam Store API — loaded on-demand) ————————————————
    [ObservableProperty] public partial GameDetailEntry? Detail { get; set; }
    [ObservableProperty] public partial bool IsDetailLoading { get; set; }
    [ObservableProperty] public partial bool IsDetailAvailable { get; set; }

    // —— Fix catalog entry (from fix_games.json) ——————————————————————————————
    [ObservableProperty] public partial FixEntry? FixEntry { get; set; }
    [ObservableProperty] public partial bool HasFixAvailable { get; set; }

    // —— Applied state ————————————————————————————————————————————————————————
    [ObservableProperty] public partial bool IsFixApplied { get; set; }
    [ObservableProperty] public partial bool IsGameInstalled { get; set; }

    // —— Action states ————————————————————————————————————————————————————————
    [ObservableProperty] public partial bool IsApplyingFix { get; set; }
    [ObservableProperty] public partial bool IsAddingGame { get; set; }
    [ObservableProperty] public partial int ActionPercent { get; set; }
    [ObservableProperty] public partial string ActionStatus { get; set; }
    [ObservableProperty] public partial BypassStatus CurrentBypassStatus { get; set; }

    // —— Selected screenshot index (for the strip) ————————————————————————————
    [ObservableProperty] public partial int SelectedScreenshotIndex { get; set; }
    [ObservableProperty] public partial string CurrentScreenshotUrl { get; set; }
    [ObservableProperty] public partial string HeroBackgroundUrl { get; set; }
    [ObservableProperty] public partial string GameIconUrl { get; set; }
    [ObservableProperty] public partial bool ShowRecommendedRequirements { get; set; }

    // —— About content loading state (set by Page, not ViewModel) ——————————————
    [ObservableProperty] public partial bool IsAboutContentLoading { get; set; }



    public IReadOnlyList<ScreenshotEntry> Screenshots => Detail?.Screenshots ?? Array.Empty<ScreenshotEntry>();
    public IReadOnlyList<MovieEntry> Movies => Detail?.Movies ?? Array.Empty<MovieEntry>();
    public IReadOnlyList<string> Categories => Detail?.Categories ?? Array.Empty<string>();
    public IReadOnlyList<string> GenreTags => BuildGenreTags(Game?.Genre);
    public string DisplayShortDescription => Detail?.ShortDescription ?? Game?.ShortDescription ?? string.Empty;
    public string OverviewScreenshotUrl1 => Detail?.Screenshots?.ElementAtOrDefault(0)?.FullUrl ?? string.Empty;
    public string OverviewScreenshotUrl2 => Detail?.Screenshots?.ElementAtOrDefault(1)?.FullUrl ?? string.Empty;
    public string OverviewScreenshotUrl3 => Detail?.Screenshots?.ElementAtOrDefault(2)?.FullUrl ?? string.Empty;
    public bool HasOverviewScreenshots => !string.IsNullOrEmpty(OverviewScreenshotUrl1);
    public IReadOnlyList<string> PostAboutScreenshotUrls => BuildPostAboutScreenshotUrls();
    public int PostAboutScreenshotCount => PostAboutScreenshotUrls.Count;
    public string PostAboutHeroScreenshotUrl => PostAboutScreenshotUrls.ElementAtOrDefault(0) ?? string.Empty;
    public IReadOnlyList<string> PostAboutTailScreenshotUrls => PostAboutScreenshotUrls.Skip(1).ToArray();
    public bool HasPostAboutScreenshots => !string.IsNullOrWhiteSpace(PostAboutHeroScreenshotUrl);
    public bool HasPostAboutTailScreenshots => PostAboutTailScreenshotUrls.Count > 0;
    public string PostAboutScreenshotUrl1 => PostAboutScreenshotUrls.ElementAtOrDefault(0) ?? string.Empty;
    public string PostAboutScreenshotUrl2 => PostAboutScreenshotUrls.ElementAtOrDefault(1) ?? string.Empty;
    public string PostAboutScreenshotUrl3 => PostAboutScreenshotUrls.ElementAtOrDefault(2) ?? string.Empty;
    public bool HasPostAboutLayoutSingle => PostAboutScreenshotCount == 1;
    public bool HasPostAboutLayoutDouble => PostAboutScreenshotCount == 2;
    public bool HasPostAboutLayoutTriple => PostAboutScreenshotCount >= 3;

    /// <summary>
    /// Konten utama "About the Game" — raw HTML dari Steam API.
    /// Ini yang ditampilkan oleh WebView2. Mayoritas game punya field ini.
    /// </summary>
    public string DisplayAboutTheGame => Detail?.AboutTheGame ?? string.Empty;

    /// <summary>
    /// Konten kaya — DetailedDescription diprioritaskan (biasanya lebih lengkap,
    /// mencakup AboutTheGame + konten ekstra seperti edition info).
    /// Fallback ke AboutTheGame jika DetailedDescription kosong.
    /// Tidak ada filter comparison — ambil konten apa adanya.
    /// </summary>
    public string DisplayRichDescription =>
        !string.IsNullOrWhiteSpace(Detail?.DetailedDescription)
            ? Detail!.DetailedDescription
            : Detail?.AboutTheGame ?? string.Empty;

    /// <summary>
    /// Konten "Detailed Description" — hanya ditampilkan jika berbeda dari AboutTheGame.
    /// Kebanyakan game punya isi identik, jadi ini biasanya kosong.
    /// </summary>
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
    private int _loadVersion;

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
        INexaPlayOverrideService nexaPlayOverride,
        IAppLogService log,
        INavigationService nav)
    {
        _metadata     = metadata;
        _storeService = storeService;
        _onlineFix    = onlineFix;
        _addGame      = addGame;
        _steam        = steam;
        _fixData      = fixData;
        _nexaPlayOverride = nexaPlayOverride;
        _log          = log;
        _nav          = nav;

        // Default values for partial properties (initializers not allowed on partial properties)
        ActionStatus        = string.Empty;
        CurrentBypassStatus = BypassStatus.Unknown;
        CurrentScreenshotUrl = string.Empty;
        HeroBackgroundUrl   = string.Empty;
        GameIconUrl         = string.Empty;
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
        var loadVersion = Interlocked.Increment(ref _loadVersion);
        ct.ThrowIfCancellationRequested();

        // Step 1 — base metadata (O(1) dictionary lookup)
        var baseGame = await _metadata.GetMetadataAsync(appId, ct);
        ct.ThrowIfCancellationRequested();
        if (loadVersion != _loadVersion) return;

        Game = baseGame;
        GameIconUrl = Game?.IconImageUrl
            ?? Game?.HeaderImageUrl
            ?? string.Empty;
        OnPropertyChanged(nameof(GenreTags));
        NotifyDisplayProperties();

        // Step 2 — fix catalog + applied state
        var fixes = await _fixData.GetAllFixesAsync(ct);
        ct.ThrowIfCancellationRequested();
        if (loadVersion != _loadVersion) return;
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
            ct.ThrowIfCancellationRequested();
            if (loadVersion != _loadVersion) return;

            var screenshotCount = fetched?.Screenshots?.Count ?? 0;
            var hasAbout = !string.IsNullOrWhiteSpace(fetched?.AboutTheGame);
            var hasDetailed = !string.IsNullOrWhiteSpace(fetched?.DetailedDescription);
            var hasRaw = !string.IsNullOrWhiteSpace(fetched?.RawMetadataJson);
            _log.Log("GameDetail", $"Detail fetch appId={appId} null={fetched is null} screenshots={screenshotCount} about={hasAbout} detailed={hasDetailed} raw={hasRaw}");

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



            NotifyDisplayProperties();
            var heroOverride = (await _nexaPlayOverride.GetCatalogOverrideAsync(appId))?.LibraryHero2x;
            HeroBackgroundUrl = heroOverride
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "library_hero_2x")
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "library_hero")
                ?? Detail?.SgdbHeroUrl
                ?? Game?.LibraryHero2xUrl
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "background_raw")
                ?? Game?.BackgroundRawImageUrl
                ?? Detail?.BackgroundImageUrl
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "header")
                ?? Game?.HeaderImageUrl
                ?? string.Empty;
            var iconOverride = (await _nexaPlayOverride.GetCatalogOverrideAsync(appId))?.Icon;
            GameIconUrl = iconOverride
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "icon")
                ?? Detail?.SgdbIconUrl
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
        OnPropertyChanged(nameof(PostAboutScreenshotUrls));
        OnPropertyChanged(nameof(PostAboutScreenshotCount));
        OnPropertyChanged(nameof(PostAboutHeroScreenshotUrl));
        OnPropertyChanged(nameof(PostAboutTailScreenshotUrls));
        OnPropertyChanged(nameof(HasPostAboutScreenshots));
        OnPropertyChanged(nameof(HasPostAboutTailScreenshots));
        OnPropertyChanged(nameof(PostAboutScreenshotUrl1));
        OnPropertyChanged(nameof(PostAboutScreenshotUrl2));
        OnPropertyChanged(nameof(PostAboutScreenshotUrl3));
        OnPropertyChanged(nameof(HasPostAboutLayoutSingle));
        OnPropertyChanged(nameof(HasPostAboutLayoutDouble));
        OnPropertyChanged(nameof(HasPostAboutLayoutTriple));
        OnPropertyChanged(nameof(DisplayAboutTheGame));
        OnPropertyChanged(nameof(DisplayRichDescription));
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
            JsonElement node;
            if (doc.RootElement.TryGetProperty("assets", out var assets) &&
                assets.TryGetProperty(assetKey, out var nestedNode))
            {
                node = nestedNode;
            }
            else if (doc.RootElement.TryGetProperty(assetKey, out var rootNode))
            {
                node = rootNode;
            }
            else
            {
                return null;
            }

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

    private IReadOnlyList<string> BuildPostAboutScreenshotUrls()
    {
        if (Detail?.Screenshots is null || Detail.Screenshots.Count == 0)
            return Array.Empty<string>();

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfNotEmpty(excluded, OverviewScreenshotUrl1);
        AddIfNotEmpty(excluded, OverviewScreenshotUrl2);
        AddIfNotEmpty(excluded, OverviewScreenshotUrl3);

        var result = new List<string>(capacity: 5);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var shot in Detail.Screenshots)
        {
            var url = shot.FullUrl;
            if (string.IsNullOrWhiteSpace(url))
                continue;
            if (excluded.Contains(url))
                continue;
            if (!seen.Add(url))
                continue;

            result.Add(url);
            if (result.Count >= 5)
                break;
        }

        return result;
    }

    private static void AddIfNotEmpty(HashSet<string> set, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            set.Add(value);
    }
}
