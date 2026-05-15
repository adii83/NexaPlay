using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface ILicenseService
{
    Task<LicenseInfo> LoadAsync();
    Task<LicenseInfo> ValidateOnlineAsync(string licenseKey, string deviceId, CancellationToken ct = default);
    Task<LicenseInfo> ActivateAsync(string licenseKey, CancellationToken ct = default);
    Task SaveAsync(LicenseInfo info);
    Task DeactivateAsync();
    string GetDeviceId();
}
