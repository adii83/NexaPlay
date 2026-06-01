using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Presentation.Views.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class BypassGameDetailViewModel : ObservableObject
{
    private readonly IMetadataService _metadata;
    private readonly ISteamStoreService _storeService;
    private readonly IBypassGamesDataService _bypassGamesData;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private readonly IBypassTutorialVideoService _tutorialVideoService;
    private readonly IAppLogService _log;
    private readonly INavigationService _nav;
    private readonly IWindowsDefenderService _defender;
    private readonly ISteamService _steam;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(3) };

    [ObservableProperty] public partial GameEntry? Game { get; set; }
    [ObservableProperty] public partial GameDetailEntry? Detail { get; set; }
    [ObservableProperty] public partial bool IsDetailLoading { get; set; }
    [ObservableProperty] public partial bool IsDetailAvailable { get; set; }

    [ObservableProperty] public partial string HeroBackgroundUrl { get; set; }
    [ObservableProperty] public partial string GameIconUrl { get; set; }
    [ObservableProperty] public partial FixEntry? BypassEntry { get; set; }
    [ObservableProperty] public partial string CoverArtUrl { get; set; }
    [ObservableProperty] public partial string TutorialVideoTitle { get; set; } = "Tutorial Video";
    [ObservableProperty] public partial string TutorialVideoEmbedUrl { get; set; } = string.Empty;
    [ObservableProperty] public partial string TutorialVideoWatchUrl { get; set; } = string.Empty;
    [ObservableProperty] public partial string TutorialVideoThumbnailUrl { get; set; } = string.Empty;
    public bool HasTutorialVideo => !string.IsNullOrWhiteSpace(TutorialVideoEmbedUrl);

    public IReadOnlyList<string> GenreTags => BuildGenreTags(Game?.Genre);
    public string DisplayShortDescription => Detail?.ShortDescription ?? Game?.ShortDescription ?? string.Empty;
    public string DisplayDeveloper => Detail?.Developers.Count > 0 ? string.Join(", ", Detail.Developers) : Game?.DeveloperDisplay ?? string.Empty;
    public string DisplayPublisher => Detail?.Publishers.Count > 0 ? string.Join(", ", Detail.Publishers) : Game?.PublisherDisplay ?? string.Empty;
    public string DisplayReleaseDate => Detail?.ReleaseDate ?? Game?.ReleaseDate ?? string.Empty;
    public string DisplayPrice => Game?.PriceNormalized > 0 ? $"Rp {Game.PriceNormalized:N0}" : "Free";
    public bool IsPremiumGame => Game?.IsPremium == true;
    public bool ShowAktivasiOfflineBadge => BypassEntry?.AktivasiOffline == true;
    public bool ShowSteamSharingBadge => BypassEntry?.Category == GameCategory.SteamSharing;
    public bool ShowThirdPartySection => BypassEntry is not null && !BypassEntry.IsSteamType;
    public bool ShowSteamSection => BypassEntry is not null && BypassEntry.IsSteamType;
    public bool ShowDapatkanKode => BypassEntry?.DapatkanKode == true;

    public string SteamUsername => BypassEntry?.Username ?? string.Empty;
    public string SteamPassword => BypassEntry?.Password ?? string.Empty;

    [ObservableProperty] public partial bool IsLoadingKode { get; set; }
    [ObservableProperty] public partial string SteamGuardCode { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsBypassProcessing { get; set; }
    [ObservableProperty] public partial int BypassProgressPercent { get; set; }
    [ObservableProperty] public partial string BypassProgressMessage { get; set; } = string.Empty;
    [ObservableProperty] public partial string BypassProgressDetail { get; set; } = string.Empty;
    [ObservableProperty] public partial string BypassErrorMessage { get; set; } = string.Empty;
    public bool HasSteamGuardCodeResult => !string.IsNullOrEmpty(SteamGuardCode);
    public bool IsSteamGuardCodeSuccess => !string.IsNullOrEmpty(SteamGuardCode) && !SteamGuardCode.StartsWith("Error:") && !SteamGuardCode.StartsWith("Tidak ada");
    public bool HasBypassProgress => IsBypassProcessing || BypassProgressPercent > 0;
    public bool HasBypassError => !string.IsNullOrWhiteSpace(BypassErrorMessage);
    public string BypassProgressPercentText => $"{Math.Clamp(BypassProgressPercent, 0, 100)}%";

    private int _loadVersion;
    public Func<string, Task<bool>>? ConfirmAsync { get; set; }
    public Func<string, Task<string?>>? SelectFolderAsync { get; set; }
    public Func<string, string, Task>? ShowDialogAsync { get; set; }

    partial void OnGameChanged(GameEntry? value) => NotifyStatusProperties();

    public BypassGameDetailViewModel(
        IMetadataService metadata,
        ISteamStoreService storeService,
        IBypassGamesDataService bypassGamesData,
        INexaPlayOverrideService nexaPlayOverride,
        IBypassTutorialVideoService tutorialVideoService,
        IAppLogService log,
        INavigationService nav,
        IWindowsDefenderService defender,
        ISteamService steam)
    {
        _metadata = metadata;
        _storeService = storeService;
        _bypassGamesData = bypassGamesData;
        _nexaPlayOverride = nexaPlayOverride;
        _tutorialVideoService = tutorialVideoService;
        _log = log;
        _nav = nav;
        _defender = defender;
        _steam = steam;

        HeroBackgroundUrl = string.Empty;
        GameIconUrl = string.Empty;
        CoverArtUrl = string.Empty;
        TutorialVideoThumbnailUrl = "https://img.youtube.com/vi/lkETeFanN7c/maxresdefault.jpg";
    }

    public async Task LoadAsync(int appId, FixEntry? preferredBypassEntry, CancellationToken ct = default)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);
        ct.ThrowIfCancellationRequested();

        var baseGame = await _metadata.GetMetadataAsync(appId, ct);
        ct.ThrowIfCancellationRequested();
        if (loadVersion != _loadVersion) return;

        Game = baseGame;
        BypassEntry = preferredBypassEntry is not null && preferredBypassEntry.AppId == appId
            ? preferredBypassEntry
            : await ResolveBypassEntryAsync(appId, ct);
        GameIconUrl = Game?.IconImageUrl
            ?? Game?.HeaderImageUrl
            ?? string.Empty;
        OnPropertyChanged(nameof(GenreTags));
        NotifyDisplayProperties();

        IsDetailLoading = true;
        IsDetailAvailable = false;
        try
        {
            var fetched = await _storeService.GetDetailAsync(appId, ct);
            ct.ThrowIfCancellationRequested();
            if (loadVersion != _loadVersion) return;

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

            var overrideCover = (await _nexaPlayOverride.GetCatalogOverrideAsync(appId))?.LibraryCapsule;
            var apiCover = Detail?.LibraryCapsuleUrl;
            var sgdbGrid = Detail?.SgdbGridUrl;

            // Cover art for default 3rd-party section (library capsule portrait 2:3)
            CoverArtUrl = !string.IsNullOrWhiteSpace(BypassEntry?.PosterUrl) ? BypassEntry.PosterUrl
                : !string.IsNullOrWhiteSpace(overrideCover) ? overrideCover
                : !string.IsNullOrWhiteSpace(apiCover) ? apiCover
                : !string.IsNullOrWhiteSpace(sgdbGrid) ? sgdbGrid
                : !string.IsNullOrWhiteSpace(Game?.LibraryCapsuleUrl) ? Game.LibraryCapsuleUrl
                : !string.IsNullOrWhiteSpace(Game?.HeaderImageUrl) ? Game.HeaderImageUrl
                : appId > 0 ? $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/library_600x900_2x.jpg"
                : string.Empty;

            OnPropertyChanged(nameof(CoverArtUrl));
            OnPropertyChanged(nameof(ShowSteamSection));
            OnPropertyChanged(nameof(ShowThirdPartySection));
            OnPropertyChanged(nameof(ShowDapatkanKode));
            OnPropertyChanged(nameof(SteamUsername));
            OnPropertyChanged(nameof(SteamPassword));
            SteamGuardCode = string.Empty;
            IsLoadingKode = false;

            var tutorial = await _tutorialVideoService.GetTutorialVideoAsync(appId, BypassEntry?.Category ?? GameCategory.SteamAccount, ct);
            TutorialVideoTitle = tutorial.Title;
            TutorialVideoEmbedUrl = tutorial.EmbedUrl;
            TutorialVideoWatchUrl = tutorial.WatchUrl;
            TutorialVideoThumbnailUrl = tutorial.ThumbnailUrl;
            OnPropertyChanged(nameof(HasTutorialVideo));
        }
        finally
        {
            IsDetailLoading = false;
        }
    }

    public Task LoadAsync(int appId, CancellationToken ct = default) =>
        LoadAsync(appId, preferredBypassEntry: null, ct);

    [RelayCommand]
    private async Task StartBypassGameAsync()
    {
        if (BypassEntry is null || Game is null || IsBypassProcessing)
            return;

        if (BypassEntry.IsSteamType)
        {
            BypassErrorMessage = "Kategori Akun Steam tidak memakai alur bypass 3rd-party. Gunakan panduan pada bagian Akun Steam.";
            OnPropertyChanged(nameof(HasBypassError));
            if (ShowDialogAsync is not null)
                await ShowDialogAsync("Kategori Tidak Didukung", BypassErrorMessage);
            return;
        }

        if (BypassEntry.Files.Count == 0)
        {
            BypassErrorMessage = "File bypass belum tersedia untuk game ini. Hubungi Admin untuk pembaruan file bypass.";
            OnPropertyChanged(nameof(HasBypassError));
            if (ShowDialogAsync is not null)
                await ShowDialogAsync("File Bypass Tidak Ditemukan", BypassErrorMessage);
            return;
        }

        IsBypassProcessing = true;
        BypassErrorMessage = string.Empty;
        BypassProgressPercent = 0;
        BypassProgressMessage = "Memulai proses bypass...";
        BypassProgressDetail = "Menyiapkan proses...";
        OnPropertyChanged(nameof(HasBypassError));
        OnPropertyChanged(nameof(HasBypassProgress));
        OnPropertyChanged(nameof(BypassProgressPercentText));

        string? downloadDir = null;
        string? extractDir = null;

        try
        {
            ReportProgress(10, "Memeriksa antivirus...");
            try
            {
                var antivirus = await _defender.DetectAntivirusAsync();
                var hasThirdPartyAv = antivirus.Any(a => a.IsActive && a.Vendor != AntivirusVendor.WindowsDefender);
                if (hasThirdPartyAv)
                {
                    var names = string.Join(", ", antivirus.Where(a => a.IsActive && a.Vendor != AntivirusVendor.WindowsDefender).Select(a => a.DisplayName));
                    var confirmed = ConfirmAsync is not null && await ConfirmAsync(
                        $"Antivirus pihak ketiga terdeteksi: {names}.{Environment.NewLine}" +
                        $"Game mungkin tidak berjalan dengan benar selama antivirus ini aktif.{Environment.NewLine}{Environment.NewLine}" +
                        "Sebaiknya uninstall dan gunakan Windows Defender saja." +
                        $"{Environment.NewLine}{Environment.NewLine}Tetap lanjut?");
                    if (!confirmed)
                        throw new InvalidOperationException("Proses dibatalkan. Anda memilih tidak melanjutkan saat antivirus pihak ketiga terdeteksi.");
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception avEx)
            {
                _log.Log("BypassDetail", $"Antivirus check non-fatal appid={Game.AppId} err={avEx.Message}");
            }

            ReportProgress(20, "Mencari lokasi instalasi game...");
            var gamePath = _steam.ResolveGameInstallPath(Game.AppId);
            if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
            {
                gamePath = SelectFolderAsync is null
                    ? null
                    : await SelectFolderAsync("Game belum terdeteksi otomatis. Pilih folder instalasi game secara manual.");

                if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
                    throw new InvalidOperationException("Folder game belum dipilih atau tidak valid. Pilih folder instalasi game yang benar lalu coba lagi.");
            }

            ReportProgress(30, "Menambahkan ke exclusion list...");
            var exclusionResult = await _defender.EnsurePathExcludedAsync(gamePath);
            if (exclusionResult.DefenderMissing)
            {
                if (ShowDialogAsync is not null)
                {
                    await ShowDialogAsync(
                        "Windows Defender Tidak Tersedia",
                        "Windows Defender tidak terpasang atau dinonaktifkan di perangkat ini, sehingga langkah exclusion dilewati.\n\nPastikan antivirus lain tidak menghapus file bypass secara otomatis.");
                }
            }
            else if (!exclusionResult.Success)
            {
                if (exclusionResult.NeedsAdmin)
                {
                    throw new InvalidOperationException(
                        "Aplikasi perlu dijalankan sebagai Administrator untuk menambahkan folder game ke exclusion Windows Defender.\n\n" +
                        "Tutup aplikasi, jalankan kembali sebagai Administrator, lalu ulangi proses bypass.");
                }

                throw new InvalidOperationException(string.IsNullOrWhiteSpace(exclusionResult.Error)
                    ? "Gagal menambahkan folder game ke exclusion Windows Defender."
                    : exclusionResult.Error);
            }

            downloadDir = Path.Combine(Path.GetTempPath(), "NexaPlayFix", Game.AppId.ToString(), "download");
            extractDir = Path.Combine(Path.GetTempPath(), "NexaPlayFix", Game.AppId.ToString(), "extract");
            Directory.CreateDirectory(downloadDir);
            Directory.CreateDirectory(extractDir);

            ReportProgress(40, "Mengunduh file bypass...");
            var downloadedFiles = new List<(FixFile meta, string path, string fileName)>();
            for (var i = 0; i < BypassEntry.Files.Count; i++)
            {
                var file = BypassEntry.Files[i];
                if (string.IsNullOrWhiteSpace(file.GDriveUrl))
                    throw new InvalidOperationException($"Link download untuk part {file.Part} kosong atau tidak valid.");

                var sanitizedName = SanitizeDownloadedFileName(file);
                var target = Path.Combine(downloadDir, sanitizedName);
                await DownloadGoogleDriveFileWithRetryAsync(
                    file.GDriveUrl,
                    target,
                    (bytesRead, totalBytes) =>
                    {
                        var filePercent = totalBytes > 0
                            ? (int)Math.Clamp((bytesRead * 100.0) / totalBytes, 0, 100)
                            : 50;
                        var overall = ((i * 100.0) + filePercent) / Math.Max(1, BypassEntry.Files.Count);
                        var pctSmooth = 40 + (int)Math.Round(overall * 0.3);
                        ReportProgress(
                            pctSmooth,
                            $"Mengunduh file {i + 1}/{BypassEntry.Files.Count}...",
                            $"Progres file: {filePercent}%");
                    });
                ValidateDownloadedArchiveFile(target, sanitizedName);

                downloadedFiles.Add((file, target, sanitizedName));
                var pct = 40 + (int)Math.Round(((i + 1) / (double)BypassEntry.Files.Count) * 30);
                ReportProgress(pct, $"Mengunduh file {i + 1}/{BypassEntry.Files.Count}...", "File selesai diunduh");
            }

            ReportProgress(70, "Mengekstrak file...", "Memproses archive...");
            ExtractDownloadedArchives(downloadedFiles, downloadDir, extractDir, BypassEntry.Password);

            ReportProgress(85, "Mengganti file game...", "Menimpa file yang sama seperti GameHub...");
            var allFiles = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
            var total = Math.Max(1, allFiles.Length);
            for (var i = 0; i < allFiles.Length; i++)
            {
                var source = allFiles[i];
                var relative = Path.GetRelativePath(extractDir, source);
                var dest = Path.Combine(gamePath, relative);
                var destDir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrWhiteSpace(destDir))
                    Directory.CreateDirectory(destDir);
                File.Copy(source, dest, overwrite: true);
                var pct = 85 + (int)Math.Round(((i + 1) / (double)total) * 13);
                ReportProgress(pct, $"Mengganti file... {i + 1}/{total}", "Replace (overwrite) file lama");
            }

            ReportProgress(99, "Membersihkan file temporary...", "Menghapus file sementara...");
            TryDeleteDirectory(downloadDir);
            TryDeleteDirectory(extractDir);

            if (BypassEntry.UseShortcut)
            {
                ReportProgress(99, "Membuat shortcut desktop...", "Menyelesaikan tahap akhir...");
                await TryAutoCreateShortcutAsync(gamePath, Game.Name, BypassEntry.ExeHint);
            }

            var launchOption = BuildLaunchOptionForGame(
                gamePath,
                Game.AppId,
                BypassEntry.UseShortcut,
                BypassEntry.ExeHint,
                BypassEntry.LaunchOption);
            if (!string.IsNullOrWhiteSpace(launchOption))
            {
                ReportProgress(99, "Sinkronisasi Steam launch option...", "Menutup Steam, set launch option, lalu restart Steam...");
                var launchOptionApplied = await _steam.SetLaunchOptionsAndRestartAsync(Game.AppId, launchOption);
                if (!launchOptionApplied)
                    _log.Log("BypassDetail", $"SetLaunchOptions gagal appid={Game.AppId}");
            }

            ReportProgress(100, "Selesai! Proses bypass berhasil.", "Semua tahap selesai");
            if (ShowDialogAsync is not null)
                await ShowDialogAsync("Bypass Berhasil", "Semua proses bypass selesai. Game siap dijalankan.");
        }
        catch (Exception ex)
        {
            var friendlyError = string.IsNullOrWhiteSpace(ex.Message)
                ? "Proses bypass gagal karena terjadi kesalahan yang tidak diketahui. Silakan coba ulang."
                : ex.Message;

            BypassErrorMessage = friendlyError;
            _log.Log("BypassDetail", $"StartBypassGame failed appid={Game.AppId} err={friendlyError}");
            OnPropertyChanged(nameof(HasBypassError));
            if (ShowDialogAsync is not null)
            {
                var errorTitle = friendlyError.Contains("dibatalkan", StringComparison.OrdinalIgnoreCase)
                    ? "Proses Dibatalkan"
                    : friendlyError.Contains("Administrator", StringComparison.OrdinalIgnoreCase)
                        ? "Butuh Hak Administrator"
                        : "Bypass Gagal";
                await ShowDialogAsync(errorTitle, friendlyError);
            }
        }
        finally
        {
            IsBypassProcessing = false;
            OnPropertyChanged(nameof(HasBypassProgress));
            if (downloadDir is not null) TryDeleteDirectory(downloadDir);
            if (extractDir is not null) TryDeleteDirectory(extractDir);
        }
    }

    [RelayCommand]
    private void CopySteamUsername()
    {
        if (!string.IsNullOrEmpty(SteamUsername))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(SteamUsername);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    [RelayCommand]
    private void CopySteamPassword()
    {
        if (!string.IsNullOrEmpty(SteamPassword))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(SteamPassword);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    [RelayCommand]
    private async Task ReportAccountAsync()
    {
        // Navigasi ke tautan pelaporan / WhatsApp admin
        var uri = new Uri("https://wa.me/6281234567890?text=Lapor%20akun%20bermasalah%20di%20Game:%20" + Uri.EscapeDataString(BypassEntry?.Title ?? "Unknown"));
        await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    [RelayCommand]
    private async Task GetSteamGuardCodeAsync()
    {
        if (string.IsNullOrEmpty(SteamUsername)) return;
        
        IsLoadingKode = true;
        SteamGuardCode = string.Empty;
        
        try
        {
            var code = await NexaPlay.Infrastructure.Services.SteamGuardService.FetchLatestSteamGuardCodeAsync(SteamUsername);
            SteamGuardCode = code;
        }
        catch (Exception ex)
        {
            SteamGuardCode = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoadingKode = false;
            OnPropertyChanged(nameof(HasSteamGuardCodeResult));
            OnPropertyChanged(nameof(IsSteamGuardCodeSuccess));
        }
    }

    [RelayCommand]
    private void CopySteamGuardCode()
    {
        if (IsSteamGuardCodeSuccess)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(SteamGuardCode);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
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
        OnPropertyChanged(nameof(DisplayDeveloper));
        OnPropertyChanged(nameof(DisplayPublisher));
        OnPropertyChanged(nameof(DisplayReleaseDate));
        OnPropertyChanged(nameof(DisplayPrice));
        NotifyStatusProperties();
    }

    private void NotifyStatusProperties()
    {
        OnPropertyChanged(nameof(IsPremiumGame));
        OnPropertyChanged(nameof(ShowAktivasiOfflineBadge));
        OnPropertyChanged(nameof(ShowSteamSharingBadge));
        OnPropertyChanged(nameof(ShowThirdPartySection));
        OnPropertyChanged(nameof(ShowDapatkanKode));
        OnPropertyChanged(nameof(HasBypassProgress));
        OnPropertyChanged(nameof(HasBypassError));
        OnPropertyChanged(nameof(BypassProgressPercentText));
    }

    private void ReportProgress(int percent, string message, string? detail = null)
    {
        BypassProgressPercent = Math.Clamp(percent, 0, 100);
        BypassProgressMessage = message;
        BypassProgressDetail = detail ?? string.Empty;
        OnPropertyChanged(nameof(HasBypassProgress));
        OnPropertyChanged(nameof(BypassProgressPercentText));
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch { }
    }

    private static string SanitizeDownloadedFileName(FixFile file)
    {
        var filename = file.Filename;
        if (string.IsNullOrWhiteSpace(filename) ||
            filename.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            filename.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            filename = $"part{file.Part}.rar";
        }

        var safe = Path.GetFileName(filename);
        if (string.IsNullOrWhiteSpace(safe))
            safe = $"part{file.Part}.rar";

        foreach (var c in Path.GetInvalidFileNameChars())
            safe = safe.Replace(c, '_');

        return safe;
    }

    private static string? BuildLaunchOptionForGame(string gamePath, int appId, bool useShortcut, string? exeHint, string? rawLaunchOption)
    {
        // Requirement: otomatis dari hasil bypass, tanpa mapping JSON eksternal.
        // Penentu tetap: hanya entry dengan use_shortcut=true + exe_hint.
        if (!useShortcut || string.IsNullOrWhiteSpace(exeHint))
            return null;

        var exePath = ResolveExePathFromHint(gamePath, exeHint);
        if (string.IsNullOrWhiteSpace(exePath))
            return null;

        var escapedExe = exePath.Replace("\"", "\\\"");
        return $"\"{escapedExe}\" %command%";
    }

    private static string? ResolveExePathFromHint(string gamePath, string exeHint)
    {
        if (string.IsNullOrWhiteSpace(gamePath) || string.IsNullOrWhiteSpace(exeHint))
            return null;

        var exeName = Path.GetFileName(exeHint.Trim());
        if (string.IsNullOrWhiteSpace(exeName))
            return null;

        var directPath = Path.Combine(gamePath, exeName);
        if (File.Exists(directPath))
            return directPath;

        try
        {
            // Cari rekursif berdasarkan exe_hint seperti logika pemilihan exe shortcut.
            return Directory.EnumerateFiles(gamePath, "*.exe", SearchOption.AllDirectories)
                .FirstOrDefault(p => string.Equals(Path.GetFileName(p), exeName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    private async Task DownloadGoogleDriveFileWithRetryAsync(string sourceUrl, string targetPath, Action<long, long>? progress = null)
    {
        const int maxRetries = 3;
        Exception? last = null;
        for (var retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                await DownloadGoogleDriveFileAsync(sourceUrl, targetPath, progress);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                if (retry < maxRetries - 1)
                    await Task.Delay(2000 * (retry + 1));
            }
        }

        throw new InvalidOperationException($"Gagal download file dari Google Drive setelah {maxRetries} percobaan: {last?.Message}");
    }

    private async Task DownloadGoogleDriveFileAsync(string sourceUrl, string targetPath, Action<long, long>? progress = null)
    {
        var fileId = ExtractGoogleDriveFileId(sourceUrl);
        if (string.IsNullOrWhiteSpace(fileId))
            throw new InvalidOperationException("URL Google Drive tidak valid (file id tidak ditemukan).");

        var downloadUrl1 = $"https://drive.usercontent.google.com/download?id={fileId}&export=download&confirm=t";
        using var response1 = await _http.GetAsync(downloadUrl1, HttpCompletionOption.ResponseHeadersRead);
        if (response1.StatusCode == HttpStatusCode.TooManyRequests)
            throw new InvalidOperationException("Rate limit Google Drive tercapai. Silakan coba lagi nanti.");

        if (!IsHtmlResponse(response1))
        {
            await SaveResponseToFileAsync(response1, targetPath, progress);
            return;
        }

        var html = await response1.Content.ReadAsStringAsync();
        var extractedUrl = ExtractGoogleDriveDownloadLink(html, sourceUrl);
        if (!string.IsNullOrWhiteSpace(extractedUrl) && !string.Equals(extractedUrl, downloadUrl1, StringComparison.OrdinalIgnoreCase))
        {
            using var response2 = await _http.GetAsync(extractedUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!IsHtmlResponse(response2))
            {
                await SaveResponseToFileAsync(response2, targetPath, progress);
                return;
            }
        }

        var downloadUrl3 = $"https://drive.google.com/uc?export=download&id={fileId}&confirm=t";
        using var response3 = await _http.GetAsync(downloadUrl3, HttpCompletionOption.ResponseHeadersRead);
        if (!IsHtmlResponse(response3))
        {
            await SaveResponseToFileAsync(response3, targetPath, progress);
            return;
        }

        throw new InvalidOperationException("Google Drive mengembalikan halaman HTML, bukan file archive. Link membutuhkan konfirmasi manual atau akses dibatasi.");
    }

    private static string? ExtractGoogleDriveFileId(string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
            return null;

        var queryId = Regex.Match(sourceUrl, @"[?&]id=([^&]+)", RegexOptions.IgnoreCase);
        if (queryId.Success)
            return Uri.UnescapeDataString(queryId.Groups[1].Value);

        var filePathId = Regex.Match(sourceUrl, @"/file/d/([^/]+)", RegexOptions.IgnoreCase);
        if (filePathId.Success)
            return Uri.UnescapeDataString(filePathId.Groups[1].Value);

        return null;
    }

    private static string ExtractGoogleDriveDownloadLink(string html, string originalUrl)
    {
        try
        {
            var hrefMatch = Regex.Match(html, @"href=[""']([^""']*uc\?export=download[^""']*)[""']", RegexOptions.IgnoreCase);
            if (hrefMatch.Success)
            {
                var href = WebUtility.HtmlDecode(hrefMatch.Groups[1].Value);
                if (href.StartsWith("/"))
                    return "https://drive.google.com" + href;
                if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return href;
            }

            var formMatch = Regex.Match(html, @"<form[^>]*action=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
            if (formMatch.Success)
            {
                var action = WebUtility.HtmlDecode(formMatch.Groups[1].Value);
                if (action.Contains("export=download", StringComparison.OrdinalIgnoreCase))
                {
                    if (action.StartsWith("/"))
                        return "https://drive.google.com" + action;
                    if (action.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        return action;
                }
            }

            var fileId = ExtractGoogleDriveFileId(originalUrl);
            if (!string.IsNullOrWhiteSpace(fileId))
                return $"https://drive.usercontent.google.com/download?id={fileId}&export=download&confirm=t&uuid=";

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsHtmlResponse(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SaveResponseToFileAsync(HttpResponseMessage response, string targetPath, Action<long, long>? progress = null)
    {
        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        long bytesRead = 0;
        var buffer = new byte[8192];
        await using var httpStream = await response.Content.ReadAsStreamAsync();
        await using var fs = File.Create(targetPath);
        int read;
        while ((read = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fs.WriteAsync(buffer, 0, read);
            bytesRead += read;
            progress?.Invoke(bytesRead, totalBytes);
        }
        progress?.Invoke(bytesRead, totalBytes);
    }

    private static void ValidateDownloadedArchiveFile(string targetPath, string fileName)
    {
        if (!File.Exists(targetPath))
            throw new InvalidOperationException($"File hasil download tidak ditemukan: {fileName}");

        var info = new FileInfo(targetPath);
        if (info.Length < 64)
            throw new InvalidOperationException($"File download terlalu kecil/tidak valid: {fileName} ({info.Length} bytes)");

        using var fs = File.OpenRead(targetPath);
        Span<byte> header = stackalloc byte[8];
        var read = fs.Read(header);
        if (read <= 0)
            throw new InvalidOperationException($"File download kosong: {fileName}");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext == ".zip")
        {
            if (read < 4 || !(header[0] == 0x50 && header[1] == 0x4B))
                throw new InvalidOperationException($"File bukan ZIP valid: {fileName}");
        }
        else if (ext == ".rar")
        {
            var isRar4 = read >= 7 && header[0] == 0x52 && header[1] == 0x61 && header[2] == 0x72 && header[3] == 0x21 && header[4] == 0x1A && header[5] == 0x07 && header[6] == 0x00;
            var isRar5 = read >= 8 && header[0] == 0x52 && header[1] == 0x61 && header[2] == 0x72 && header[3] == 0x21 && header[4] == 0x1A && header[5] == 0x07 && header[6] == 0x01 && header[7] == 0x00;
            if (!isRar4 && !isRar5)
                throw new InvalidOperationException($"File bukan RAR valid (kemungkinan halaman HTML Google Drive): {fileName}");
        }
    }

    private static void ExtractDownloadedArchives(
        List<(FixFile meta, string path, string fileName)> downloadedFiles,
        string downloadDir,
        string extractDir,
        string? password)
    {
        string GetArchiveKey(string fileName)
        {
            if (fileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
            {
                var m = Regex.Match(fileName, @"^(.*?)(?:\.part\d+)?\.rar$", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value;
            }
            return fileName;
        }

        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in downloadedFiles)
        {
            var key = GetArchiveKey(file.fileName);
            if (!processed.Add(key)) continue;

            var ext = Path.GetExtension(file.fileName).ToLowerInvariant();
            var isRar = ext == ".rar";
            var isZip = ext == ".zip";
            if (!isRar && !isZip) continue;

            if (isRar)
            {
                var partFiles = downloadedFiles
                    .Where(f => f.fileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
                    .Where(f => Regex.IsMatch(f.fileName, $"^{Regex.Escape(key)}(?:\\.part\\d+)?\\.rar$", RegexOptions.IgnoreCase))
                    .Select(f => f.fileName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (partFiles.Count == 0) partFiles.Add(file.fileName);
                ValidatePartFiles(partFiles, downloadDir);
                var orderedParts = partFiles.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
                var firstPart = DetermineFirstPartFile(orderedParts);
                var firstPartPath = Path.Combine(downloadDir, firstPart);

                var extractResult = ExtractWithExternalTool(firstPartPath, extractDir, password);
                if (!extractResult.success)
                    throw new InvalidOperationException($"WinRAR/7-Zip gagal mengekstrak {firstPart}: {extractResult.error}");
            }
            else
            {
                ExtractArchiveToDirectory(file.path, extractDir);
            }
        }
    }

    private static string DetermineFirstPartFile(List<string> fileNames)
    {
        if (fileNames.Count == 0)
            throw new InvalidOperationException("Daftar file archive kosong.");

        var candidate = fileNames.FirstOrDefault(f => f.EndsWith(".part1.rar", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(candidate)) return candidate;

        candidate = fileNames.FirstOrDefault(f =>
            Path.GetExtension(f).Equals(".rar", StringComparison.OrdinalIgnoreCase) &&
            !f.Contains(".part", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(candidate)) return candidate;

        int PartNumber(string name)
        {
            var match = Regex.Match(name, @"\.part(\d+)\.rar", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
                return value;
            return int.MaxValue;
        }

        return fileNames.OrderBy(n => PartNumber(n)).First();
    }

    private static void ValidatePartFiles(List<string> fileNames, string rootPath)
    {
        foreach (var partFile in fileNames)
        {
            var partPath = Path.Combine(rootPath, partFile);
            if (!File.Exists(partPath))
                throw new InvalidOperationException($"Part file tidak ditemukan: {partFile}");

            var info = new FileInfo(partPath);
            if (info.Length < 20)
                throw new InvalidOperationException($"Part file terlalu kecil/corrupt: {partFile} ({info.Length} bytes)");
        }
    }

    private static void ExtractArchiveToDirectory(string archivePath, string extractDir)
    {
        var ext = Path.GetExtension(archivePath).ToLowerInvariant();
        if (ext == ".zip")
        {
            ZipFile.ExtractToDirectory(archivePath, extractDir, overwriteFiles: true);
            return;
        }
        throw new InvalidOperationException($"Format archive '{ext}' perlu diekstrak via external tool.");
    }

    private static (bool success, string? error) ExtractWithExternalTool(string archivePath, string extractTo, string? password)
    {
        string? winrarError = null;
        var winrar = DetectWinRARPath();
        if (!string.IsNullOrWhiteSpace(winrar) && File.Exists(winrar))
        {
            var winrarResult = ExtractWithWinRar(winrar, archivePath, extractTo, password);
            if (winrarResult.success)
                return winrarResult;
            winrarError = winrarResult.error;
        }

        string? sevenZipError = null;
        var sevenZip = Detect7ZipPath();
        if (!string.IsNullOrWhiteSpace(sevenZip) && File.Exists(sevenZip))
        {
            var sevenZipResult = ExtractWith7Zip(sevenZip, archivePath, extractTo, password);
            if (sevenZipResult.success)
                return sevenZipResult;
            sevenZipError = sevenZipResult.error;
        }

        if (winrarError is null && sevenZipError is null)
            return (false, "WinRAR atau 7-Zip tidak ditemukan.");

        return (false, $"WinRAR gagal: {winrarError ?? "-"} | 7-Zip gagal: {sevenZipError ?? "-"}");
    }

    private static string? DetectWinRARPath()
    {
        var possible = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinRAR", "WinRAR.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinRAR", "WinRAR.exe"),
            @"C:\Program Files\WinRAR\WinRAR.exe",
            @"C:\Program Files (x86)\WinRAR\WinRAR.exe"
        };

        foreach (var path in possible)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static string? Detect7ZipPath()
    {
        var possible = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe"),
            @"C:\Program Files\7-Zip\7z.exe",
            @"C:\Program Files (x86)\7-Zip\7z.exe"
        };

        foreach (var path in possible)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static (bool success, string? error) ExtractWithWinRar(string winrarPath, string archivePath, string extractTo, string? password)
    {
        var args = new StringBuilder();
        args.Append("x ");
        args.Append("-ibck -inul -o+ ");
        if (!string.IsNullOrWhiteSpace(password))
        {
            var escapedPassword = password.Replace("\"", "\"\"");
            args.Append($"-p\"{escapedPassword}\" ");
        }
        else
        {
            // Disable interactive password prompt in silent/background mode.
            args.Append("-p- ");
        }
        args.Append($"\"{archivePath}\" \"{extractTo}\"");

        var psi = new ProcessStartInfo
        {
            FileName = winrarPath,
            Arguments = args.ToString(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(archivePath) ?? string.Empty
        };

        using var proc = Process.Start(psi);
        if (proc is null) return (false, "Gagal memulai WinRAR.");
        proc.WaitForExit();
        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();

        if (proc.ExitCode is 0 or 1) return (true, null);
        return (false, $"WinRAR exit code {proc.ExitCode}. {error} {output}".Trim());
    }

    private static (bool success, string? error) ExtractWith7Zip(string sevenZipPath, string archivePath, string extractTo, string? password)
    {
        var args = new StringBuilder();
        args.Append("x ");
        args.Append($"-o\"{extractTo}\" ");
        args.Append("-y ");
        if (!string.IsNullOrWhiteSpace(password)) args.Append($"-p{password} ");
        args.Append($"\"{archivePath}\"");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = sevenZipPath,
            Arguments = args.ToString(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(archivePath) ?? string.Empty
        };

        using var proc = Process.Start(psi);
        if (proc is null) return (false, "Gagal memulai 7-Zip.");
        proc.WaitForExit();
        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();

        if (proc.ExitCode == 0) return (true, null);
        return (false, $"7-Zip exit code {proc.ExitCode}. {error} {output}".Trim());
    }

    private async Task TryAutoCreateShortcutAsync(string gamePath, string gameName, string? exeHint)
    {
        try
        {
            var executables = Directory.GetFiles(gamePath, "*.exe", SearchOption.AllDirectories)
                .Where(p => !Path.GetFileName(p).StartsWith("unins", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (executables.Count == 0) return;

            string? selected = null;
            if (!string.IsNullOrWhiteSpace(exeHint))
            {
                selected = executables.FirstOrDefault(p =>
                    string.Equals(Path.GetFileNameWithoutExtension(p), Path.GetFileNameWithoutExtension(exeHint), StringComparison.OrdinalIgnoreCase))
                    ?? executables.FirstOrDefault(p =>
                        Path.GetFileName(p).Contains(exeHint, StringComparison.OrdinalIgnoreCase));
            }

            selected ??= executables
                .OrderByDescending(p => ScoreExecutable(Path.GetFileNameWithoutExtension(p), gameName))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(selected)) return;

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(desktop, $"{gameName} - FIX.lnk");
            var escapedTarget = selected.Replace("'", "''");
            var escapedShortcut = shortcutPath.Replace("'", "''");
            var escapedWorkingDir = Path.GetDirectoryName(selected)!.Replace("'", "''");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"$s=(New-Object -ComObject WScript.Shell).CreateShortcut('{escapedShortcut}');$s.TargetPath='{escapedTarget}';$s.WorkingDirectory='{escapedWorkingDir}';$s.Save()\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is not null)
                await proc.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            _log.Log("BypassDetail", $"Auto shortcut gagal: {ex.Message}");
        }
    }

    private static int ScoreExecutable(string exeName, string gameName)
    {
        var exe = exeName.ToLowerInvariant();
        var game = gameName.ToLowerInvariant();
        var score = 0;
        if (exe.Contains("launcher")) score += 20;
        if (exe.Contains("shipping")) score += 10;
        if (game.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(w => exe.Contains(w))) score += 30;
        if (exe.Length < 32) score += 5;
        return score;
    }

    private async Task<FixEntry?> ResolveBypassEntryAsync(int appId, CancellationToken ct)
    {
        var steam = await _bypassGamesData.GetSteamGamesAsync(ct);
        var steamMatch = steam.FirstOrDefault(x => x.AppId == appId);
        if (steamMatch is not null)
            return steamMatch;

        var direct = await _bypassGamesData.GetFixAsync(appId, ct);
        if (direct is not null)
            return direct;

        var newer = await _bypassGamesData.GetNewFixesAsync(ct);
        return newer.FirstOrDefault(x => x.AppId == appId);
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
}
