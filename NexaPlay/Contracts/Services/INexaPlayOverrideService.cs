using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

/// <summary>
/// Provides NexaPlay-specific metadata overrides fetched from a dedicated GitHub repo.
/// Overrides are sparse: only fields present in the JSON will replace existing values.
/// Priority: nexaplay_override > override_data > steam_data.
/// </summary>
public interface INexaPlayOverrideService
{
    /// <summary>Returns the catalog override block for the given appId, or null if none.</summary>
    Task<NexaPlayCatalogOverride?> GetCatalogOverrideAsync(int appId, CancellationToken ct = default);

    /// <summary>Returns the detail override block for the given appId, or null if none.</summary>
    Task<NexaPlayDetailOverride?> GetDetailOverrideAsync(int appId, CancellationToken ct = default);

    /// <summary>Returns all appIds that have any override entry.</summary>
    Task<IReadOnlySet<int>> GetOverriddenAppIdsAsync(CancellationToken ct = default);

    /// <summary>Forces a re-download of the override file from GitHub.</summary>
    Task RefreshAsync(CancellationToken ct = default);
}
