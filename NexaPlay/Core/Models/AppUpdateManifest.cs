namespace NexaPlay.Core.Models;

public sealed class AppUpdateManifest
{
    public string? Version { get; set; }
    public string? InstallerUrl { get; set; }
    public string? InstallerSha256 { get; set; }
    public List<string>? ReleaseNotes { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public bool Mandatory { get; set; }
}
