namespace NexaPlay.Core.Models;

public sealed class BypassTutorialVideo
{
    public static readonly BypassTutorialVideo Empty = new();

    public string VideoId { get; init; } = string.Empty;
    public string Title { get; init; } = "Tutorial Video";
    public string EmbedUrl { get; init; } = string.Empty;
    public string WatchUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;

    public bool IsValid => !string.IsNullOrWhiteSpace(EmbedUrl);
}
