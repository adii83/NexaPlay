namespace NexaPlay.Core.Models;

public sealed class AppUpdateCheckResult
{
    public string CurrentVersion { get; set; } = Core.Constants.AppConstants.AppVersion;
    public string LatestVersion { get; set; } = Core.Constants.AppConstants.AppVersion;
    public bool IsUpdateAvailable { get; set; }
    public bool Mandatory { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? InstallerUrl { get; set; }
    public string? InstallerSha256 { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
    public IReadOnlyList<string> ReleaseNotes { get; set; } = Array.Empty<string>();
}
