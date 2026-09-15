using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NexaPlay.Core.Helpers;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class BypassGamesViewModel : ObservableObject
{
    private readonly IBypassGamesDataService _fixData;
    private readonly IOnlineFixService _onlineFix;
    private readonly IWindowsDefenderService _defender;
    private readonly ISteamService _steam;
    private readonly IMetadataService _metadata;
    private readonly IAppLogService _log;

    // ── Catalog state ────────────────────────────────────────────
    [ObservableProperty] public partial ObservableCollection<FixEntry> DisplayGames { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }
    [ObservableProperty] public partial string SearchQuery { get; set; }

    // ── Filter state ──────────────────────────────────────────────
    [ObservableProperty] public partial bool FilterStandard { get; set; }
    [ObservableProperty] public partial bool FilterPremium { get; set; }
    [ObservableProperty] public partial string ActiveCategory { get; set; }  // "all","ubisoft","ea","rockstar","playstation","other","steam-sharing"

    // ── AV scan ───────────────────────────────────────────────────
    [ObservableProperty] public partial IReadOnlyList<AntivirusInfo> AntivirusList { get; set; }
    [ObservableProperty] public partial bool IsScanning { get; set; }

    // ── Fix progress ──────────────────────────────────────────────
    [ObservableProperty] public partial FixEntry? SelectedFix { get; set; }
    [ObservableProperty] public partial bool IsApplying { get; set; }
    [ObservableProperty] public partial int FixPercent { get; set; }
    [ObservableProperty] public partial string FixPhase { get; set; }
    [ObservableProperty] public partial string BypassStatusMessage { get; set; }
    [ObservableProperty] public partial BypassStatus CurrentBypassStatus { get; set; }

    // ── Raw data ──────────────────────────────────────────────────
    private List<FixEntry> _allFixes = new();
    private List<FixEntry> _steamGames = new();
    private CancellationTokenSource? _fixCts;
    private int _gridColumns = 5;

    // ── Category labels (untuk UI) ────────────────────────────────
    public static readonly string[] CategoryIds  = { "all", "ubisoft", "ea", "rockstar", "playstation", "other", "steam-sharing" };
    public static readonly string[] CategoryLabels = { "Semua", "Ubisoft", "EA", "Rockstar", "PlayStation", "Other", "Akun Steam" };

    public BypassGamesViewModel(
        IBypassGamesDataService fixData, IOnlineFixService onlineFix,
        IWindowsDefenderService defender, ISteamService steam,
        IMetadataService metadata,
        IAppLogService log)
    {
        _fixData          = fixData;
        _onlineFix        = onlineFix;
        _defender         = defender;
        _steam            = steam;
        _metadata         = metadata;
        _log              = log;

        DisplayGames        = new ObservableCollection<FixEntry>();
        AntivirusList       = Array.Empty<AntivirusInfo>();
        FixPhase            = string.Empty;
        BypassStatusMessage = string.Empty;
        SearchQuery         = string.Empty;
        ActiveCategory      = "all";
        FilterStandard      = true;
        FilterPremium       = true;
        CurrentBypassStatus = BypassStatus.Unknown;
    }

    // ── Load initial ──────────────────────────────────────────────

    public async Task LoadAsync()
    {
        if (_allFixes.Count > 0) return; // sudah loaded (kembali dari navigasi)

        IsLoading = true;
        IsEmpty   = false;
        try
        {
            _allFixes   = (await _fixData.GetAllFixesAsync()).ToList();
            _steamGames = (await _fixData.GetSteamGamesAsync()).ToList();
            await HydrateMissingListingCoversAsync();
            _log.Log("BypassGames", $"Loaded {_allFixes.Count} fixes + {_steamGames.Count} steam games");
        }
        catch (Exception ex)
        {
            _log.Log("BypassGames", $"Load failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }

        ApplyFilters();
    }

    public void InvalidateData()
    {
        _fixCts?.Cancel();
        _allFixes.Clear();
        _steamGames.Clear();
        DisplayGames.Clear();
        IsEmpty = true;
    }

    public async Task ReloadAsync()
    {
        InvalidateData();
        await LoadAsync();
    }

    // ── Filter ────────────────────────────────────────────────────

    public void SetCategory(string categoryId)
    {
        var changed = !string.Equals(ActiveCategory, categoryId, StringComparison.OrdinalIgnoreCase);
        if (!changed)
            return;

        ActiveCategory = categoryId;
        ApplyFilters();
    }

    public void ApplyFilters()
    {
        IEnumerable<FixEntry> source;

        // Steam Sharing tab → dari steam_games.json (sama seperti GameHub)
        if (ActiveCategory == "steam-sharing")
        {
            source = _steamGames;
        }
        else
        {
            source = _allFixes;
            // 3rd Party area hanya untuk non-steam categories.
            source = source.Where(f => !f.IsSteamType);
            if (ActiveCategory != "all")
            {
                var catEnum = ActiveCategory switch
                {
                    "ubisoft"     => GameCategory.Ubisoft,
                    "ea"          => GameCategory.EA,
                    "rockstar"    => GameCategory.Rockstar,
                    "playstation" => GameCategory.PlayStation,
                    "other"       => GameCategory.Other,
                    _             => GameCategory.Other
                };
                source = source.Where(f => f.Category == catEnum);
            }
        }

        // Filter Standard/Premium
        if (!FilterStandard) source = source.Where(f => f.IsPremium);
        if (!FilterPremium)  source = source.Where(f => !f.IsPremium);

        // Filter search
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.NormalizeForSearch();
            source = source.Where(f =>
                f.Title.NormalizeForSearch().Contains(q, StringComparison.Ordinal) ||
                (f.Publisher ?? "").NormalizeForSearch().Contains(q, StringComparison.Ordinal) ||
                f.AppId.ToString().Contains(q, StringComparison.Ordinal));
        }

        var filtered = source
            .OrderBy(f => System.Text.RegularExpressions.Regex.Replace(f.Title, @"[®™:]", "").Trim(),
                     StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Re-assign list baru agar WinUI me-reset visual tree dan ImageBrush
        // Ini menghindari bug gambar hitam/hilang akibat Clear() dan Add() massal beruntun.
        DisplayGames = new ObservableCollection<FixEntry>(filtered);

        IsEmpty = DisplayGames.Count == 0 && !IsLoading;
    }

    private async Task HydrateMissingListingCoversAsync()
    {
        var missingAppIds = _allFixes
            .Concat(_steamGames)
            .Where(entry => entry.AppId > 0 && string.IsNullOrWhiteSpace(entry.PosterUrl))
            .Select(entry => entry.AppId)
            .Distinct()
            .ToList();
        if (missingAppIds.Count == 0)
            return;

        var metadata = await _metadata.GetMetadataBatchAsync(missingAppIds);
        var covers = metadata.ToDictionary(
            game => game.AppId,
            game => FirstNonEmpty(
                game.LibraryCapsuleUrl,
                game.RawHeaderImageUrl,
                $"https://steamcdn-a.akamaihd.net/steam/apps/{game.AppId}/library_600x900_2x.jpg"));

        Hydrate(_allFixes, covers);
        Hydrate(_steamGames, covers);

        void Hydrate(List<FixEntry> entries, IReadOnlyDictionary<int, string?> coverByAppId)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (!string.IsNullOrWhiteSpace(entry.PosterUrl) ||
                    !coverByAppId.TryGetValue(entry.AppId, out var cover) ||
                    string.IsNullOrWhiteSpace(cover))
                    continue;

                var hydrated = CopyWithPoster(entry, cover);
                entries[index] = hydrated;
                _fixData.UpdateCacheItem(hydrated);
            }
        }
    }

    private static FixEntry CopyWithPoster(FixEntry entry, string poster) => new()
    {
        AppId = entry.AppId,
        Title = entry.Title,
        Publisher = entry.Publisher,
        Category = entry.Category,
        PosterUrl = poster,
        Username = entry.Username,
        Password = entry.Password,
        IsPremium = entry.IsPremium,
        HasDenuvo = entry.HasDenuvo,
        AktivasiOffline = entry.AktivasiOffline,
        DapatkanKode = entry.DapatkanKode,
        ExeHint = entry.ExeHint,
        LaunchOption = entry.LaunchOption,
        UseShortcut = entry.UseShortcut,
        Files = entry.Files
    };

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    // ── Grid columns update ───────────────────────────────────────

    public void UpdateGridColumns(int columns)
    {
        _gridColumns = Math.Clamp(columns, 3, 6);
    }

    // ── Search & filter partial callbacks ────────────────────────

    partial void OnSearchQueryChanged(string value) => ApplyFilters();
    partial void OnFilterStandardChanged(bool value) => ApplyFilters();
    partial void OnFilterPremiumChanged(bool value)  => ApplyFilters();

    // ── AV scan ───────────────────────────────────────────────────

    public async Task ScanSystemAsync()
    {
        IsScanning    = true;
        AntivirusList = await _defender.DetectAntivirusAsync();
        IsScanning    = false;
    }

    // ── Apply fix ─────────────────────────────────────────────────

    public async Task ApplyFixAsync(FixEntry fix)
    {
        if (IsApplying) return;
        SelectedFix         = fix;
        IsApplying          = true;
        FixPercent          = 0;
        CurrentBypassStatus = BypassStatus.Downloading;
        BypassStatusMessage = "Checking availability...";

        _fixCts = new CancellationTokenSource();
        var available = await _onlineFix.CheckAvailabilityAsync(fix.AppId, _fixCts.Token);
        if (!available)
        {
            BypassStatusMessage = "Fix not available for this game";
            CurrentBypassStatus = BypassStatus.NotAvailable;
            IsApplying          = false;
            return;
        }

        var progress = new Progress<BypassProgressState>(state =>
        {
            FixPercent          = state.Percent < 0 ? FixPercent : state.Percent;
            CurrentBypassStatus = state.Status;
            FixPhase            = state.Phase;
            BypassStatusMessage = state.Phase switch
            {
                "download" => $"Downloading... {state.Percent}%",
                "extract"  => "Extracting files...",
                "done"     => state.Status == BypassStatus.Applied ? "Fix applied!" : state.Error ?? "Failed",
                _          => state.Message ?? state.Status.ToString()
            };
        });

        await _onlineFix.ApplyAsync(fix.AppId, progress, _fixCts.Token);
        IsApplying = false;
    }

    public void CancelFix()
    {
        _fixCts?.Cancel();
        IsApplying          = false;
        BypassStatusMessage = "Cancelled";
        CurrentBypassStatus = BypassStatus.Cancelled;
    }

    public async Task UnfixAsync(int appId)
    {
        await _onlineFix.UnfixAsync(appId);
        BypassStatusMessage = "Fix removed";
        CurrentBypassStatus = BypassStatus.Unknown;
    }

    public bool IsFixApplied(int appId) => _onlineFix.IsApplied(appId);
}
