using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IMetadataService
{
    bool IsCacheAvailable { get; }
    Task<GameEntry?> GetMetadataAsync(int appId, CancellationToken ct = default);
    Task<IReadOnlyList<GameEntry>> SearchAsync(string query, int maxResults = 50, CancellationToken ct = default);
    Task<IReadOnlyList<GameEntry>> GetCatalogSnapshotAsync(CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetAllCatalogAppIdsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetPopularAppIdsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetNewFixAppIdsAsync(CancellationToken ct = default);
    Task RefreshAsync(bool forceDownload = false, CancellationToken ct = default);
    Task WarmupEssentialSourcesAsync(IProgress<MetadataWarmupProgress>? progress = null, CancellationToken ct = default);
    Task ClearCacheAsync();
}
