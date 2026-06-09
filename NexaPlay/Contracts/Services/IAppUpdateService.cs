using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IAppUpdateService
{
    string CurrentVersion { get; }
    Task<AppUpdateCheckResult> GetCachedStatusAsync();
    Task<AppUpdateCheckResult> CheckForUpdatesAsync(bool force = false, CancellationToken ct = default);
    Task<string> DownloadInstallerAsync(AppUpdateCheckResult update, IProgress<double>? progress = null, CancellationToken ct = default);
    Task LaunchInstallerAndExitAsync(string installerPath, CancellationToken ct = default);
}
