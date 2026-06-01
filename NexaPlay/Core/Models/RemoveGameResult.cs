namespace NexaPlay.Core.Models;

public sealed class RemoveGameResult
{
    public bool Success { get; init; }
    public bool BlockedByInstalledGame { get; init; }
    public string? Error { get; init; }
}
