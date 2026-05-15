using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IOnlineFixService
{
    Task<bool> CheckAvailabilityAsync(int appId, CancellationToken ct = default);
    Task ApplyAsync(int appId, IProgress<BypassProgressState> progress, CancellationToken ct = default);
    Task UnfixAsync(int appId, CancellationToken ct = default);
    bool IsApplied(int appId);
}
