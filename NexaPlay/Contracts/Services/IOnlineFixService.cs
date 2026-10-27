using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IOnlineFixService
{
    Task RefreshCatalogAsync(CancellationToken ct = default);
    Task<bool> CheckAvailabilityAsync(int appId, CancellationToken ct = default);
    Task<OnlineFixResult> ApplyAsync(int appId, IProgress<BypassProgressState> progress, CancellationToken ct = default);
    Task<OnlineFixResult> UnfixAsync(int appId, CancellationToken ct = default);
    bool IsApplied(int appId);
}
