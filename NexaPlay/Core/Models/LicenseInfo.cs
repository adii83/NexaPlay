using NexaPlay.Core.Enums;

namespace NexaPlay.Core.Models;

public sealed class LicenseInfo
{
    public string Key { get; init; } = string.Empty;
    public LicensePlan Plan { get; init; }
    public LicenseStatus Status { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string? Message { get; init; }

    public bool IsValid => Status == LicenseStatus.Valid;
    public bool IsPremium => Plan == LicensePlan.Premium && IsValid;
}
