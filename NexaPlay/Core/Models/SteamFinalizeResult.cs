namespace NexaPlay.Core.Models;

public sealed record SteamFinalizeResult(
    bool ManifestLocked,
    bool LaunchOptionApplied,
    string? Warning);
