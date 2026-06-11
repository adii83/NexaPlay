using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using System.IO.Compression;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Lightweight lookup for listing covers on Home and Games surfaces.
/// Priority stays below NexaPlay override and above existing metadata/header fallbacks.
/// </summary>
public sealed class GameCoverIndexService : IGameCoverIndexService
{
    private readonly IAppLogService _log;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private readonly string _catalogDir;
    private readonly string _gzipPath;
    private readonly string _jsonPath;

    private Dictionary<int, string>? _coverIndex;

    public GameCoverIndexService(IAppLogService log)
    {
        _log = log;
        _http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NexaPlay/1.0");

        _catalogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder,
            "runtime_catalog_sources");

        _gzipPath = Path.Combine(_catalogDir, AppConstants.LibraryCapsuleIndexGzipCacheFileName);
        _jsonPath = Path.Combine(_catalogDir, AppConstants.LibraryCapsuleIndexCacheFileName);

        Directory.CreateDirectory(_catalogDir);
    }

    public async Task<string?> GetLibraryCapsuleAsync(int appId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _coverIndex is not null && _coverIndex.TryGetValue(appId, out var url)
            ? url
            : null;
    }

    public Task WarmupAsync(CancellationToken ct = default) => EnsureLoadedAsync(ct);

    public Task ClearCacheAsync()
    {
        _coverIndex = null;

        TryDelete(_gzipPath);
        TryDelete(_jsonPath);
        _log.Log("CoverIndex", "Library capsule index cache cleared");
        return Task.CompletedTask;
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_coverIndex is not null)
        {
            return;
        }

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_coverIndex is not null)
            {
                return;
            }

            if (!HasUsableLocalSource())
            {
                await SyncSourcesAsync(force: false, ct);
            }

            try
            {
                _coverIndex = await BuildIndexAsync(ct);
            }
            catch (JsonException ex)
            {
                _log.Log("CoverIndex", $"Local cover index parse failed, forcing refresh: {ex.Message}");
                await SyncSourcesAsync(force: true, ct);
                _coverIndex = await BuildIndexAsync(ct);
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private bool HasUsableLocalSource() => IsFileUsable(_gzipPath) || IsFileUsable(_jsonPath);

    private async Task SyncSourcesAsync(bool force, CancellationToken ct)
    {
        var gzipAvailable = await DownloadIfNeededSafeAsync(
            AppConstants.LibraryCapsuleIndexGzipUrl,
            _gzipPath,
            force,
            ct,
            AppConstants.LibraryCapsuleIndexGzipCacheFileName);

        if (!gzipAvailable && !IsFileUsable(_jsonPath))
        {
            var jsonAvailable = await DownloadIfNeededSafeAsync(
                AppConstants.LibraryCapsuleIndexUrl,
                _jsonPath,
                force,
                ct,
                AppConstants.LibraryCapsuleIndexCacheFileName);

            if (!jsonAvailable)
            {
                throw new IOException("Library capsule cover index unavailable.");
            }
        }
    }

    private async Task<Dictionary<int, string>> BuildIndexAsync(CancellationToken ct)
    {
        if (IsFileUsable(_gzipPath))
        {
            try
            {
                var fromGzip = await ParseIndexAsync(_gzipPath, isGzip: true, ct);
                if (fromGzip.Count > 0)
                {
                    _log.Log("CoverIndex", $"Library capsule index loaded from gzip: {fromGzip.Count:N0} appids");
                    return fromGzip;
                }
            }
            catch (Exception ex)
            {
                _log.Log("CoverIndex", $"Gzip cover index parse failed: {ex.Message}");
            }
        }

        if (IsFileUsable(_jsonPath))
        {
            var fromJson = await ParseIndexAsync(_jsonPath, isGzip: false, ct);
            if (fromJson.Count > 0)
            {
                _log.Log("CoverIndex", $"Library capsule index loaded from json: {fromJson.Count:N0} appids");
                return fromJson;
            }
        }

        throw new JsonException("Library capsule cover index is empty or invalid.");
    }

    private static async Task<Dictionary<int, string>> ParseIndexAsync(string path, bool isGzip, CancellationToken ct)
    {
        var result = new Dictionary<int, string>(capacity: 160_000);

        await using var file = File.OpenRead(path);
        await using Stream stream = isGzip ? new GZipStream(file, CompressionMode.Decompress) : file;
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out var appId) || prop.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var coverUrl = ReadCoverUrl(prop.Value);
            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                result[appId] = coverUrl;
            }
        }

        return result;
    }

    private static string? ReadCoverUrl(JsonElement node)
    {
        if (node.TryGetProperty("library_capsule", out var libraryCapsule) &&
            libraryCapsule.ValueKind == JsonValueKind.String)
        {
            return libraryCapsule.GetString();
        }

        if (node.TryGetProperty("library_capsule_2x", out var libraryCapsule2x) &&
            libraryCapsule2x.ValueKind == JsonValueKind.String)
        {
            return libraryCapsule2x.GetString();
        }

        return null;
    }

    private async Task<bool> DownloadIfNeededSafeAsync(
        string url,
        string outputPath,
        bool force,
        CancellationToken ct,
        string sourceName)
    {
        try
        {
            return await DownloadIfNeededAsync(url, outputPath, force, ct, sourceName);
        }
        catch (Exception ex)
        {
            _log.Log("CoverIndex", $"Download failed for {sourceName}: {ex.Message}");
            return IsFileUsable(outputPath);
        }
    }

    private async Task<bool> DownloadIfNeededAsync(
        string url,
        string outputPath,
        bool force,
        CancellationToken ct,
        string sourceName)
    {
        var localExists = File.Exists(outputPath);
        var localAge = localExists
            ? DateTime.UtcNow - File.GetLastWriteTimeUtc(outputPath)
            : TimeSpan.MaxValue;

        if (!force && localExists && localAge < AppConstants.SafetyNetTtl && IsFileUsable(outputPath))
        {
            return true;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var tempPath = Path.Combine(Path.GetTempPath(), "NexaPlay", $"{sourceName}.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using (var target = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await source.CopyToAsync(target, ct);
                await target.FlushAsync(ct);
            }

            var length = new FileInfo(tempPath).Length;
            if (length <= 0)
            {
                throw new IOException($"Downloaded empty file for {sourceName}");
            }

            File.Copy(tempPath, outputPath, overwrite: true);
            _log.Log("CoverIndex", $"Download ok {sourceName}: {length} bytes");
            return true;
        }
        catch
        {
            if (!IsFileUsable(outputPath))
            {
                throw;
            }

            return true;
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static bool IsFileUsable(string path)
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

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
