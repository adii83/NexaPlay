using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAppUpdateService _appUpdate;
    private readonly ILicenseService _license;
    private readonly ISteamService _steam;
    private readonly IWindowsDefenderService _defender;
    private readonly IMetadataService _metadata;
    private readonly IGameCoverIndexService _gameCoverIndex;
    private readonly ICoverImageCacheService _coverImageCache;
    private readonly IBypassGamesDataService _fixData;
    private readonly IAppLogService _log;
    private readonly INexaPlayOverrideService _nexaPlayOverride;

    // License
    [ObservableProperty] public partial LicenseInfo? CurrentLicense { get; set; }
    [ObservableProperty] public partial string LicenseKeyInput { get; set; }
    [ObservableProperty] public partial bool IsActivating { get; set; }
    [ObservableProperty] public partial string ActivationMessage { get; set; }
    [ObservableProperty] public partial bool ActivationSuccess { get; set; }
    [ObservableProperty] public partial bool IsLoadingGames { get; set; }
    [ObservableProperty] public partial double DownloadProgress { get; set; }
    [ObservableProperty] public partial string DownloadProgressText { get; set; }
    [ObservableProperty] public partial string CurrentAppVersion { get; set; }
    [ObservableProperty] public partial string LatestAppVersion { get; set; }
    [ObservableProperty] public partial string UpdateStatusTitle { get; set; }
    [ObservableProperty] public partial string UpdateStatusMessage { get; set; }
    [ObservableProperty] public partial string LastUpdateCheckedText { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> UpdateReleaseNotes { get; set; }
    [ObservableProperty] public partial bool IsUpdateAvailable { get; set; }
    [ObservableProperty] public partial bool IsCheckingForUpdates { get; set; }
    [ObservableProperty] public partial bool IsInstallingUpdate { get; set; }
    [ObservableProperty] public partial double UpdateDownloadProgress { get; set; }
    [ObservableProperty] public partial string UpdateDownloadProgressText { get; set; }

    // Steam
    [ObservableProperty] public partial string SteamPath { get; set; }
    [ObservableProperty] public partial int SteamLibraryCount { get; set; }

    // Defender
    [ObservableProperty] public partial IReadOnlyList<AntivirusInfo> AntivirusList { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> Exclusions { get; set; }
    [ObservableProperty] public partial bool IsDetecting { get; set; }

    // System
    [ObservableProperty] public partial string DeviceId { get; set; }

    public SettingsViewModel(
        IAppUpdateService appUpdate,
        ILicenseService license, ISteamService steam,
        IWindowsDefenderService defender, IMetadataService metadata,
        IGameCoverIndexService gameCoverIndex,
        ICoverImageCacheService coverImageCache,
        IBypassGamesDataService fixData, IAppLogService log,
        INexaPlayOverrideService nexaPlayOverride)
    {
        _appUpdate = appUpdate;
        _license  = license;
        _steam    = steam;
        _defender = defender;
        _metadata = metadata;
        _gameCoverIndex = gameCoverIndex;
        _coverImageCache = coverImageCache;
        _fixData  = fixData;
        _log      = log;
        _nexaPlayOverride = nexaPlayOverride;

        // Default values for partial properties
        LicenseKeyInput   = string.Empty;
        ActivationMessage = string.Empty;
        SteamPath         = "Not detected";
        AntivirusList     = Array.Empty<AntivirusInfo>();
        Exclusions        = Array.Empty<string>();
        DeviceId          = string.Empty;
        CurrentAppVersion = _appUpdate.CurrentVersion;
        LatestAppVersion = _appUpdate.CurrentVersion;
        UpdateStatusTitle = "Belum pernah diperiksa";
        UpdateStatusMessage = "NexaPlay belum melakukan pengecekan update untuk sesi ini.";
        LastUpdateCheckedText = "-";
        UpdateReleaseNotes = Array.Empty<string>();
        UpdateDownloadProgressText = "0%";
    }

    public async Task LoadAsync()
    {
        DeviceId           = _license.GetDeviceId();
        CurrentLicense     = await _license.LoadAsync();
        SteamPath          = _steam.GetSteamBasePath() ?? "Not detected";
        SteamLibraryCount  = _steam.GetLibraryPaths().Count;
        await LoadCachedUpdateStatusAsync();
    }

    private async Task LoadCachedUpdateStatusAsync()
    {
        var result = await _appUpdate.GetCachedStatusAsync();
        ApplyUpdateResult(result);
    }

    public async Task CheckForUpdatesAsync(bool force = true)
    {
        IsCheckingForUpdates = true;
        UpdateStatusMessage = "Sedang memeriksa manifest update terbaru...";

        try
        {
            var result = await _appUpdate.CheckForUpdatesAsync(force);
            ApplyUpdateResult(result);
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    public async Task InstallUpdateAsync()
    {
        if (!IsUpdateAvailable)
        {
            throw new InvalidOperationException("Belum ada update yang bisa dipasang.");
        }

        var latest = await _appUpdate.CheckForUpdatesAsync(force: true);
        ApplyUpdateResult(latest);

        if (!latest.IsUpdateAvailable)
        {
            return;
        }

        IsInstallingUpdate = true;
        UpdateDownloadProgress = 0;
        UpdateDownloadProgressText = "0%";
        UpdateStatusMessage = $"Mengunduh installer NexaPlay {latest.LatestVersion}...";

        try
        {
            var progress = new Progress<double>(p =>
            {
                UpdateDownloadProgress = Math.Clamp(p, 0, 100);
                UpdateDownloadProgressText = $"{UpdateDownloadProgress:F0}%";
            });

            var installerPath = await _appUpdate.DownloadInstallerAsync(latest, progress);
            UpdateStatusMessage = "Installer selesai diunduh. Menyiapkan proses update...";
            await _appUpdate.LaunchInstallerAndExitAsync(installerPath);
        }
        finally
        {
            IsInstallingUpdate = false;
        }
    }

    private void ApplyUpdateResult(AppUpdateCheckResult result)
    {
        CurrentAppVersion = result.CurrentVersion;
        LatestAppVersion = string.IsNullOrWhiteSpace(result.LatestVersion) ? result.CurrentVersion : result.LatestVersion;
        IsUpdateAvailable = result.IsUpdateAvailable;
        UpdateReleaseNotes = result.ReleaseNotes?.Count > 0 ? result.ReleaseNotes : Array.Empty<string>();
        LastUpdateCheckedText = result.LastCheckedAt?.ToLocalTime().ToString("dd MMM yyyy, HH:mm") ?? "-";

        if (result.IsUpdateAvailable)
        {
            UpdateStatusTitle = result.Mandatory ? "Update wajib tersedia" : "Update tersedia";
            UpdateStatusMessage = string.IsNullOrWhiteSpace(result.Message)
                ? $"Versi {result.LatestVersion} tersedia untuk dipasang."
                : result.Message;
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.Message) && result.Message.StartsWith("Gagal memeriksa update:", StringComparison.OrdinalIgnoreCase))
        {
            UpdateStatusTitle = "Gagal memeriksa update";
            UpdateStatusMessage = result.Message;
            return;
        }

        if (result.LastCheckedAt is not null)
        {
            UpdateStatusTitle = "Sudah versi terbaru";
            UpdateStatusMessage = "Versi NexaPlay Anda sudah paling baru.";
            return;
        }

        UpdateStatusTitle = "Belum pernah diperiksa";
        UpdateStatusMessage = result.Message;
    }

    public async Task ActivateLicenseAsync()
    {
        if (string.IsNullOrWhiteSpace(LicenseKeyInput)) return;
        IsActivating      = true;
        ActivationMessage = "Validating...";
        ActivationSuccess = false;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        var result = await _license.ActivateAsync(LicenseKeyInput.Trim().ToUpperInvariant(), cts.Token);
        CurrentLicense = result;

        ActivationSuccess = result.IsValid;
        ActivationMessage = result.Status switch
        {
            LicenseStatus.Valid          => $"License activated! Plan: {result.Plan}",
            LicenseStatus.Banned         => "This license has been banned.",
            LicenseStatus.DeviceMismatch => "License is bound to another device.",
            LicenseStatus.Offline        => "Cannot connect. Saved for offline use.",
            LicenseStatus.NetworkError   => "Network error. Check your connection.",
            _                            => result.Message ?? "Invalid license key."
        };
        IsActivating = false;
        _log.Log("Settings", $"Activation result: {result.Status}");
    }

    public async Task DeactivateLicenseAsync()
    {
        await _license.DeactivateAsync();
        CurrentLicense    = null;
        ActivationMessage = "License removed.";
        LicenseKeyInput   = string.Empty;
    }

    public async Task DetectAntivirusAsync()
    {
        IsDetecting   = true;
        AntivirusList = await _defender.DetectAntivirusAsync();
        Exclusions    = await _defender.GetExclusionsAsync();
        IsDetecting   = false;
    }

    public async Task<(bool Success, string Message)> ClearMetadataCacheAsync()
    {
        try 
        {
            await _metadata.ClearCacheAsync();
            await _gameCoverIndex.ClearCacheAsync();
            await _coverImageCache.ClearCacheAsync();
            _log.Log("Settings", "Metadata cache cleared");
            return (true, "Cache aplikasi berhasil dihapus. Ruang penyimpanan telah dikosongkan.");
        }
        catch (Exception ex)
        {
            _log.Log("Settings", $"Clear cache failed: {ex.Message}");
            return (false, $"Gagal menghapus cache: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> LoadGamesAsync()
    {
        IsLoadingGames = true;
        ActivationMessage = "Memuat Daftar Game terbaru...";
        DownloadProgress = 0;
        DownloadProgressText = "0%";

        try
        {
            var progress = new Progress<double>(p => 
            {
                DownloadProgress = Math.Min(100, p * 0.8);
                DownloadProgressText = $"{DownloadProgress:F0}%";
            });

            // Refresh main dynamic sources (override_data.json, fix_games.json, new_fix_games.json, steam_games.json)
            await _metadata.RefreshDynamicSourcesAsync(progress);

            DownloadProgress = 85;
            DownloadProgressText = "85%";
            await _metadata.GetPopularAppIdsAsync();
            
            DownloadProgress = 90;
            DownloadProgressText = "90%";
            await _metadata.GetNewFixAppIdsAsync();

            DownloadProgress = 95;
            DownloadProgressText = "95%";
            await _nexaPlayOverride.RefreshAsync();
            
            // Refresh fix data service from newly downloaded files
            await _fixData.RefreshAsync();

            DownloadProgress = 100;
            DownloadProgressText = "100%";
            _log.Log("Settings", "Games data successfully loaded.");
            return (true, "Data game terbaru berhasil diunduh dan diperbarui.");
        }
        catch (System.Net.Http.HttpRequestException hex)
        {
            _log.Log("Settings", $"LoadGamesAsync network error: {hex.Message}");
            return (false, "Gagal mengunduh data. Periksa koneksi internet Anda dan coba lagi.");
        }
        catch (Exception ex)
        {
            _log.Log("Settings", $"LoadGamesAsync failed: {ex.Message}");
            return (false, $"Terjadi kesalahan saat memuat data: {ex.Message}");
        }
        finally
        {
            IsLoadingGames = false;
        }
    }

    public async Task ClearAllDataAndRestartAsync()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Core.Constants.AppConstants.AppDataFolder);
            var exePath = Environment.ProcessPath;

            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                throw new InvalidOperationException("Lokasi executable NexaPlay tidak ditemukan untuk restart.");
            }

            var cleanupScriptPath = Path.Combine(
                Path.GetTempPath(),
                $"nexaplay-clear-data-{Guid.NewGuid():N}.cmd");

            var cleanupScript = new StringBuilder()
                .AppendLine("@echo off")
                .AppendLine("setlocal enableextensions")
                .AppendLine($"set \"TARGET={appDataPath}\"")
                .AppendLine($"set \"EXE={exePath}\"")
                .AppendLine("timeout /t 2 /nobreak >nul")
                .AppendLine(":retry_delete")
                .AppendLine("if exist \"%TARGET%\" (")
                .AppendLine("  rmdir /s /q \"%TARGET%\" >nul 2>nul")
                .AppendLine("  if exist \"%TARGET%\" (")
                .AppendLine("    timeout /t 1 /nobreak >nul")
                .AppendLine("    goto retry_delete")
                .AppendLine("  )")
                .AppendLine(")")
                .AppendLine("start \"\" \"%EXE%\"")
                .AppendLine("del \"%~f0\"")
                .ToString();

            await File.WriteAllTextAsync(cleanupScriptPath, cleanupScript);

            _log.Log("Settings", $"Factory reset dijadwalkan. target={appDataPath}");

            Process.Start(new ProcessStartInfo
            {
                FileName = cleanupScriptPath,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch (Exception ex)
        {
            _log.Log("Settings", $"Clear data failed: {ex.Message}");
            throw;
        }

        Application.Current.Exit();
    }

    public async Task RefreshFixDataAsync()
    {
        await _fixData.RefreshAsync();
        ActivationMessage = "Fix catalog refreshed.";
        _log.Log("Settings", "Fix data refreshed");
    }

    public async Task RestartSteamAsync()
    {
        await _steam.RestartAsync();
    }

    public async Task ScanSteamLibraryAsync()
    {
        var games = _steam.ScanInstalledGames();
        SteamLibraryCount = _steam.GetLibraryPaths().Count;
        ActivationMessage = $"Found {games.Count} installed games.";
    }
}
