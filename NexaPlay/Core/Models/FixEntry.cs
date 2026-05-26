using NexaPlay.Core.Enums;

namespace NexaPlay.Core.Models;

/// <summary>A single downloadable part of a fix archive</summary>
public sealed class FixFile
{
    public int Part { get; init; }
    public string Filename { get; init; } = string.Empty;
    public string GDriveId { get; init; } = string.Empty;
    public string GDriveUrl { get; init; } = string.Empty;
}

/// <summary>A fix entry from fix_games.json or steam_games.json</summary>
public sealed class FixEntry
{
    public int AppId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Publisher { get; init; } = string.Empty;
    public GameCategory Category { get; init; }
    public string? PosterUrl { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool IsPremium { get; init; }
    public bool HasDenuvo { get; init; }
    public bool AktivasiOffline { get; init; }
    public bool DapatkanKode { get; init; }

    // Computed helpers
    public bool HasPopularCover => !string.IsNullOrWhiteSpace(PosterUrl);
    public bool IsSteamType => Category == GameCategory.SteamAccount || Category == GameCategory.SteamSharing;
    public bool ShowAktivasiOfflineBadge => AktivasiOffline;
    public bool ShowSteamSharingBadge => Category == GameCategory.SteamSharing;

    public string? ExeHint { get; init; }
    public bool UseShortcut { get; init; }
    public IReadOnlyList<FixFile> Files { get; init; } = Array.Empty<FixFile>();
    public string InstallHint => $"Usually installed at: steamapps/common/{Title}";
}
