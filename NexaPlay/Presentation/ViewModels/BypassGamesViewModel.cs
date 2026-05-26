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
    private readonly ISteamStoreService _storeService;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
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
    private readonly SemaphoreSlim _enrichLock = new(1, 1);
    private int _gridColumns = 5;

    // ── Category labels (untuk UI) ────────────────────────────────
    public static readonly string[] CategoryIds  = { "all", "ubisoft", "ea", "rockstar", "playstation", "other", "steam-sharing" };
    public static readonly string[] CategoryLabels = { "Semua", "Ubisoft", "EA", "Rockstar", "PlayStation", "Other", "Akun Steam" };

    public BypassGamesViewModel(
        IBypassGamesDataService fixData, IOnlineFixService onlineFix,
        IWindowsDefenderService defender, ISteamService steam,
        IMetadataService metadata, ISteamStoreService storeService,
        INexaPlayOverrideService nexaPlayOverride,
        IAppLogService log)
    {
        _fixData          = fixData;
        _onlineFix        = onlineFix;
        _defender         = defender;
        _steam            = steam;
        _metadata         = metadata;
        _storeService     = storeService;
        _nexaPlayOverride = nexaPlayOverride;
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

        // Enrich covers di background untuk kedua list (agar cache tetap ada saat filter/kategori diubah)
        _ = EnrichCoversInBackgroundAsync(_allFixes);
        _ = EnrichCoversInBackgroundAsync(_steamGames);
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

    // ── Cover enrichment ─────────

    private async Task EnrichCoversInBackgroundAsync(List<FixEntry> sourceList)
    {
        if (sourceList.Count == 0) return;

        try
        {
            using var gate = new SemaphoreSlim(4, 4);
            var tasks = sourceList.ToList().Select(async fix =>
            {
                await gate.WaitAsync();
                try
                {
                    // 1. Ambil data dasar (mengandung HeaderImageUrl, LibraryCapsuleUrl lokal)
                    var meta = await _metadata.GetMetadataAsync(fix.AppId);
                    
                    // 2. Ambil spesifik dari NexaPlay Override
                    var overrideCover = (await _nexaPlayOverride.GetCatalogOverrideAsync(fix.AppId))?.LibraryCapsule;
                    
                    // 3. Ambil dari StoreService (detail Steam API + SGDB)
                    var detail = await _storeService.GetDetailAsync(fix.AppId);
                    var apiCover = detail?.LibraryCapsuleUrl;
                    var sgdbGrid = detail?.SgdbGridUrl;

                    // Tentukan cover terbaik sesuai urutan prioritas yang dibahas
                    var preferredCover = !string.IsNullOrWhiteSpace(fix.PosterUrl) ? fix.PosterUrl
                        : !string.IsNullOrWhiteSpace(overrideCover) ? overrideCover
                        : !string.IsNullOrWhiteSpace(apiCover) ? apiCover
                        : !string.IsNullOrWhiteSpace(sgdbGrid) ? sgdbGrid
                        : !string.IsNullOrWhiteSpace(meta?.LibraryCapsuleUrl) ? meta.LibraryCapsuleUrl
                        : !string.IsNullOrWhiteSpace(meta?.HeaderImageUrl) ? meta.HeaderImageUrl
                        : fix.AppId > 0 ? $"https://steamcdn-a.akamaihd.net/steam/apps/{fix.AppId}/library_600x900_2x.jpg" 
                        : null;

                    if (string.IsNullOrWhiteSpace(preferredCover)) return;
                    if (string.Equals(preferredCover, fix.PosterUrl, StringComparison.OrdinalIgnoreCase)) return;
                    
                    var newFix = new FixEntry
                    {
                        AppId           = fix.AppId,
                        Title           = fix.Title,
                        Publisher       = fix.Publisher,
                        Category        = fix.Category,
                        PosterUrl       = preferredCover,
                        Password        = fix.Password,
                        IsPremium       = fix.IsPremium,
                        AktivasiOffline = fix.AktivasiOffline,
                        ExeHint         = fix.ExeHint,
                        UseShortcut     = fix.UseShortcut,
                        Files           = fix.Files
                    };

                    // Update the source list (cache) so filters don't revert to old cover
                    var sourceIdx = sourceList.IndexOf(fix);
                    if (sourceIdx >= 0)
                    {
                        sourceList[sourceIdx] = newFix;
                    }

                    // PENTING: Update juga ke global singleton _fixData agar saat kembali
                    // dari halaman GameDetail (navigasi balik), gambar tidak ter-reset!
                    _fixData.UpdateCacheItem(newFix);

                    // Update UI if the item is currently visible
                    var uiIdx = DisplayGames.IndexOf(fix);
                    if (uiIdx >= 0)
                    {
                        DisplayGames[uiIdx] = newFix;
                    }
                }
                catch (Exception ex)
                {
                    _log.Log("BypassGames", $"Cover enrich failed appId={fix.AppId}: {ex.Message}");
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);
            _log.Log("BypassGames", "Cover enrichment done for a source list.");
        }
        catch (Exception ex)
        {
            _log.Log("BypassGames", $"EnrichCovers failed: {ex.Message}");
        }
    }

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
