using NexaPlay.Core.Enums;

namespace NexaPlay.Core.Models;

public sealed class BypassProgressState
{
    public int AppId { get; init; }
    public BypassStatus Status { get; init; }
    public string Phase { get; init; } = string.Empty;
    public int Percent { get; init; }
    public long BytesRead { get; init; }
    public long TotalBytes { get; init; }
    public int CurrentPart { get; init; }
    public int TotalParts { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
}

public sealed class AntivirusInfo
{
    public AntivirusVendor Vendor { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class InstalledGame
{
    public int AppId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string InstallPath { get; init; } = string.Empty;
    public bool FixApplied { get; init; }
    public DateTime? FixAppliedDate { get; init; }
}
