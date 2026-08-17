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

    private readonly IAppLogService _log;
    private readonly HttpClient _http;
    private readonly string _cacheDir;
    private readonly SemaphoreSlim _downloadGate = new(6, 6);
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _downloads = new(StringComparer.OrdinalIgnoreCase);

    public CoverImageCacheService(IAppLogService log)
    {
        _log = log;
        _http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NexaPlay/1.0");

        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder,
            "runtime_catalog_sources",
            "cover_files");

        Directory.CreateDirectory(_cacheDir);
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
                () => DownloadAndReleaseAsync(appId, sourceUrl, remoteUri, localPath),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return await shared.Value.WaitAsync(ct);
    }

    private async Task<string?> DownloadAndReleaseAsync(int appId, string sourceUrl, Uri remoteUri, string localPath)
    {
        try
        {
            return await DownloadAsync(appId, sourceUrl, remoteUri, localPath);
        }
        finally
        {
            _downloads.TryRemove(localPath, out _);
        }
    }

    private async Task<string?> DownloadAsync(int appId, string sourceUrl, Uri remoteUri, string localPath)
    {
        using var timeout = new CancellationTokenSource(DownloadTimeout);
        var gateEntered = false;
        try
        {
            await _downloadGate.WaitAsync(timeout.Token);
            gateEntered = true;

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

                File.Move(tempPath, localPath, overwrite: true);
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
        }

        Directory.CreateDirectory(_cacheDir);
        return Task.CompletedTask;
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
