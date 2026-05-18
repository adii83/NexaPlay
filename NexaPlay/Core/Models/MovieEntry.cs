namespace NexaPlay.Core.Models;

/// <summary>A trailer/movie from Steam Store API appdetails.</summary>
public sealed class MovieEntry
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    /// <summary>600×337 thumbnail image.</summary>
    public string ThumbnailUrl { get; init; } = string.Empty;

    /// <summary>HLS m3u8 stream URL (H.264). Preferred for WinUI MediaPlayerElement.</summary>
    public string? HlsUrl { get; init; }

    /// <summary>DASH mpd URL (H.264 fallback).</summary>
    public string? DashH264Url { get; init; }

    /// <summary>True if Steam marks this as a highlight trailer.</summary>
    public bool IsHighlight { get; init; }
}
