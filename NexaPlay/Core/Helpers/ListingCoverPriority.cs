namespace NexaPlay.Core.Helpers;

public static class ListingCoverPriority
{
    public static string? Select(
        string? overrideCapsule,
        string? indexedCapsule,
        string? runtimeCapsule,
        string? r2Capsule,
        string? header)
    {
        if (!string.IsNullOrWhiteSpace(overrideCapsule)) return overrideCapsule;
        if (!string.IsNullOrWhiteSpace(indexedCapsule)) return indexedCapsule;
        if (!string.IsNullOrWhiteSpace(runtimeCapsule)) return runtimeCapsule;
        if (!string.IsNullOrWhiteSpace(r2Capsule)) return r2Capsule;
        return string.IsNullOrWhiteSpace(header) ? null : header;
    }
}
