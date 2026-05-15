using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IFixGamesDataService
{
    Task<IReadOnlyList<FixEntry>> GetAllFixesAsync(CancellationToken ct = default);
    Task<FixEntry?> GetFixAsync(int appId, CancellationToken ct = default);
    Task RefreshAsync(CancellationToken ct = default);
}
