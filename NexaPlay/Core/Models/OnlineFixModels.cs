namespace NexaPlay.Core.Models;

public sealed record OnlineFixPackage(string Filename, string DriveId, string DownloadUrl);

public sealed record OnlineFixEntry(int AppId, IReadOnlyList<OnlineFixPackage> Files);

public enum OnlineFixOutcome
{
    Applied,
    Removed,
    Cancelled,
    NotAvailable,
    NotInstalled,
    Failed
}

/// <summary>Operation completion is independent of asynchronously delivered UI progress.</summary>
public sealed record OnlineFixResult(OnlineFixOutcome Outcome, string Message)
{
    public bool Succeeded => Outcome is OnlineFixOutcome.Applied or OnlineFixOutcome.Removed;
}
