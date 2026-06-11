namespace NexaPlay.Contracts.Services;

public interface IGameCoverIndexService
{
    Task<string?> GetLibraryCapsuleAsync(int appId, CancellationToken ct = default);
    Task WarmupAsync(CancellationToken ct = default);
    Task ClearCacheAsync();
}
