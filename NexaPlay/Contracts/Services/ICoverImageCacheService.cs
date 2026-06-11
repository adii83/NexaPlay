namespace NexaPlay.Contracts.Services;

public interface ICoverImageCacheService
{
    Task<string?> GetCachedOrFetchAsync(int appId, string? sourceUrl, CancellationToken ct = default);
    Task ClearCacheAsync();
}
