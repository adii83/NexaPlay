namespace NexaPlay.Core.Models;

public sealed record OnlineFixPackage(string Filename, string DriveId, string DownloadUrl);

public sealed record OnlineFixEntry(int AppId, IReadOnlyList<OnlineFixPackage> Files, string? Password = null)
{
    public const string DefaultArchivePassword = "online-fix.me";

    // Supplying a password to an unencrypted archive is harmless. An explicit value is never
    // retried with the default, so a mistaken mapping cannot silently select another password.
    public string ArchivePassword => string.IsNullOrEmpty(Password) ? DefaultArchivePassword : Password;

    public override string ToString() => $"OnlineFixEntry {{ AppId = {AppId}, Files = {Files.Count} }}";
}

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
