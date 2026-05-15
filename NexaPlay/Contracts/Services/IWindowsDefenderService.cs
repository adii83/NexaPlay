using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IWindowsDefenderService
{
    Task<IReadOnlyList<AntivirusInfo>> DetectAntivirusAsync();
    Task<bool> AddExclusionAsync(string path);
    Task<bool> IsPathExcludedAsync(string path);
    Task<IReadOnlyList<string>> GetExclusionsAsync();
}
