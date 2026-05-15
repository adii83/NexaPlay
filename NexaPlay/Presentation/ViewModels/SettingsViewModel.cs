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

    // License
    [ObservableProperty] private LicenseInfo? _currentLicense;
    [ObservableProperty] private string _licenseKeyInput = string.Empty;
    [ObservableProperty] private bool _isActivating;
    [ObservableProperty] private string _activationMessage = string.Empty;
    [ObservableProperty] private bool _activationSuccess;

    // Steam
    [ObservableProperty] private string _steamPath = "Not detected";
    [ObservableProperty] private int _steamLibraryCount;

    // Defender
    [ObservableProperty] private IReadOnlyList<AntivirusInfo> _antivirusList = Array.Empty<AntivirusInfo>();
    [ObservableProperty] private IReadOnlyList<string> _exclusions = Array.Empty<string>();
    [ObservableProperty] private bool _isDetecting;

    // System
    [ObservableProperty] private string _deviceId = string.Empty;

    public SettingsViewModel(
        ILicenseService license, ISteamService steam,
        IWindowsDefenderService defender, IMetadataService metadata,
        IBypassGamesDataService fixData, IAppLogService log)
    {
        _license  = license;
        _steam    = steam;
        _defender = defender;
        _metadata = metadata;
        _fixData  = fixData;
        _log      = log;
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

    public async Task ClearMetadataCacheAsync()
    {
        await _metadata.ClearCacheAsync();
        ActivationMessage = "Metadata cache cleared.";
        _log.Log("Settings", "Metadata cache cleared");
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
