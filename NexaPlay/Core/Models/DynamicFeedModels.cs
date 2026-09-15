namespace NexaPlay.Core.Models;

public enum DynamicFeedKind
{
    Popular,
    NewFix,
    FixGames,
    SteamGames
}

[Flags]
public enum DynamicFeedChanges
{
    None = 0,
    Popular = 1,
    NewFix = 2,
    FixGames = 4,
    SteamGames = 8
}

public sealed record DynamicFeedRefreshResult(
    DynamicFeedChanges Changes,
    int Requested,
    int NotModified,
    int Failed,
    bool Suppressed = false);

public sealed record MetadataRefreshResult(
    DynamicFeedRefreshResult Feeds,
    bool OverrideChanged,
    bool OverlayChanged);
