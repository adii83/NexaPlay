using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IDynamicFeedRefreshService
{
    string GetLocalPath(DynamicFeedKind feed);
    Task<DynamicFeedRefreshResult> RefreshAsync(bool force = false, CancellationToken ct = default);
}
