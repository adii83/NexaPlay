using CommunityToolkit.Mvvm.ComponentModel;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class FixGamesViewModel : ObservableObject
{
    private readonly IFixGamesDataService _fixData;
    private readonly IOnlineFixService _onlineFix;
    private readonly IWindowsDefenderService _defender;
    private readonly ISteamService _steam;
    private readonly IAppLogService _log;

    // Catalog
    [ObservableProperty] private IReadOnlyList<FixEntry> _fixes = Array.Empty<FixEntry>();
    [ObservableProperty] private IReadOnlyList<FixEntry> _filteredFixes = Array.Empty<FixEntry>();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchQuery = string.Empty;

    // AV
    [ObservableProperty] private IReadOnlyList<AntivirusInfo> _antivirusList = Array.Empty<AntivirusInfo>();
    [ObservableProperty] private bool _isScanning;

    // Fix progress
    [ObservableProperty] private FixEntry? _selectedFix;
    [ObservableProperty] private bool _isApplying;
    [ObservableProperty] private int _fixPercent;
    [ObservableProperty] private string _fixPhase = string.Empty;
    [ObservableProperty] private string _fixStatusMessage = string.Empty;
    [ObservableProperty] private FixStatus _currentFixStatus = FixStatus.Unknown;

    private CancellationTokenSource? _fixCts;

    public FixGamesViewModel(
        IFixGamesDataService fixData, IOnlineFixService onlineFix,
        IWindowsDefenderService defender, ISteamService steam, IAppLogService log)
    {
        _fixData   = fixData;
        _onlineFix = onlineFix;
        _defender  = defender;
        _steam     = steam;
        _log       = log;
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
        _log.Log("FixGames", "Excluding path...");
        await _defender.AddExclusionAsync(path);
    }

    public async Task ApplyFixAsync(FixEntry fix)
    {
        if (IsApplying) return;
        SelectedFix      = fix;
        IsApplying       = true;
        FixPercent       = 0;
        CurrentFixStatus = FixStatus.Downloading;
        FixStatusMessage = "Checking availability...";

        _fixCts = new CancellationTokenSource();
        var available = await _onlineFix.CheckAvailabilityAsync(fix.AppId, _fixCts.Token);
        if (!available)
        {
            FixStatusMessage = "Fix not available for this game";
            CurrentFixStatus = FixStatus.NotAvailable;
            IsApplying       = false;
            return;
        }

        var progress = new Progress<FixProgressState>(state =>
        {
            FixPercent       = state.Percent < 0 ? FixPercent : state.Percent;
            CurrentFixStatus = state.Status;
            FixPhase         = state.Phase;
            FixStatusMessage = state.Phase switch
            {
                "download" => $"Downloading... {state.Percent}%",
                "extract"  => "Extracting files...",
                "done"     => state.Status == FixStatus.Applied ? "Fix applied!" : state.Error ?? "Failed",
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
        FixStatusMessage = "Cancelled";
        CurrentFixStatus = FixStatus.Cancelled;
    }

    public async Task UnfixAsync(int appId)
    {
        await _onlineFix.UnfixAsync(appId);
        FixStatusMessage = "Fix removed";
        CurrentFixStatus = FixStatus.Unknown;
    }

    public bool IsFixApplied(int appId) => _onlineFix.IsApplied(appId);

    partial void OnSearchQueryChanged(string value)
    {
        FilteredFixes = string.IsNullOrWhiteSpace(value)
            ? Fixes
            : Fixes.Where(f => f.Title.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
