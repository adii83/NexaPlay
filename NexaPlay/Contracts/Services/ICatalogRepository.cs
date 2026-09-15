using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface ICatalogRepository
{
    Task<GameEntry?> GetByAppIdAsync(int appId, CancellationToken ct = default);
    Task<IReadOnlyList<GameEntry>> GetByAppIdsAsync(IReadOnlyList<int> appIds, CancellationToken ct = default);
    Task<CatalogPage> QueryAsync(CatalogQuery query, CancellationToken ct = default);
    Task<int> GetVersionAsync(CancellationToken ct = default);
    Task ReplaceRuntimeOverridesAsync(IReadOnlyDictionary<int, CatalogRuntimeOverride> overrides, CancellationToken ct = default);
    Task ApplyPatchAsync(CatalogPatch patch, CancellationToken ct = default);
}
