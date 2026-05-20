using CommunityToolkit.Mvvm.ComponentModel;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly IAddGameService _addGame;
    private readonly IOnlineFixService _onlineFix;
    private readonly ISteamService _steam;
    private readonly IAppLogService _log;

    [ObservableProperty] public partial IReadOnlyList<InstalledGame> InstalledGames { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> LuaGames { get; set; }
    [ObservableProperty] public partial int FixedCount { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string AddAppId { get; set; }
    [ObservableProperty] public partial string AddStatus { get; set; }
    [ObservableProperty] public partial int AddProgress { get; set; }
    [ObservableProperty] public partial bool IsAdding { get; set; }

    public LibraryViewModel(IAddGameService addGame, IOnlineFixService onlineFix, ISteamService steam, IAppLogService log)
    {
        _addGame   = addGame;
        _onlineFix = onlineFix;
        _steam     = steam;
        _log       = log;

        // Default values for partial properties
        InstalledGames = Array.Empty<InstalledGame>();
        LuaGames       = Array.Empty<string>();
        AddAppId       = string.Empty;
        AddStatus      = string.Empty;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var installed = _steam.ScanInstalledGames();
            InstalledGames = installed.Select(g => new InstalledGame
            {
                AppId = g.AppId,
                Name = g.Name,
                InstallPath = g.InstallPath,
                FixApplied = _onlineFix.IsApplied(g.AppId),
                FixAppliedDate = g.FixAppliedDate
            }).ToList();
            FixedCount = InstalledGames.Count(g => g.FixApplied);
            LuaGames   = _addGame.ListLibraryGames();
        }
        finally { IsLoading = false; }
    }

    public async Task AddGameAsync()
    {
        if (string.IsNullOrWhiteSpace(AddAppId)) return;
        IsAdding    = true;
        AddStatus   = "Starting...";
        AddProgress = 0;

        var progress = new Progress<BypassProgressState>(state =>
        {
            AddProgress = state.Percent;
            AddStatus   = state.Phase switch
            {
                "download" => $"Downloading... {state.Percent}%",
                "validate" => "Validating...",
                "install" or "done" => state.Status == BypassStatus.Applied ? "Installed!" : state.Error ?? "Failed",
                _ => state.Message ?? state.Status.ToString()
            };
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _addGame.AddGameAsync(AddAppId.Trim(), progress, cts.Token);
        IsAdding = false;
        await LoadAsync();
    }

    public async Task RemoveGameAsync(string appId)
    {
        await _addGame.RemoveGameAsync(appId);
        await LoadAsync();
    }

    public async Task UnfixAsync(int appId)
    {
        await _onlineFix.UnfixAsync(appId);
        await LoadAsync();
    }
}
