namespace NexaPlay.Core.Enums;

public enum LicensePlan
{
    None,
    Standard,
    Premium
}

public enum LicenseStatus
{
    Unknown,
    Valid,
    Invalid,
    Banned,
    Expired,
    DeviceMismatch,
    NetworkError,
    Offline
}
