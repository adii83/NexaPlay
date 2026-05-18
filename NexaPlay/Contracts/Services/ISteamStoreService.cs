using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

/// <summary>
/// Fetches rich game detail from the Steam Store API on-demand.
/// Results are cached per-appid on disk (TTL: 7 days) so repeated
/// opens of the same game detail page do not hit the network.
///
/// SOLID — Single Responsibility:
///   This service is only responsible for Steam Store API detail data.
///   It does NOT handle steam_data.json.gz (that is IMetadataService).
/// </summary>
public interface ISteamStoreService
{
    /// <summary>
    /// Returns rich detail for the given appId.
    /// Checks disk cache first; calls Steam API only when cache is absent or expired.
    /// Returns null when the API call fails and no cache is available.
    /// </summary>
    Task<GameDetailEntry?> GetDetailAsync(int appId, CancellationToken ct = default);

    /// <summary>Removes the cached detail file for the given appId.</summary>
    Task InvalidateCacheAsync(int appId);
}
