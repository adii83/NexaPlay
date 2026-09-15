using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface ICatalogUpdateService
{
    Task<CatalogUpdateResult> CheckAndApplyPatchesAsync(bool force = false, CancellationToken ct = default);
}
