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
    private readonly ILicenseService _licenseService;
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
    [ObservableProperty] public partial string RemoveBlockedDialogMessage { get; set; }
    [ObservableProperty] public partial int RemoveBlockedDialogRequestToken { get; set; }
    [ObservableProperty] public partial bool IsAddGameDialogOpen { get; set; }
    [ObservableProperty] public partial string AddGameDialogTitle { get; set; }
    [ObservableProperty] public partial string AddGameDialogStatus { get; set; }
    [ObservableProperty] public partial int AddGameDialogPercent { get; set; }
    [ObservableProperty] public partial bool ShowAddGameDialogProgress { get; set; }
    [ObservableProperty] public partial bool CanCloseAddGameDialog { get; set; }
    [ObservableProperty] public partial bool IsAddGameCancelRequested { get; set; }
    [ObservableProperty] public partial bool IsRemoveBlockedDialogOpen { get; set; }
    [ObservableProperty] public partial bool IsRemoveGameConfirmDialogOpen { get; set; }
    [ObservableProperty] public partial string RemoveGameConfirmMessage { get; set; }
    [ObservableProperty] public partial string UiInfoDialogTitle { get; set; }
    [ObservableProperty] public partial string UiInfoDialogMessage { get; set; }
    [ObservableProperty] public partial bool IsUiInfoDialogOpen { get; set; }
    [ObservableProperty] public partial bool IsOnlineFixDialogOpen { get; set; }
    [ObservableProperty] public partial string OnlineFixDialogTitle { get; set; }
    [ObservableProperty] public partial string OnlineFixDialogMessage { get; set; }
    [ObservableProperty] public partial int OnlineFixDialogPercent { get; set; }
    [ObservableProperty] public partial bool ShowOnlineFixDialogProgress { get; set; }
    [ObservableProperty] public partial bool ShowOnlineFixDialogSecondaryButton { get; set; }
    [ObservableProperty] public partial string OnlineFixDialogPrimaryText { get; set; }
    [ObservableProperty] public partial string OnlineFixDialogSecondaryText { get; set; }
    [ObservableProperty] public partial bool IsOnlineFixDialogPrimaryEnabled { get; set; }
    [ObservableProperty] public partial bool IsOnlineFixDialogSecondaryEnabled { get; set; }

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
    public string AddGameButtonText => IsGameInstalled ? "Remove Game" : IsAddingGame ? "Adding..." : "Add Game";
    public string OnlineFixButtonText => IsFixApplied ? "Remove Online-Fix" : (IsApplyingFix ? "Memproses..." : "Online-Fix");
    public bool CanAddGame => Game is not null && !IsAddingGame;
    public bool CanApplyOnlineFix => Game is not null && !IsApplyingFix;
    public bool CanRestartSteam => !IsAddingGame && !IsApplyingFix;
    public string AddGameDialogPercentText => $"{Math.Max(0, AddGameDialogPercent)}%";
    public string AddGameDialogActionText => CanCloseAddGameDialog ? "Mengerti" : "Batal";
    public string OnlineFixDialogPercentText => $"{Math.Max(0, OnlineFixDialogPercent)}%";
    public Func<string, Task>? ShowLicenseGateDialogAsync { get; set; }

    private CancellationTokenSource? _fixCts;
    private CancellationTokenSource? _addGameCts;
    private OnlineFixDialogMode _onlineFixDialogMode = OnlineFixDialogMode.Idle;
    private int _loadVersion;

    partial void OnGameChanged(GameEntry? value) => NotifyStatusProperties();
    partial void OnHasFixAvailableChanged(bool value) => NotifyStatusProperties();
    partial void OnIsFixAppliedChanged(bool value) => NotifyStatusProperties();
    partial void OnIsGameInstalledChanged(bool value) => NotifyStatusProperties();
    partial void OnIsApplyingFixChanged(bool value) => NotifyStatusProperties();
    partial void OnIsAddingGameChanged(bool value) => NotifyStatusProperties();
    partial void OnAddGameDialogPercentChanged(int value) => OnPropertyChanged(nameof(AddGameDialogPercentText));
    partial void OnCanCloseAddGameDialogChanged(bool value) => OnPropertyChanged(nameof(AddGameDialogActionText));
    partial void OnOnlineFixDialogPercentChanged(int value) => OnPropertyChanged(nameof(OnlineFixDialogPercentText));


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
        ILicenseService licenseService,
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
        _licenseService = licenseService;
        _nexaPlayOverride = nexaPlayOverride;
        _log          = log;
        _nav          = nav;

        // Default values for partial properties (initializers not allowed on partial properties)
        ActionStatus        = string.Empty;
        CurrentBypassStatus = BypassStatus.Unknown;
        CurrentScreenshotUrl = string.Empty;
        HeroBackgroundUrl   = string.Empty;
        GameIconUrl         = string.Empty;
        RemoveBlockedDialogMessage = string.Empty;
        RemoveGameConfirmMessage = string.Empty;
        AddGameDialogTitle = "Mengunduh & Memasang";
        AddGameDialogStatus = string.Empty;
        ShowAddGameDialogProgress = true;
        UiInfoDialogTitle = string.Empty;
        UiInfoDialogMessage = string.Empty;
        OnlineFixDialogTitle = string.Empty;
        OnlineFixDialogMessage = string.Empty;
        OnlineFixDialogPrimaryText = "Mengerti";
        OnlineFixDialogSecondaryText = "Kembali";
        IsAddGameCancelRequested = false;
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
        if (Game is null || IsApplyingFix) return;
        if (IsFixApplied)
        {
            await RemoveFixAsync();
            return;
        }
        var targetAppId = Game.AppId;
        IsApplyingFix = true;
        ActionPercent = 0;
        ActionStatus = "Memeriksa ketersediaan Online-Fix...";
        CurrentBypassStatus = BypassStatus.Pending;
        _fixCts?.Dispose();
        _fixCts = new CancellationTokenSource();

        try
        {
            SetOnlineFixDialogChecking(targetAppId);
            var available = await _onlineFix.CheckAvailabilityAsync(targetAppId, _fixCts.Token);

            if (_fixCts.IsCancellationRequested)
            {
                SetOnlineFixDialogCancelled();
                IsApplyingFix = false;
                return;
            }

            if (!available)
            {
                SetOnlineFixDialogUnavailable();
                ActionStatus = "Online-Fix belum tersedia.";
                CurrentBypassStatus = BypassStatus.NotAvailable;
                IsApplyingFix = false;
                return;
            }

            SetOnlineFixDialogConfirm(targetAppId);
        }
        catch (OperationCanceledException)
        {
            SetOnlineFixDialogCancelled();
            ActionStatus = "Pemeriksaan Online-Fix dibatalkan.";
            CurrentBypassStatus = BypassStatus.Cancelled;
            IsApplyingFix = false;
            return;
        }
        catch (Exception ex)
        {
            SetOnlineFixDialogFailed("Gagal memeriksa ketersediaan Online-Fix. Silakan coba lagi.");
            ActionStatus = $"Cek Online-Fix gagal: {ex.Message}";
            CurrentBypassStatus = BypassStatus.Failed;
            IsApplyingFix = false;
            return;
        }
    }

    [RelayCommand]
    private void CancelFix()
    {
        _fixCts?.Cancel();
        SetOnlineFixDialogCancelled();
        IsApplyingFix = false;
        ActionStatus = "Proses Online-Fix dibatalkan.";
        CurrentBypassStatus = BypassStatus.Cancelled;
    }

    [RelayCommand]
    private async Task RemoveFixAsync()
    {
        if (Game is null || IsApplyingFix) return;
        var targetAppId = Game.AppId;

        IsApplyingFix = true;
        CurrentBypassStatus = BypassStatus.Applying;
        ActionStatus = "Memproses penghapusan Online-Fix...";
        SetOnlineFixDialogUnfixing();

        try
        {
            await _onlineFix.UnfixAsync(targetAppId);
            IsFixApplied = _onlineFix.IsApplied(targetAppId);

            if (!IsFixApplied)
            {
                SetOnlineFixDialogUnfixDone();
                ActionStatus = "Online-Fix berhasil dihapus.";
                CurrentBypassStatus = BypassStatus.Unknown;
            }
            else
            {
                SetOnlineFixDialogUnfixFailed("Online-Fix belum berhasil dihapus. Silakan coba lagi.");
                ActionStatus = "Online-Fix belum berhasil dihapus.";
                CurrentBypassStatus = BypassStatus.Failed;
            }
        }
        catch (OperationCanceledException)
        {
            SetOnlineFixDialogCancelled();
            ActionStatus = "Penghapusan Online-Fix dibatalkan.";
            CurrentBypassStatus = BypassStatus.Cancelled;
        }
        catch (Exception ex)
        {
            SetOnlineFixDialogUnfixFailed("Terjadi kendala saat menghapus Online-Fix. Silakan coba lagi.");
            ActionStatus = $"Gagal unfix: {ex.Message}";
            CurrentBypassStatus = BypassStatus.Failed;
        }
        finally
        {
            IsApplyingFix = false;
        }
    }

    [RelayCommand]
    private async Task AddGameAsync()
    {
        if (Game is null || IsAddingGame) return;
        if (!await EnsurePremiumAccessAsync(Game.IsPremium))
            return;

        if (IsGameInstalled)
        {
            OpenRemoveGameConfirmDialog();
            return;
        }

        if (Game.HasDenuvo)
        {
            var steamGames = await _fixData.GetSteamGamesAsync(CancellationToken.None);
            var hasSteamBypass = steamGames.Any(f => f.AppId == Game.AppId);
            if (!HasFixAvailable && !hasSteamBypass)
            {
                UiInfoDialogTitle = "Game Belum Dapat Dimainkan";
                UiInfoDialogMessage = "Game ini memakai proteksi Denuvo dan saat ini belum tersedia di daftar Bypass Games kami. Pantau terus pembaruan pada halaman Bypass Games.";
                IsUiInfoDialogOpen = true;
                ActionStatus = "Belum tersedia di Bypass Games.";
                return;
            }
        }

        IsAddingGame  = true;
        ActionPercent = 0;
        ActionStatus  = "Starting...";
        AddGameDialogTitle = "Mengunduh & Memasang";
        AddGameDialogStatus = "Menyiapkan proses...";
        AddGameDialogPercent = 0;
        ShowAddGameDialogProgress = true;
        CanCloseAddGameDialog = false;
        IsAddGameCancelRequested = false;
        IsAddGameDialogOpen = true;
        _addGameCts?.Dispose();
        _addGameCts = new CancellationTokenSource();
        var addGameUnavailableFromApi = false;

        var progress = new Progress<BypassProgressState>(state =>
        {
            ActionPercent = state.Percent;
            AddGameDialogPercent = state.Percent < 0 ? AddGameDialogPercent : state.Percent;
            if (state.Status == BypassStatus.Failed)
            {
                var err = state.Error ?? state.Message ?? string.Empty;
                if (err.Contains("tidak tersedia", StringComparison.OrdinalIgnoreCase) ||
                    err.Contains("semua api gagal", StringComparison.OrdinalIgnoreCase))
                {
                    addGameUnavailableFromApi = true;
                }
            }
            ActionStatus  = state.Phase switch
            {
                "download" when state.Status == BypassStatus.Failed => state.Error ?? "Game belum tersedia.",
                "download" => $"Downloading... {state.Percent}%",
                "validate" when state.Status == BypassStatus.Failed => state.Error ?? "Validasi gagal.",
                "validate" => "Validating...",
                "install" or "done" => state.Status == BypassStatus.Applied
                                       ? "Installed." : state.Error ?? "Failed.",
                _ => state.Message ?? state.Status.ToString()
            };

            AddGameDialogStatus = state.Phase switch
            {
                "download" when state.Status == BypassStatus.Failed =>
                    "Game ini belum tersedia di sumber unduhan kami saat ini.",
                "download" => state.Percent >= 0 ? $"Sedang mengunduh... {state.Percent}%" : "Sedang mengunduh...",
                "validate" => "Memverifikasi file unduhan...",
                "install" => "Memasang file game ke Steam...",
                "done" => state.Status == BypassStatus.Applied ? "Pemasangan selesai." : (state.Error ?? "Proses selesai."),
                "cancel" => "Proses dibatalkan.",
                _ => state.Message ?? "Memproses..."
            };

            ShowAddGameDialogProgress = !(state.Phase == "download" && state.Status == BypassStatus.Failed);
            if (!ShowAddGameDialogProgress && AddGameDialogStatus.Contains("belum tersedia", StringComparison.OrdinalIgnoreCase))
            {
                AddGameDialogTitle = "Game Belum Tersedia";
                AddGameDialogStatus = "Game ini belum tersedia. Silakan request ke admin melalui WhatsApp atau forum Discord yang sudah disediakan.";
            }
        });

        try
        {
            await _addGame.AddGameAsync(Game.AppId.ToString(), progress, _addGameCts.Token);
            IsGameInstalled = _addGame.IsGameInstalled(Game.AppId.ToString());
            CanCloseAddGameDialog = true;
            if (IsAddGameCancelRequested || (_addGameCts?.IsCancellationRequested ?? false))
            {
                AddGameDialogStatus = "Proses dibatalkan. Game tidak jadi ditambahkan.";
                AddGameDialogPercent = Math.Max(0, AddGameDialogPercent);
                ActionStatus = "Proses add game dibatalkan.";
            }
            else if (addGameUnavailableFromApi)
            {
                AddGameDialogTitle = "Game Belum Tersedia";
                AddGameDialogStatus = "Game ini belum tersedia di sumber unduhan kami saat ini.";
                ShowAddGameDialogProgress = false;
                UiInfoDialogTitle = "Game belum tersedia.";
                UiInfoDialogMessage = "Game ini belum tersedia. Silakan request ke admin melalui WhatsApp atau forum Discord yang sudah disediakan.";
                IsAddGameDialogOpen = false;
                IsUiInfoDialogOpen = true;
                ActionStatus = "Game belum tersedia di semua sumber API.";
            }
            else if (IsGameInstalled)
            {
                AddGameDialogPercent = 100;
                AddGameDialogStatus = "Berhasil dipasang. Anda bisa menutup dialog ini.";
            }
            else if (string.IsNullOrWhiteSpace(AddGameDialogStatus) || AddGameDialogStatus.Contains("Memasang", StringComparison.OrdinalIgnoreCase))
            {
                AddGameDialogStatus = "Proses belum berhasil. Silakan cek koneksi atau coba lagi.";
            }

            if (AddGameDialogStatus.Contains("belum tersedia", StringComparison.OrdinalIgnoreCase))
            {
                AddGameDialogTitle = "Game Belum Tersedia";
                AddGameDialogStatus = "Game ini belum tersedia. Silakan request ke admin melalui WhatsApp atau forum Discord yang sudah disediakan.";
                ShowAddGameDialogProgress = false;
            }
        }
        finally
        {
            _addGameCts?.Dispose();
            _addGameCts = null;
            IsAddingGame = false;
        }
    }

    [RelayCommand]
    private async Task RemoveGameAsync()
    {
        if (Game is null) return;
        var result = await _addGame.RemoveGameAsync(Game.AppId.ToString());
        if (result.Success)
        {
            IsGameInstalled = false;
            ActionStatus = "Game script removed.";
            return;
        }

        if (result.BlockedByInstalledGame)
        {
            RemoveBlockedDialogMessage = result.Error ?? "Game masih terinstall.";
            RemoveBlockedDialogRequestToken++;
            IsRemoveBlockedDialogOpen = true;
            ActionStatus = "Remove diblokir: game masih terinstall.";
            return;
        }

        ActionStatus = result.Error ?? "Gagal remove game script.";
    }

    public void CloseAddGameDialog()
    {
        IsAddGameDialogOpen = false;
    }

    public void HandleAddGameDialogAction()
    {
        if (CanCloseAddGameDialog)
        {
            CloseAddGameDialog();
            return;
        }

        if (Game is null || IsAddGameCancelRequested)
            return;

        IsAddGameCancelRequested = true;
        AddGameDialogStatus = "Membatalkan proses...";
        _addGameCts?.Cancel();
        _addGame.CancelAdd(Game.AppId.ToString());
    }

    public void CloseRemoveBlockedDialog()
    {
        IsRemoveBlockedDialogOpen = false;
    }

    public void OpenRemoveGameConfirmDialog()
    {
        if (Game is null) return;
        RemoveGameConfirmMessage = $"Hapus \"{Game.Name}\" dari Steam?";
        IsRemoveGameConfirmDialogOpen = true;
    }

    public void CloseRemoveGameConfirmDialog()
    {
        IsRemoveGameConfirmDialogOpen = false;
    }

    public async Task ConfirmRemoveGameAsync()
    {
        IsRemoveGameConfirmDialogOpen = false;
        await RemoveGameAsync();
    }

    public void CloseUiInfoDialog()
    {
        IsUiInfoDialogOpen = false;
    }

    public async Task HandleOnlineFixPrimaryActionAsync()
    {
        if (Game is null)
            return;
        var targetAppId = Game.AppId;

        if (_onlineFixDialogMode != OnlineFixDialogMode.ConfirmAvailable)
        {
            IsOnlineFixDialogOpen = false;
            return;
        }

        SetOnlineFixDialogApplying();
        ActionStatus = "Memulai penerapan Online-Fix...";
        CurrentBypassStatus = BypassStatus.Downloading;
        OnlineFixDialogPercent = 0;

        var progress = new Progress<BypassProgressState>(state =>
        {
            CurrentBypassStatus = state.Status;
            ActionPercent = state.Percent < 0 ? ActionPercent : state.Percent;
            OnlineFixDialogPercent = state.Percent < 0 ? OnlineFixDialogPercent : state.Percent;

            if (state.Status == BypassStatus.Failed)
            {
                var failedMessage = state.Error ?? state.Message ?? "Online-Fix gagal diterapkan.";
                ActionStatus = failedMessage;
                OnlineFixDialogMessage = failedMessage;
                return;
            }

            ActionStatus = state.Phase switch
            {
                "download" => state.Percent >= 0 ? $"Mengunduh Online-Fix... {state.Percent}%" : "Mengunduh Online-Fix...",
                "extract" => "Mengekstrak Online-Fix...",
                "done" => state.Status == BypassStatus.Applied ? "Online-Fix berhasil diterapkan." : (state.Error ?? "Online-Fix gagal diterapkan."),
                _ => state.Message ?? state.Status.ToString()
            };

            OnlineFixDialogMessage = state.Phase switch
            {
                "download" => state.Percent >= 0 ? $"Sedang mengunduh file Online-Fix... {state.Percent}%" : "Sedang mengunduh file Online-Fix...",
                "extract" => "Mengekstrak file ke folder game...",
                "done" => state.Status == BypassStatus.Applied ? "Online-Fix berhasil dipasang." : (state.Error ?? "Online-Fix gagal dipasang."),
                "cancel" => "Proses Online-Fix dibatalkan.",
                _ => state.Message ?? "Memproses Online-Fix..."
            };
        });

        try
        {
            await _onlineFix.ApplyAsync(targetAppId, progress, _fixCts?.Token ?? CancellationToken.None);
            IsFixApplied = _onlineFix.IsApplied(targetAppId);

            if (_fixCts?.IsCancellationRequested == true || CurrentBypassStatus == BypassStatus.Cancelled)
            {
                SetOnlineFixDialogCancelled();
            }
            else if (IsFixApplied)
            {
                SetOnlineFixDialogDone();
                ActionStatus = "Online-Fix berhasil diterapkan.";
            }
            else if (CurrentBypassStatus == BypassStatus.Failed)
            {
                var errorText = OnlineFixDialogMessage;
                if (errorText.Contains("not installed", StringComparison.OrdinalIgnoreCase) ||
                    errorText.Contains("path not found", StringComparison.OrdinalIgnoreCase) ||
                    errorText.Contains("belum terinstall", StringComparison.OrdinalIgnoreCase) ||
                    errorText.Contains("tidak ditemukan", StringComparison.OrdinalIgnoreCase))
                {
                    SetOnlineFixDialogGameNotInstalled();
                }
                else if (errorText.Contains("not available", StringComparison.OrdinalIgnoreCase))
                {
                    SetOnlineFixDialogUnavailable();
                }
                else
                {
                    SetOnlineFixDialogFailed(errorText);
                }
            }
            else
            {
                SetOnlineFixDialogFailed("Online-Fix gagal diterapkan.");
            }
        }
        finally
        {
            IsApplyingFix = false;
        }
    }

    public void HandleOnlineFixSecondaryAction()
    {
        if (_onlineFixDialogMode == OnlineFixDialogMode.Applying || _onlineFixDialogMode == OnlineFixDialogMode.Checking)
        {
            _fixCts?.Cancel();
            SetOnlineFixDialogCancelled();
            IsApplyingFix = false;
            CurrentBypassStatus = BypassStatus.Cancelled;
            ActionStatus = "Proses Online-Fix dibatalkan.";
            return;
        }

        IsOnlineFixDialogOpen = false;
        if (_onlineFixDialogMode == OnlineFixDialogMode.ConfirmAvailable)
        {
            IsApplyingFix = false;
            CurrentBypassStatus = BypassStatus.Unknown;
            ActionStatus = "Penerapan Online-Fix dibatalkan.";
        }
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

    private async Task<bool> EnsurePremiumAccessAsync(bool requiresPremium)
    {
        if (!requiresPremium)
            return true;

        try
        {
            var license = await _licenseService.LoadAsync();
            if (!license.IsValid)
            {
                if (ShowLicenseGateDialogAsync is not null)
                    await ShowLicenseGateDialogAsync("license-invalid");
                return false;
            }

            if (!license.IsPremium)
            {
                if (ShowLicenseGateDialogAsync is not null)
                    await ShowLicenseGateDialogAsync("premium-required");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _log.Log("LicenseGate", $"GameDetail premium gate failed appid={Game?.AppId} err={ex.Message}");
            if (ShowLicenseGateDialogAsync is not null)
                await ShowLicenseGateDialogAsync("verification-failed");
            return false;
        }
    }

    private void SetOnlineFixDialogChecking(int appId)
    {
        _onlineFixDialogMode = OnlineFixDialogMode.Checking;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = true;
        ShowOnlineFixDialogSecondaryButton = true;
        IsOnlineFixDialogPrimaryEnabled = false;
        IsOnlineFixDialogSecondaryEnabled = true;
        OnlineFixDialogTitle = "Memeriksa Ketersediaan";
        OnlineFixDialogMessage = $"Sedang memeriksa Online-Fix untuk App ID {appId}...";
        OnlineFixDialogPrimaryText = "Terapkan Online-Fix";
        OnlineFixDialogSecondaryText = "Kembali";
        OnlineFixDialogPercent = 0;
    }

    private void SetOnlineFixDialogConfirm(int appId)
    {
        _onlineFixDialogMode = OnlineFixDialogMode.ConfirmAvailable;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = false;
        ShowOnlineFixDialogSecondaryButton = true;
        IsOnlineFixDialogPrimaryEnabled = true;
        IsOnlineFixDialogSecondaryEnabled = true;
        OnlineFixDialogTitle = "Online-Fix Tersedia";
        OnlineFixDialogMessage = $"Online-Fix untuk App ID {appId} tersedia. Ingin menerapkan sekarang?";
        OnlineFixDialogPrimaryText = "Terapkan Online-Fix";
        OnlineFixDialogSecondaryText = "Kembali";
    }

    private void SetOnlineFixDialogApplying()
    {
        _onlineFixDialogMode = OnlineFixDialogMode.Applying;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = true;
        ShowOnlineFixDialogSecondaryButton = true;
        IsOnlineFixDialogPrimaryEnabled = false;
        IsOnlineFixDialogSecondaryEnabled = true;
        OnlineFixDialogTitle = "Menerapkan Online-Fix";
        OnlineFixDialogMessage = "Menyiapkan proses...";
        OnlineFixDialogSecondaryText = "Batal";
    }

    private void SetOnlineFixDialogUnavailable()
    {
        _onlineFixDialogMode = OnlineFixDialogMode.Unavailable;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = false;
        ShowOnlineFixDialogSecondaryButton = false;
        IsOnlineFixDialogPrimaryEnabled = true;
        IsOnlineFixDialogSecondaryEnabled = false;
        OnlineFixDialogTitle = "Online-Fix";
        OnlineFixDialogMessage = "Online-Fix pada game ini belum tersedia.";
        OnlineFixDialogPrimaryText = "Mengerti";
    }

    private void SetOnlineFixDialogGameNotInstalled()
    {
        _onlineFixDialogMode = OnlineFixDialogMode.GameNotInstalled;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = false;
        ShowOnlineFixDialogSecondaryButton = false;
        IsOnlineFixDialogPrimaryEnabled = true;
        IsOnlineFixDialogSecondaryEnabled = false;
        OnlineFixDialogTitle = "Game Tidak Terpasang";
        OnlineFixDialogMessage = "Game belum Anda install di Steam. Install dulu game aslinya, lalu coba Online-Fix lagi.";
        OnlineFixDialogPrimaryText = "Mengerti";
    }

    private void SetOnlineFixDialogDone()
    {
        _onlineFixDialogMode = OnlineFixDialogMode.Done;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = true;
        ShowOnlineFixDialogSecondaryButton = false;
        IsOnlineFixDialogPrimaryEnabled = true;
        IsOnlineFixDialogSecondaryEnabled = false;
        OnlineFixDialogTitle = "Online-Fix Berhasil";
        OnlineFixDialogMessage = "Online-Fix berhasil diterapkan.";
        OnlineFixDialogPrimaryText = "Mengerti";
        OnlineFixDialogPercent = 100;
    }

    private void SetOnlineFixDialogUnfixing()
    {
        _onlineFixDialogMode = OnlineFixDialogMode.Unfixing;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = false;
        ShowOnlineFixDialogSecondaryButton = false;
        IsOnlineFixDialogPrimaryEnabled = false;
        IsOnlineFixDialogSecondaryEnabled = false;
        OnlineFixDialogTitle = "Menghapus Online-Fix";
        OnlineFixDialogMessage = "Sedang menghapus file Online-Fix dari folder game...";
        OnlineFixDialogPrimaryText = "Mengerti";
    }

    private void SetOnlineFixDialogUnfixDone()
    {
        _onlineFixDialogMode = OnlineFixDialogMode.UnfixDone;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = false;
        ShowOnlineFixDialogSecondaryButton = false;
        IsOnlineFixDialogPrimaryEnabled = true;
        IsOnlineFixDialogSecondaryEnabled = false;
        OnlineFixDialogTitle = "Remove Online-Fix Berhasil";
        OnlineFixDialogMessage = "File Online-Fix berhasil dihapus.";
        OnlineFixDialogPrimaryText = "Mengerti";
    }

    private void SetOnlineFixDialogUnfixFailed(string? message)
    {
        _onlineFixDialogMode = OnlineFixDialogMode.UnfixFailed;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = false;
        ShowOnlineFixDialogSecondaryButton = false;
        IsOnlineFixDialogPrimaryEnabled = true;
        IsOnlineFixDialogSecondaryEnabled = false;
        OnlineFixDialogTitle = "Remove Online-Fix Gagal";
        OnlineFixDialogMessage = string.IsNullOrWhiteSpace(message)
            ? "Terjadi kendala saat menghapus Online-Fix. Silakan coba lagi."
            : message;
        OnlineFixDialogPrimaryText = "Mengerti";
    }

    private void SetOnlineFixDialogCancelled()
    {
        _onlineFixDialogMode = OnlineFixDialogMode.Cancelled;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = false;
        ShowOnlineFixDialogSecondaryButton = false;
        IsOnlineFixDialogPrimaryEnabled = true;
        IsOnlineFixDialogSecondaryEnabled = false;
        OnlineFixDialogTitle = "Proses Dibatalkan";
        OnlineFixDialogMessage = "Penerapan Online-Fix dibatalkan.";
        OnlineFixDialogPrimaryText = "Mengerti";
    }

    private void SetOnlineFixDialogFailed(string? message)
    {
        _onlineFixDialogMode = OnlineFixDialogMode.Failed;
        IsOnlineFixDialogOpen = true;
        ShowOnlineFixDialogProgress = false;
        ShowOnlineFixDialogSecondaryButton = false;
        IsOnlineFixDialogPrimaryEnabled = true;
        IsOnlineFixDialogSecondaryEnabled = false;
        OnlineFixDialogTitle = "Online-Fix Gagal";
        OnlineFixDialogMessage = string.IsNullOrWhiteSpace(message)
            ? "Terjadi kendala saat menerapkan Online-Fix. Silakan coba lagi."
            : message;
        OnlineFixDialogPrimaryText = "Mengerti";
    }

    private enum OnlineFixDialogMode
    {
        Idle,
        Checking,
        ConfirmAvailable,
        Applying,
        Unavailable,
        GameNotInstalled,
        Done,
        Unfixing,
        UnfixDone,
        UnfixFailed,
        Failed,
        Cancelled
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
