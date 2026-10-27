using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IOnlineFixCatalogService
{
    Task RefreshAsync(CancellationToken ct = default);
    Task<OnlineFixEntry?> GetAsync(int appId, CancellationToken ct = default);
}
