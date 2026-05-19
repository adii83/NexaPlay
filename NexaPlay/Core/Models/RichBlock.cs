namespace NexaPlay.Core.Models;

public enum RichBlockType
{
    Text,
    CenteredText,
    Header,
    Image,
    Video
}

public sealed class RichBlock
{
    public RichBlockType Type { get; init; }
    public string Content { get; init; } = string.Empty;
}
