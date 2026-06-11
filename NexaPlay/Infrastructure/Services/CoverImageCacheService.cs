using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using System.Security.Cryptography;
using System.Text;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Stores actual cover image files on disk so list cards can render from local files
/// after startup warmup instead of relying on a fresh online image fetch each time.
/// </summary>
public sealed class CoverImageCacheService : ICoverImageCacheService
{
    private readonly IAppLogService _log;
    private readonly HttpClient _http;
    private readonly string _cacheDir;
    private readonly SemaphoreSlim _downloadGate = new(6, 6);

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
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return sourceUrl;
        }

        if (!TryBuildRemoteUri(sourceUrl, out var remoteUri))
        {
            return sourceUrl;
        }

        var localPath = BuildCachePath(appId, remoteUri);
        if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
        {
            return localPath;
        }

        await _downloadGate.WaitAsync(ct);
        try
        {
            if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
            {
                return localPath;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            var tempPath = localPath + ".tmp";

            try
            {
                using var response = await _http.GetAsync(remoteUri, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                await using var source = await response.Content.ReadAsStreamAsync(ct);
                await using (var target = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await source.CopyToAsync(target, ct);
                    await target.FlushAsync(ct);
                }

                if (!File.Exists(tempPath) || new FileInfo(tempPath).Length <= 0)
                {
                    return sourceUrl;
                }

                File.Copy(tempPath, localPath, overwrite: true);
                return localPath;
            }
            catch (Exception ex)
            {
                _log.Log("CoverCache", $"Cover download failed appId={appId}: {ex.Message}");
                return sourceUrl;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                }
            }
        }
        finally
        {
            _downloadGate.Release();
        }
    }

    public Task ClearCacheAsync()
    {
        try
        {
            if (Directory.Exists(_cacheDir))
            {
                Directory.Delete(_cacheDir, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _log.Log("CoverCache", $"Clear cache failed: {ex.Message}");
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
