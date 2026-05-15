using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface ISteamService
{
    string? GetSteamBasePath();
    IReadOnlyList<string> GetLibraryPaths();
    IReadOnlyList<InstalledGame> ScanInstalledGames();
    string? ResolveGameInstallPath(int appId);
    string? GetGameName(int appId);
    Task RestartAsync();
}
