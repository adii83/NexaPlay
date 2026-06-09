using Microsoft.UI.Xaml;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

public sealed class AppUpdateService : IAppUpdateService
{
    private readonly HttpClient _http;
    private readonly IAppLogService _log;
    private readonly string _appDataDir;
    private readonly string _updatesDir;
    private readonly string _statePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public AppUpdateService(IAppLogService log)
    {
        _log = log;
        _http = new HttpClient
        {
            Timeout = AppConstants.HttpDefaultTimeout
        };

        _appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        _updatesDir = Path.Combine(_appDataDir, AppConstants.AppUpdateDownloadsFolderName);
        _statePath = Path.Combine(_appDataDir, AppConstants.AppUpdateStateFileName);

        Directory.CreateDirectory(_appDataDir);
        Directory.CreateDirectory(_updatesDir);
    }

    public string CurrentVersion => AppConstants.AppVersion;

    public async Task<AppUpdateCheckResult> GetCachedStatusAsync()
    {
        var state = await ReadStateAsync();
        if (state is null)
        {
            return new AppUpdateCheckResult
            {
                CurrentVersion = CurrentVersion,
                LatestVersion = CurrentVersion,
                Message = "Belum pernah memeriksa update."
            };
        }

        return ToResult(state);
    }

    public async Task<AppUpdateCheckResult> CheckForUpdatesAsync(bool force = false, CancellationToken ct = default)
    {
        var cached = await ReadStateAsync();
        if (!force
            && cached?.LastCheckedAt is DateTimeOffset lastCheckedAt
            && DateTimeOffset.UtcNow - lastCheckedAt < AppConstants.AppUpdateCheckCooldown)
        {
            _log.Log("Update", $"Menggunakan cache update. LastChecked={lastCheckedAt:O}");
            return ToResult(cached);
        }

        try
        {
            _log.Log("Update", $"Memeriksa manifest update dari {AppConstants.AppUpdateManifestUrl}");
            using var response = await _http.GetAsync(AppConstants.AppUpdateManifestUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var manifest = JsonSerializer.Deserialize<AppUpdateManifest>(json, _jsonOptions)
                           ?? throw new InvalidOperationException("Manifest update tidak valid.");

            if (string.IsNullOrWhiteSpace(manifest.Version))
            {
                throw new InvalidOperationException("Manifest update tidak memiliki field version.");
            }

            if (string.IsNullOrWhiteSpace(manifest.InstallerUrl))
            {
                throw new InvalidOperationException("Manifest update tidak memiliki field installerUrl.");
            }

            var currentComparable = ToComparableVersion(CurrentVersion);
            var latestComparable = ToComparableVersion(manifest.Version);
            var isUpdateAvailable = latestComparable > currentComparable;

            var result = new AppUpdateCheckResult
            {
                CurrentVersion = CurrentVersion,
                LatestVersion = manifest.Version.Trim(),
                IsUpdateAvailable = isUpdateAvailable,
                Mandatory = manifest.Mandatory,
                InstallerUrl = manifest.InstallerUrl?.Trim(),
                InstallerSha256 = manifest.InstallerSha256?.Trim(),
                PublishedAt = manifest.PublishedAt,
                LastCheckedAt = DateTimeOffset.UtcNow,
                ReleaseNotes = (manifest.ReleaseNotes ?? new List<string>())
                    .Where(note => !string.IsNullOrWhiteSpace(note))
                    .Select(note => note.Trim())
                    .ToList(),
                Message = isUpdateAvailable
                    ? $"Versi {manifest.Version.Trim()} tersedia untuk diunduh."
                    : "Anda sudah menggunakan versi terbaru."
            };

            await WriteStateAsync(ToState(result));
            _log.Log("Update", $"Hasil cek update: current={result.CurrentVersion}, latest={result.LatestVersion}, available={result.IsUpdateAvailable}");
            return result;
        }
        catch (Exception ex)
        {
            _log.Log("Update", $"Cek update gagal: {ex.Message}");
            return new AppUpdateCheckResult
            {
                CurrentVersion = CurrentVersion,
                LatestVersion = cached?.LatestVersion ?? CurrentVersion,
                IsUpdateAvailable = cached?.IsUpdateAvailable ?? false,
                Mandatory = cached?.Mandatory ?? false,
                InstallerUrl = cached?.InstallerUrl,
                InstallerSha256 = cached?.InstallerSha256,
                PublishedAt = cached?.PublishedAt,
                LastCheckedAt = cached?.LastCheckedAt,
                ReleaseNotes = cached?.ReleaseNotes is { } cachedNotes ? cachedNotes : Array.Empty<string>(),
                Message = $"Gagal memeriksa update: {ex.Message}"
            };
        }
    }

    public async Task<string> DownloadInstallerAsync(AppUpdateCheckResult update, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(update.InstallerUrl))
        {
            throw new InvalidOperationException("URL installer update belum tersedia.");
        }

        var installerUri = new Uri(update.InstallerUrl, UriKind.Absolute);
        var targetFileName = Path.GetFileName(installerUri.LocalPath);
        if (string.IsNullOrWhiteSpace(targetFileName))
        {
            targetFileName = $"NexaPlaySetup-{update.LatestVersion}.exe";
        }

        var finalPath = Path.Combine(_updatesDir, targetFileName);
        var tempPath = finalPath + ".download";

        _log.Log("Update", $"Mengunduh installer update ke {finalPath}");

        using var response = await _http.GetAsync(installerUri, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        progress?.Report(0);

        await using (var source = await response.Content.ReadAsStreamAsync(ct))
        await using (var destination = File.Create(tempPath))
        {
            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;

                if (totalBytes is > 0)
                {
                    progress?.Report((double)totalRead / totalBytes.Value * 100d);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(update.InstallerSha256))
        {
            var actualHash = await ComputeSha256Async(tempPath, ct);
            if (!string.Equals(actualHash, update.InstallerSha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(tempPath);
                throw new InvalidOperationException("Hash installer update tidak cocok. Download dibatalkan.");
            }
        }

        if (File.Exists(finalPath))
        {
            File.Delete(finalPath);
        }

        File.Move(tempPath, finalPath);
        progress?.Report(100);
        _log.Log("Update", $"Installer update selesai diunduh: {finalPath}");
        return finalPath;
    }

    public async Task LaunchInstallerAndExitAsync(string installerPath, CancellationToken ct = default)
    {
        if (!File.Exists(installerPath))
        {
            throw new FileNotFoundException("Installer update tidak ditemukan.", installerPath);
        }

        var currentExePath = Environment.ProcessPath
                             ?? Process.GetCurrentProcess().MainModule?.FileName
                             ?? throw new InvalidOperationException("Path aplikasi aktif tidak ditemukan.");
        var currentPid = Environment.ProcessId;
        var scriptPath = Path.Combine(_updatesDir, "run-update.ps1");

        var script = BuildUpdaterScript(currentPid, installerPath, currentExePath);
        await File.WriteAllTextAsync(scriptPath, script, Encoding.UTF8, ct);

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _updatesDir
        };

        var helperProcess = Process.Start(startInfo);
        if (helperProcess is null)
        {
            throw new InvalidOperationException("Helper updater gagal dijalankan.");
        }

        _log.Log("Update", $"Helper updater dijalankan. PID={currentPid}, installer={installerPath}");
        await Task.Delay(300, ct);
        Application.Current.Exit();
    }

    private async Task<AppUpdateState?> ReadStateAsync()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(_statePath);
            return JsonSerializer.Deserialize<AppUpdateState>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _log.Log("Update", $"Baca state update gagal: {ex.Message}");
            return null;
        }
    }

