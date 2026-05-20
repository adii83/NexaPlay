using CommunityToolkit.Mvvm.ComponentModel;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class BypassGamesViewModel : ObservableObject
{
    private readonly IBypassGamesDataService _fixData;
    private readonly IOnlineFixService _onlineFix;
    private readonly IWindowsDefenderService _defender;
    private readonly ISteamService _steam;
    private readonly IAppLogService _log;

    // Catalog
    [ObservableProperty] public partial IReadOnlyList<FixEntry> Fixes { get; set; }
    [ObservableProperty] public partial IReadOnlyList<FixEntry> FilteredFixes { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string SearchQuery { get; set; }

    // AV
    [ObservableProperty] public partial IReadOnlyList<AntivirusInfo> AntivirusList { get; set; }
    [ObservableProperty] public partial bool IsScanning { get; set; }

    // Fix progress
    [ObservableProperty] public partial FixEntry? SelectedFix { get; set; }
    [ObservableProperty] public partial bool IsApplying { get; set; }
    [ObservableProperty] public partial int FixPercent { get; set; }
    [ObservableProperty] public partial string FixPhase { get; set; }
    [ObservableProperty] public partial string BypassStatusMessage { get; set; }
    [ObservableProperty] public partial BypassStatus CurrentBypassStatus { get; set; }

    private CancellationTokenSource? _fixCts;

    public BypassGamesViewModel(
        IBypassGamesDataService fixData, IOnlineFixService onlineFix,
        IWindowsDefenderService defender, ISteamService steam, IAppLogService log)
    {
        _fixData   = fixData;
        _onlineFix = onlineFix;
        _defender  = defender;
        _steam     = steam;
        _log       = log;

        // Default values for partial properties
        Fixes               = Array.Empty<FixEntry>();
        FilteredFixes       = Array.Empty<FixEntry>();
        SearchQuery         = string.Empty;
        AntivirusList       = Array.Empty<AntivirusInfo>();
        FixPhase            = string.Empty;
        BypassStatusMessage = string.Empty;
        CurrentBypassStatus = BypassStatus.Unknown;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Fixes         = await _fixData.GetAllFixesAsync();
            FilteredFixes = Fixes;
        }
        finally { IsLoading = false; }
    }

    public async Task ScanSystemAsync()
    {
        IsScanning    = true;
        AntivirusList = await _defender.DetectAntivirusAsync();
        IsScanning    = false;
    }

    public async Task ExcludePathAsync(string path)
    {
        _log.Log("BypassGames", "Excluding path...");
        await _defender.AddExclusionAsync(path);
    }

    public async Task ApplyFixAsync(FixEntry fix)
    {
        if (IsApplying) return;
        SelectedFix      = fix;
        IsApplying       = true;
        FixPercent       = 0;
        CurrentBypassStatus = BypassStatus.Downloading;
        BypassStatusMessage = "Checking availability...";

        _fixCts = new CancellationTokenSource();
        var available = await _onlineFix.CheckAvailabilityAsync(fix.AppId, _fixCts.Token);
        if (!available)
        {
            BypassStatusMessage = "Fix not available for this game";
            CurrentBypassStatus = BypassStatus.NotAvailable;
            IsApplying       = false;
            return;
        }

        var progress = new Progress<BypassProgressState>(state =>
        {
            FixPercent       = state.Percent < 0 ? FixPercent : state.Percent;
            CurrentBypassStatus = state.Status;
            FixPhase         = state.Phase;
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
        IsApplying       = false;
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

    partial void OnSearchQueryChanged(string value)
    {
        FilteredFixes = string.IsNullOrWhiteSpace(value)
            ? Fixes
            : Fixes.Where(f => f.Title.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
