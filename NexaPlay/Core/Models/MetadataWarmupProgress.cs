namespace NexaPlay.Core.Models;

public sealed class MetadataWarmupProgress
{
    public string FileName { get; init; } = string.Empty;
    public string Stage { get; init; } = string.Empty;
    public int CompletedFiles { get; init; }
    public int TotalFiles { get; init; }
    public double? FilePercent { get; init; }
    public string Message { get; init; } = string.Empty;
}
