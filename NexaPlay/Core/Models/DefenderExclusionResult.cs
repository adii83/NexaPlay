namespace NexaPlay.Core.Models;

public sealed class DefenderExclusionResult
{
    public bool Success { get; set; }
    public bool NeedsAdmin { get; set; }
    public bool DefenderMissing { get; set; }
    public string? Error { get; set; }
}