    private async Task WriteStateAsync(AppUpdateState state)
    {
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        await File.WriteAllTextAsync(_statePath, json);
    }

    private static AppUpdateCheckResult ToResult(AppUpdateState state)
    {
        return new AppUpdateCheckResult
        {
            CurrentVersion = string.IsNullOrWhiteSpace(state.CurrentVersion) ? AppConstants.AppVersion : state.CurrentVersion,
            LatestVersion = string.IsNullOrWhiteSpace(state.LatestVersion) ? AppConstants.AppVersion : state.LatestVersion,
            IsUpdateAvailable = state.IsUpdateAvailable,
            Mandatory = state.Mandatory,
            Message = state.Message,
            InstallerUrl = state.InstallerUrl,
            InstallerSha256 = state.InstallerSha256,
            PublishedAt = state.PublishedAt,
            LastCheckedAt = state.LastCheckedAt,
            ReleaseNotes = state.ReleaseNotes
        };
    }

    private static AppUpdateState ToState(AppUpdateCheckResult result)
    {
        return new AppUpdateState
        {
            CurrentVersion = result.CurrentVersion,
            LatestVersion = result.LatestVersion,
            IsUpdateAvailable = result.IsUpdateAvailable,
            Mandatory = result.Mandatory,
            Message = result.Message,
            InstallerUrl = result.InstallerUrl,
            InstallerSha256 = result.InstallerSha256,
            PublishedAt = result.PublishedAt,
            LastCheckedAt = result.LastCheckedAt,
            ReleaseNotes = result.ReleaseNotes.ToList()
        };
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    private static Version ToComparableVersion(string versionText)
    {
        var sanitized = versionText.Trim();
        if (sanitized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = sanitized[1..];
        }

        var dashIndex = sanitized.IndexOf('-');
        if (dashIndex >= 0)
        {
            sanitized = sanitized[..dashIndex];
        }

        var parts = sanitized.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Take(4)
            .Select(part => int.TryParse(part, out var value) ? value : 0)
            .ToList();

        while (parts.Count < 4)
        {
            parts.Add(0);
        }

        return new Version(parts[0], parts[1], parts[2], parts[3]);
    }

    private static string BuildUpdaterScript(int parentPid, string installerPath, string restartPath)
    {
        var escapedInstaller = EscapePowerShellString(installerPath);
        var escapedRestart = EscapePowerShellString(restartPath);
        var escapedArgs = EscapePowerShellString(AppConstants.AppUpdateInstallerArguments);

        return $$"""
$ErrorActionPreference = 'SilentlyContinue'
$parentPid = {{parentPid}}
$installerPath = '{{escapedInstaller}}'
$installerArgs = '{{escapedArgs}}'
$restartPath = '{{escapedRestart}}'
$deadline = [DateTime]::UtcNow.AddMinutes(10)

while (Get-Process -Id $parentPid -ErrorAction SilentlyContinue) {
    if ([DateTime]::UtcNow -gt $deadline) { break }
    Start-Sleep -Milliseconds 750
}

if (-not (Test-Path $installerPath)) { exit 1 }

$process = Start-Process -FilePath $installerPath -ArgumentList $installerArgs -Wait -PassThru
if ($process.ExitCode -eq 0 -and (Test-Path $restartPath)) {
    Start-Process -FilePath $restartPath
}
""";
    }

    private static string EscapePowerShellString(string value)
        => value.Replace("'", "''");
}
