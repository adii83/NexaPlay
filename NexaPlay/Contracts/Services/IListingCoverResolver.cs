namespace NexaPlay.Contracts.Services;

public interface IListingCoverResolver
{
    Task<string?> ResolveAsync(
        int appId,
        string? runtimeCapsule,
        string? header,
        CancellationToken ct = default);

    void ClearCache();
}
