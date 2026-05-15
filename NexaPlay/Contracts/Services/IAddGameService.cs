using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IAddGameService
{
    bool IsGameInstalled(string appId);
    IReadOnlyList<string> ListLibraryGames();
    Task AddGameAsync(string appId, IProgress<FixProgressState> progress, CancellationToken ct = default);
    Task RemoveGameAsync(string appId);
}
