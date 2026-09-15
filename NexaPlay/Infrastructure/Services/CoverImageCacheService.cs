using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Stores actual cover image files on disk so list cards can render from local files
/// after startup warmup instead of relying on a fresh online image fetch each time.
/// </summary>
public sealed class CoverImageCacheService : ICoverImageCacheService
{
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(60);
    private const long HardCapBytes = 250L * 1024 * 1024;
    private const long TrimTargetBytes = 200L * 1024 * 1024;

    private readonly IAppLogService _log;
    private readonly HttpClient _http;
    private readonly string _cacheDir;
    private readonly SemaphoreSlim _downloadGate = new(6, 6);
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _downloads = new(StringComparer.OrdinalIgnoreCase);
    private long _cacheEpoch;
    private int _writesSinceMaintenance;
    private int _maintenanceRunning;

    public CoverImageCacheService(IAppLogService log)
        : this(log, CreateHttpClient(), Path.Combine(AppConstants.AppDataPath, "runtime_catalog_sources", "cover_files"))
    {
    }

    internal CoverImageCacheService(IAppLogService log, HttpClient http, string cacheDir)
    {
        _log = log;
        _http = http;
        _cacheDir = cacheDir;

        Directory.CreateDirectory(_cacheDir);
        ScheduleMaintenance(force: true);
    }

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("NexaPlay/1.0");
        return http;
    }

    public async Task<string?> GetCachedOrFetchAsync(int appId, string? sourceUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl) || !TryBuildRemoteUri(sourceUrl, out var remoteUri))
            return sourceUrl;

        var localPath = BuildCachePath(appId, remoteUri);
        if (IsUsable(localPath))
            return localPath;

        var shared = _downloads.GetOrAdd(
            localPath,
            _ => new Lazy<Task<string?>>(
                () => DownloadAndReleaseAsync(
                    appId, sourceUrl, remoteUri, localPath, Volatile.Read(ref _cacheEpoch)),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return await shared.Value.WaitAsync(ct);
    }

    private async Task<string?> DownloadAndReleaseAsync(
        int appId,
        string sourceUrl,
        Uri remoteUri,
        string localPath,
        long cacheEpoch)
    {
        try
        {
            return await DownloadAsync(appId, sourceUrl, remoteUri, localPath, cacheEpoch);
        }
        finally
        {
            _downloads.TryRemove(localPath, out _);
        }
    }

    private async Task<string?> DownloadAsync(
        int appId,
        string sourceUrl,
        Uri remoteUri,
        string localPath,
        long cacheEpoch)
    {
        using var timeout = new CancellationTokenSource(DownloadTimeout);
        var gateEntered = false;
        try
        {
            await _downloadGate.WaitAsync(timeout.Token);
            gateEntered = true;

            if (cacheEpoch != Volatile.Read(ref _cacheEpoch))
                return sourceUrl;

            if (IsUsable(localPath))
                return localPath;

            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            var tempPath = $"{localPath}.{Guid.NewGuid():N}.tmp";

            try
            {
                using var response = await _http.GetAsync(remoteUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                response.EnsureSuccessStatusCode();

                await using var source = await response.Content.ReadAsStreamAsync(timeout.Token);
                await using (var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await source.CopyToAsync(target, timeout.Token);
                    await target.FlushAsync(timeout.Token);
                }

                if (!IsUsable(tempPath))
                    return sourceUrl;

                if (cacheEpoch != Volatile.Read(ref _cacheEpoch))
                    return sourceUrl;

                File.Move(tempPath, localPath, overwrite: true);
                ScheduleMaintenance();
                return localPath;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                }
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            _log.Log("CoverCache", $"Cover download timed out appId={appId}");
            return sourceUrl;
        }
        catch (Exception ex)
        {
            _log.Log("CoverCache", $"Cover download failed appId={appId}: {ex.Message}");
            return sourceUrl;
        }
        finally
        {
            if (gateEntered)
                _downloadGate.Release();
        }
    }

    private static bool IsUsable(string path)
    {
        try
        {
            return File.Exists(path) && new FileInfo(path).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public Task ClearCacheAsync()
    {
        Interlocked.Increment(ref _cacheEpoch);
        _downloads.Clear();

        try
        {
            if (Directory.Exists(_cacheDir))
            {
                var tombstone = $"{_cacheDir}.tombstone.{Guid.NewGuid():N}";
                Directory.Move(_cacheDir, tombstone);
                // ponytail: fire-and-forget delete; upgrade to hosted background service if needed
                _ = Task.Run(() => { try { Directory.Delete(tombstone, recursive: true); } catch { } });
            }
        }
        catch (Exception ex)
        {
            _log.Log("CoverCache", $"Clear cache detach failed: {ex.Message}");
            if (Directory.Exists(_cacheDir))
            {
                foreach (var file in Directory.EnumerateFiles(_cacheDir))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

        Directory.CreateDirectory(_cacheDir);
        return Task.CompletedTask;
    }

    private void ScheduleMaintenance(bool force = false)
    {
        if (!force && Interlocked.Increment(ref _writesSinceMaintenance) < 20)
            return;
        if (Interlocked.Exchange(ref _maintenanceRunning, 1) != 0)
            return;

        Interlocked.Exchange(ref _writesSinceMaintenance, 0);
        _ = Task.Run(() =>
        {
            try { TrimDirectory(_cacheDir, HardCapBytes, TrimTargetBytes); }
            catch (Exception ex) { _log.Log("CoverCache", $"Maintenance failed: {ex.Message}"); }
            finally { Volatile.Write(ref _maintenanceRunning, 0); }
        });
    }

    public static void TrimDirectory(string directory, long hardCapBytes, long targetBytes)
    {
        if (!Directory.Exists(directory))
            return;

        var files = new DirectoryInfo(directory).EnumerateFiles()
            .Where(file => !file.Name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file.LastWriteTimeUtc)
            .ToList();
        var total = files.Sum(file => file.Length);
        if (total <= hardCapBytes)
            return;

        foreach (var file in files)
        {
            if (total <= targetBytes)
                break;
            try
            {
                var length = file.Length;
                file.Delete();
                total -= length;
            }
            catch
            {
            }
        }
    }

    private string BuildCachePath(int appId, Uri remoteUri)
    {
        var extension = Path.GetExtension(remoteUri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 8)
        {
            extension = ".img";
        }

        var urlHash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(remoteUri.AbsoluteUri))).ToLowerInvariant();
        return Path.Combine(_cacheDir, $"{appId}_{urlHash}{extension}");
    }

    private static bool TryBuildRemoteUri(string raw, out Uri uri)
    {
        if (Uri.TryCreate(raw, UriKind.Absolute, out uri!) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        return false;
    }
}
