using CommunityToolkit.Mvvm.ComponentModel;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ILicenseService _license;
    private readonly ISteamService _steam;
    private readonly IWindowsDefenderService _defender;
    private readonly IMetadataService _metadata;
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
        ILicenseService license, ISteamService steam,
        IWindowsDefenderService defender, IMetadataService metadata,
        IBypassGamesDataService fixData, IAppLogService log,
        INexaPlayOverrideService nexaPlayOverride)
    {
        _license  = license;
        _steam    = steam;
        _defender = defender;
        _metadata = metadata;
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
    }

    public async Task LoadAsync()
    {
        DeviceId           = _license.GetDeviceId();
        CurrentLicense     = await _license.LoadAsync();
        SteamPath          = _steam.GetSteamBasePath() ?? "Not detected";
        SteamLibraryCount  = _steam.GetLibraryPaths().Count;
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
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Core.Constants.AppConstants.AppDataFolder);
            
            var catalogDir = Path.Combine(appDataPath, "runtime_catalog_sources");
            if (Directory.Exists(catalogDir)) { try { Directory.Delete(catalogDir, true); } catch {} }

            var filesToDel = new[] { 
                Core.Constants.AppConstants.LicenseFileName, 
                Core.Constants.AppConstants.AppliedStateFileName, 
                Core.Constants.AppConstants.SteamDataCacheFileName 
            };
            
            foreach (var f in filesToDel)
            {
                var fp = Path.Combine(appDataPath, f);
                if (File.Exists(fp)) { try { File.Delete(fp); } catch {} }
            }

            _log.Log("Settings", "All local data cleared. Restarting...");
        }
        catch {}

        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
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
