using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Models;
using System.IO.Compression;
using System.Net;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Lightweight runtime catalog for list surfaces.
/// Source order: steam_data.json.gz -> steam_data.json -> override_data.json.
/// Detail metadata is resolved on demand by ISteamStoreService.
/// </summary>
public sealed class MetadataService : IMetadataService
{
    private readonly IAppLogService _log;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly SemaphoreSlim _sourceSyncLock = new(1, 1);
    private readonly SemaphoreSlim _backgroundUpdateLock = new(1, 1);

    private readonly string _catalogDir;
    private readonly string _steamDataGzFile;
    private readonly string _steamDataFile;
    private readonly string _overrideDataFile;
    private readonly string _fixGamesFile;
    private readonly string _newFixGamesFile;
    private readonly string _steamGamesFile;

    private Dictionary<int, GameEntry>? _index;
    private DateTime _lastLoaded = DateTime.MinValue;
    private IReadOnlyList<int>? _popularAppIdsCache;
    private DateTime _popularLastLoaded = DateTime.MinValue;
    private IReadOnlyList<int>? _newFixAppIdsCache;
    private DateTime _newFixLastLoaded = DateTime.MinValue;
    private string? _popularEtag;
    private string? _newFixEtag;
    private readonly string _popularAppIdsCacheFile;
    private readonly string _newFixAppIdsCacheFile;
    private readonly string _popularEtagFile;
    private readonly string _newFixEtagFile;
    private DateTime _lastBackgroundCheckUtc = DateTime.MinValue;

    public MetadataService(IAppLogService log, INexaPlayOverrideService nexaPlayOverride)
    {
        _log = log;
        _nexaPlayOverride = nexaPlayOverride;
        _http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NexaPlay/1.0");
        _http.DefaultRequestVersion = System.Net.HttpVersion.Version11;
        _http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        _catalogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder,
            "runtime_catalog_sources");

        _steamDataGzFile = Path.Combine(_catalogDir, "steam_data.json.gz");
        _steamDataFile = Path.Combine(_catalogDir, "steam_data.json");
        _overrideDataFile = Path.Combine(_catalogDir, "override_data.json");
        _fixGamesFile = Path.Combine(_catalogDir, AppConstants.BypassGamesCacheFileName);
        _newFixGamesFile = Path.Combine(_catalogDir, AppConstants.NewFixGamesCacheFileName);
        _steamGamesFile = Path.Combine(_catalogDir, AppConstants.SteamGamesCacheFileName);
        _popularAppIdsCacheFile = Path.Combine(_catalogDir, "appid_populer_cache.json");
        _newFixAppIdsCacheFile = Path.Combine(_catalogDir, "new_fix_appids_cache.json");
        _popularEtagFile = Path.Combine(_catalogDir, "appid_populer.etag");
        _newFixEtagFile = Path.Combine(_catalogDir, "new_fix_games.etag");

        Directory.CreateDirectory(_catalogDir);
        _popularEtag = TryReadText(_popularEtagFile);
        _newFixEtag = TryReadText(_newFixEtagFile);
        _popularAppIdsCache = TryReadAppIdArray(_popularAppIdsCacheFile);
        _newFixAppIdsCache = TryReadAppIdArray(_newFixAppIdsCacheFile);
    }

    public bool IsCacheAvailable =>
        IsFileUsable(_steamDataGzFile) ||
        IsFileUsable(_steamDataFile) ||
        IsFileUsable(_overrideDataFile);

    public async Task<GameEntry?> GetMetadataAsync(int appId, CancellationToken ct = default)
    {
        await EnsureIndexedAsync(ct);
        _index!.TryGetValue(appId, out var entry);
        return entry;
    }

    public async Task<IReadOnlyList<GameEntry>> SearchAsync(string query, int maxResults = 50, CancellationToken ct = default)
    {
        await EnsureIndexedAsync(ct);
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<GameEntry>();

        return _index!.Values
            .Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
    }

    public async Task<IReadOnlyList<int>> GetPopularAppIdsAsync(CancellationToken ct = default)
    {
        if (_popularAppIdsCache is not null && _popularAppIdsCache.Count > 0)
        {
            _ = RefreshPopularAppIdsWithEtagAsync();
            return _popularAppIdsCache;
        }

        try
        {
            var arr = await FetchAppIdsWithEtagAsync(
                AppConstants.PopularGamesUrl,
                _popularAppIdsCacheFile,
                _popularEtagFile,
                _popularEtag,
                "Popular AppIds",
                ct);
            if (arr.Count > 0)
            {
                _popularAppIdsCache = arr;
                _popularLastLoaded = DateTime.UtcNow;
            }
            return _popularAppIdsCache ?? Array.Empty<int>();
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Popular AppIds download error: {ex.Message}");
            return _popularAppIdsCache ?? Array.Empty<int>();
        }
    }

    public async Task<IReadOnlyList<int>> GetNewFixAppIdsAsync(CancellationToken ct = default)
    {
        if (_newFixAppIdsCache is not null && _newFixAppIdsCache.Count > 0)
        {
            _ = RefreshNewFixAppIdsWithEtagAsync();
            return _newFixAppIdsCache;
        }

        try
        {
            var arr = await FetchAppIdsWithEtagAsync(
                AppConstants.NewFixGamesUrl,
                _newFixAppIdsCacheFile,
                _newFixEtagFile,
                _newFixEtag,
                "NewFix AppIds",
                ct);
            if (arr.Count > 0)
            {
                _newFixAppIdsCache = arr;
                _newFixLastLoaded = DateTime.UtcNow;
            }
            return _newFixAppIdsCache ?? Array.Empty<int>();
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"NewFix AppIds download error: {ex.Message}");
            return _newFixAppIdsCache ?? Array.Empty<int>();
        }
    }

    public async Task RefreshAsync(bool forceDownload = false, CancellationToken ct = default)
    {
        await RunWithSourceSyncLockAsync(() => SyncSourcesCoreAsync(forceDownload, useHeadCheck: false, ct), ct);
        await BuildIndexAsync(ct);
    }

    public async Task WarmupEssentialSourcesAsync(IProgress<MetadataWarmupProgress>? progress = null, CancellationToken ct = default)
    {
        await RunWithSourceSyncLockAsync(async () =>
        {
            var essentials = new (string Url, string Path, string Name)[]
            {
                (AppConstants.SteamDataJsonUrl, _steamDataFile, "steam_data.json"),
                (AppConstants.SteamDataUrl, _steamDataGzFile, "steam_data.json.gz"),
                (AppConstants.OverrideDataUrl, _overrideDataFile, "override_data.json")
            };

            var total = essentials.Length;
            for (var i = 0; i < total; i++)
            {
                var item = essentials[i];
                var completedBefore = i;
                var reporter = new Progress<double?>(filePercent =>
                {
                    progress?.Report(new MetadataWarmupProgress
                    {
                        FileName = item.Name,
                        Stage = "downloading",
                        CompletedFiles = completedBefore,
                        TotalFiles = total,
                        FilePercent = filePercent,
                        Message = $"Downloading {item.Name}"
                    });
                });

                progress?.Report(new MetadataWarmupProgress
                {
                    FileName = item.Name,
                    Stage = "start",
                    CompletedFiles = completedBefore,
                    TotalFiles = total,
                    FilePercent = 0,
                    Message = $"Start {item.Name}"
                });

                await DownloadIfNeededSafeAsync(item.Url, item.Path, force: false, useHeadCheck: false, ct, item.Name, reporter);

                progress?.Report(new MetadataWarmupProgress
                {
                    FileName = item.Name,
                    Stage = "done",
                    CompletedFiles = i + 1,
                    TotalFiles = total,
                    FilePercent = 100,
                    Message = $"Done {item.Name}"
                });
            }
        }, ct);
    }

    public Task ClearCacheAsync()
    {
        _index = null;
        _lastLoaded = DateTime.MinValue;

        try
        {
            if (Directory.Exists(_catalogDir))
                Directory.Delete(_catalogDir, recursive: true);
        }
        catch { }

        Directory.CreateDirectory(_catalogDir);
        _log.Log("Metadata", "Runtime catalog cache cleared");
        return Task.CompletedTask;
    }

    private async Task EnsureIndexedAsync(CancellationToken ct)
    {
        if (_index is not null)
        {
            _ = PerformBackgroundUpdateAsync();
            return;
        }

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_index is not null)
            {
                _ = PerformBackgroundUpdateAsync();
                return;
            }

            try
            {
                if (HasEssentialLocalSources())
                {
                    await BuildIndexAsync(ct);
                    _ = PerformBackgroundUpdateAsync();
                    return;
                }

                await RunWithSourceSyncLockAsync(() => SyncSourcesCoreAsync(force: false, useHeadCheck: false, ct), ct);
                await BuildIndexAsync(ct);
            }
            catch (JsonException ex)
            {
                _log.Log("Metadata", $"Catalog parse failed, trying force refresh: {ex.Message}");
                await RunWithSourceSyncLockAsync(() => SyncSourcesCoreAsync(force: true, useHeadCheck: false, ct), ct);
                await BuildIndexAsync(ct);
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task<bool> SyncSourcesCoreAsync(bool force, bool useHeadCheck, CancellationToken ct)
    {
        var anyUpdated = false;
        var jsonResult = await DownloadIfNeededSafeAsync(
            AppConstants.SteamDataJsonUrl, _steamDataFile, force, useHeadCheck, ct, "steam_data.json", null);
        var gzResult = await DownloadIfNeededSafeAsync(
            AppConstants.SteamDataUrl, _steamDataGzFile, force, useHeadCheck, ct, "steam_data.json.gz", null);
        anyUpdated |= jsonResult.Updated || gzResult.Updated;

        if (!gzResult.Available && jsonResult.Available)
        {
            _log.Log("Metadata", "Gzip source unavailable, fallback to steam_data.json");
        }

        if (!gzResult.Available && !jsonResult.Available)
        {
            throw new IOException("Both primary metadata sources are unavailable/corrupted.");
        }

        anyUpdated |= (await DownloadIfNeededSafeAsync(AppConstants.OverrideDataUrl, _overrideDataFile, force, useHeadCheck, ct, "override_data.json", null)).Updated;
        anyUpdated |= (await DownloadIfNeededSafeAsync(AppConstants.BypassGamesUrl, _fixGamesFile, force, useHeadCheck, ct, "fix_games.json", null)).Updated;
        anyUpdated |= (await DownloadIfNeededSafeAsync(AppConstants.NewFixGamesUrl, _newFixGamesFile, force, useHeadCheck, ct, "new_fix_games.json", null)).Updated;
        anyUpdated |= (await DownloadIfNeededSafeAsync(AppConstants.SteamGamesUrl, _steamGamesFile, force, useHeadCheck, ct, "steam_games.json", null)).Updated;

        return anyUpdated;
    }

    private async Task RunWithSourceSyncLockAsync(Func<Task> action, CancellationToken ct)
    {
        await _sourceSyncLock.WaitAsync(ct);
        try
        {
            await action();
        }
        finally
        {
            _sourceSyncLock.Release();
        }
    }

    private async Task<T> RunWithSourceSyncLockAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await _sourceSyncLock.WaitAsync(ct);
        try
        {
            return await action();
        }
        finally
        {
            _sourceSyncLock.Release();
        }
    }

    private async Task<DownloadResult> DownloadIfNeededSafeAsync(
        string url,
        string outputPath,
        bool force,
        bool useHeadCheck,
        CancellationToken ct,
        string sourceName,
        IProgress<double?>? progress)
    {
        var updated = false;
        try
        {
            updated = await DownloadIfNeededAsync(url, outputPath, force, useHeadCheck, ct, sourceName, progress);
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Download failed for {sourceName}: {ex.Message}");
            progress?.Report(null);
        }

        var available = File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
        return new DownloadResult(available, updated);
    }

    private async Task BuildIndexAsync(CancellationToken ct)
    {
        var catalog = new Dictionary<int, RuntimeCatalogEntry>(capacity: 160_000);
        var gzParsedOk = await MergeSourceSafeAsync(_steamDataGzFile, isGzip: true, catalog, ct);
        var jsonParsedOk = await MergeSourceSafeAsync(_steamDataFile, isGzip: false, catalog, ct);

        await MergeSourceSafeAsync(_overrideDataFile, isGzip: false, catalog, ct);

        // Guard: if both primary sources fail or catalog is implausibly small,
        // force caller to refresh sources (handled by EnsureIndexedAsync retry path).
        if ((!gzParsedOk && !jsonParsedOk) || catalog.Count < 50_000)
        {
            throw new JsonException(
                $"Runtime catalog incomplete/corrupted (count={catalog.Count}, gzParsed={gzParsedOk}, jsonParsed={jsonParsedOk}).");
        }

        HashSet<int> protectedAppIds;
        try
        {
            protectedAppIds = await LoadProtectionAppIdsAsync(
                [_fixGamesFile, _newFixGamesFile, _steamGamesFile],
                ct);
        }
        catch (JsonException ex)
        {
            _log.Log("Metadata", $"Protection source parse error, continue without protection list: {ex.Message}");
            protectedAppIds = new HashSet<int>();
        }

        _index = catalog.Values
            .Select(e => new GameEntry
            {
                AppId = e.AppId,
                Name = e.Title ?? $"App {e.AppId}",
                PriceDisplay = e.PriceDisplay,
                PriceNormalized = e.PriceNormalized,
                Protection = e.Protection || protectedAppIds.Contains(e.AppId),
                HeaderImageUrl = e.HeaderImageUrl ?? string.Empty,
                LibraryCapsule2xUrl = e.LibraryCapsule2xUrl,
                Genre = e.Genre
            })
            .ToDictionary(e => e.AppId);

        await ApplyNexaPlayCatalogOverridesAsync(ct);

        _lastLoaded = DateTime.UtcNow;
        _log.Log("Metadata", $"Runtime catalog built: {_index.Count:N0} unique appids, protected={protectedAppIds.Count:N0}");
    }

    private async Task ApplyNexaPlayCatalogOverridesAsync(CancellationToken ct)
    {
        if (_index is null) return;

        try
        {
            var overriddenIds = await _nexaPlayOverride.GetOverriddenAppIdsAsync(ct);
            var applied = 0;

            foreach (var appId in overriddenIds)
            {
                var ov = await _nexaPlayOverride.GetCatalogOverrideAsync(appId, ct);
                if (ov is null) continue;

                if (!_index.TryGetValue(appId, out var existing))
                {
                    existing = new GameEntry { AppId = appId, Name = $"App {appId}" };
                    _index[appId] = existing;
                }

                if (ov.Title is not null) existing.Name = ov.Title;
                if (ov.Developer is not null) existing.Developer = ov.Developer;
                if (ov.Publisher is not null) existing.Publisher = ov.Publisher;
                if (ov.Developers is not null) existing.Developers = ov.Developers;
                if (ov.Publishers is not null) existing.Publishers = ov.Publishers;
                if (ov.Genre is not null) existing.Genre = ov.Genre;
                if (ov.ShortDescription is not null) existing.ShortDescription = ov.ShortDescription;
                if (ov.ReleaseDate is not null) existing.ReleaseDate = ov.ReleaseDate;
                if (ov.PriceNormalized is not null) existing.PriceNormalized = ov.PriceNormalized.Value;
                if (ov.PriceDisplay is not null) existing.PriceDisplay = ov.PriceDisplay;
                if (ov.Protection is not null) existing.Protection = ov.Protection.Value;
                if (ov.Header is not null) existing.HeaderImageUrl = ov.Header;
                if (ov.Icon is not null) existing.IconImageUrl = ov.Icon;
                if (ov.LibraryCapsule2x is not null) existing.LibraryCapsule2xUrl = ov.LibraryCapsule2x;
                if (ov.LibraryHero2x is not null) existing.LibraryHero2xUrl = ov.LibraryHero2x;
                if (ov.BackgroundRaw is not null) existing.BackgroundRawImageUrl = ov.BackgroundRaw;

                applied++;
            }

            if (applied > 0)
                _log.Log("Metadata", $"NexaPlay catalog overrides applied: {applied}");
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"NexaPlay catalog override failed (non-blocking): {ex.Message}");
        }
    }

    private async Task<bool> MergeSourceSafeAsync(
        string path,
        bool isGzip,
        Dictionary<int, RuntimeCatalogEntry> catalog,
        CancellationToken ct)
    {
        try
        {
            await MergeSourceAsync(path, isGzip, catalog, ct);
            return true;
        }
        catch (JsonException ex)
        {
            _log.Log("Metadata", $"Skip corrupted source {Path.GetFileName(path)}: {ex.Message}");
            return false;
        }
    }

    private static async Task<HashSet<int>> LoadProtectionAppIdsAsync(
        IReadOnlyList<string> paths,
        CancellationToken ct)
    {
        var appIds = new HashSet<int>();

        foreach (var path in paths)
        {
            if (!File.Exists(path))
                continue;

            await using var file = File.OpenRead(path);
            using var doc = await JsonDocument.ParseAsync(file, cancellationToken: ct);
            CollectAppIds(doc.RootElement, appIds);
        }

        return appIds;
    }

    private static void CollectAppIds(JsonElement node, ISet<int> appIds)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in node.EnumerateObject())
                {
                    if (int.TryParse(prop.Name, out var keyedAppId))
                        appIds.Add(keyedAppId);

                    if (prop.NameEquals("appid"))
                    {
                        var explicitAppId = ReadIntValue(prop.Value);
                        if (explicitAppId is not null)
                            appIds.Add(explicitAppId.Value);
                    }

                    CollectAppIds(prop.Value, appIds);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in node.EnumerateArray())
                {
                    var appId = ReadIntValue(item);
                    if (appId is not null)
                        appIds.Add(appId.Value);
                    else
                        CollectAppIds(item, appIds);
                }
                break;
        }
    }

    private static int? ReadIntValue(JsonElement prop)
    {
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
            return value;

        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out value))
            return value;

        return null;
    }

    private static async Task MergeSourceAsync(
        string path,
        bool isGzip,
        Dictionary<int, RuntimeCatalogEntry> catalog,
        CancellationToken ct)
    {
        if (!File.Exists(path))
            return;

        await using var file = File.OpenRead(path);
        await using Stream stream = isGzip ? new GZipStream(file, CompressionMode.Decompress) : file;
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out var appId) || prop.Value.ValueKind != JsonValueKind.Object)
                continue;

            var node = prop.Value;
            if (node.TryGetProperty("appid", out var appIdNode) && appIdNode.TryGetInt32(out var explicitAppId))
                appId = explicitAppId;

            catalog.TryGetValue(appId, out var existing);
            catalog[appId] = new RuntimeCatalogEntry
            {
                AppId = appId,
                Title = ReadString(node, "title") ?? ReadString(node, "name") ?? existing?.Title,
                PriceDisplay = ReadString(node, "price_display") ?? existing?.PriceDisplay,
                PriceNormalized = ReadInt(node, "price_normalized") ?? existing?.PriceNormalized ?? 0,
                Protection = ReadProtection(node) ?? existing?.Protection ?? false,
                LibraryCapsule2xUrl = ReadAssetUrl(node, "library_capsule_2x") ?? existing?.LibraryCapsule2xUrl,
                HeaderImageUrl = ReadAssetUrl(node, "header") ?? existing?.HeaderImageUrl,
                Genre = ReadString(node, "genre") ?? existing?.Genre
            };
        }
    }

    private async Task<bool> DownloadIfNeededAsync(
        string url,
        string outputPath,
        bool force,
        bool useHeadCheck,
        CancellationToken ct,
        string sourceName,
        IProgress<double?>? progress)
    {
        var localExists = File.Exists(outputPath);
        var localAge = localExists
            ? DateTime.UtcNow - File.GetLastWriteTimeUtc(outputPath)
            : TimeSpan.MaxValue;

        if (!force && localExists && !useHeadCheck)
        {
            var len = new FileInfo(outputPath).Length;
            if (localAge < AppConstants.SafetyNetTtl && len > 0)
            {
                _log.Log("Metadata", $"Skip download {sourceName}: cache valid ({len} bytes, age {localAge.TotalMinutes:F1}m)");
                progress?.Report(100);
                return false;
            }
        }

        if (!force && useHeadCheck && localExists)
        {
            try
            {
                using var headReq = new HttpRequestMessage(HttpMethod.Head, url);
                using var headResp = await _http.SendAsync(headReq, HttpCompletionOption.ResponseHeadersRead, ct);
                headResp.EnsureSuccessStatusCode();

                var remoteLastModified = headResp.Content.Headers.LastModified;
                var localLastModified = File.GetLastWriteTimeUtc(outputPath);
                if (remoteLastModified.HasValue && remoteLastModified.Value.UtcDateTime <= localLastModified)
                {
                    _log.Log("Metadata", $"Skip download {sourceName}: not modified on GitHub");
                    progress?.Report(100);
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (localAge < AppConstants.SafetyNetTtl)
                {
                    _log.Log("Metadata", $"Skip download {sourceName}: HEAD failed but cache still valid ({ex.Message})");
                    progress?.Report(100);
                    return false;
                }

                _log.Log("Metadata", $"HEAD failed for {sourceName}, trying GET fallback: {ex.Message}");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        _log.Log("Metadata", $"Download start {sourceName} (force={force})");
        try
        {
            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var tempPath = Path.Combine(
                    Path.GetTempPath(),
                    "NexaPlay",
                    $"{sourceName}.{Environment.ProcessId}.{attempt}.{Guid.NewGuid():N}.tmp");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
                    // Wait until transfer finishes; cancellation only comes from caller/user action.
                    using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                    _log.Log("Metadata",
                        $"Download response {sourceName}: status={(int)response.StatusCode}, len={(response.Content.Headers.ContentLength?.ToString() ?? "unknown")}");
                    response.EnsureSuccessStatusCode();

                    var content = response.Content;
                    if (content is null)
                        throw new IOException($"Response content is null for {sourceName}");

                    var totalBytes = content.Headers.ContentLength;
                    var lastReportedPercent = -1d;
                    // No body timeout: wait until download fully completes (or user cancels via ct).
                    await using var source = await content.ReadAsStreamAsync(ct);
                    await using (var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[128 * 1024];
                        long written = 0;
                        int read;
                        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                        {
                            await target.WriteAsync(buffer.AsMemory(0, read), ct);
                            written += read;

                            if (totalBytes is > 0)
                            {
                                var percent = Math.Round(written * 100d / totalBytes.Value, 1);
                                if (percent - lastReportedPercent >= 2 || percent >= 100)
                                {
                                    lastReportedPercent = percent;
                                    progress?.Report(percent);
                                }
                            }
                        }
                        await target.FlushAsync(ct);
                    }

                    var downloadedLen = new FileInfo(tempPath).Length;
                    if (downloadedLen <= 0)
                        throw new IOException($"Downloaded empty file from {url}");

                    File.Copy(tempPath, outputPath, overwrite: true);
                    _log.Log("Metadata", $"Download ok {sourceName}: {downloadedLen} bytes");
                    progress?.Report(100);
                    return true;
                }
                catch (IOException ioEx) when (attempt < maxAttempts &&
                                               (ioEx.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase) ||
                                                ioEx.HResult == unchecked((int)0x80070020)))
                {
                    _log.Log("Metadata", $"Temp file lock {sourceName}, retry {attempt}/{maxAttempts}: {ioEx.Message}");
                    await Task.Delay(500 * attempt, ct);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                    catch { }
                }
            }

            throw new IOException($"Failed to download {sourceName} after retries due to temp file lock.");
        }
        catch (Exception ex)
        {
            var validExisting = localExists && new FileInfo(outputPath).Length > 0;
            if (!validExisting)
                throw;

            _log.Log("Metadata", $"Using stale source for {Path.GetFileName(outputPath)} after download error: {ex.Message}");
            return false;
        }
    }

    private async Task<string> DownloadStringAsync(string url, CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private bool HasEssentialLocalSources()
    {
        return IsFileUsable(_steamDataGzFile) || IsFileUsable(_steamDataFile);
    }

    private async Task RefreshPopularAppIdsWithEtagAsync()
    {
        try
        {
            if (DateTime.UtcNow - _popularLastLoaded < TimeSpan.FromMinutes(5))
                return;

            var latest = await FetchAppIdsWithEtagAsync(
                AppConstants.PopularGamesUrl,
                _popularAppIdsCacheFile,
                _popularEtagFile,
                _popularEtag,
                "Popular AppIds",
                CancellationToken.None);

            if (latest.Count > 0)
            {
                _popularAppIdsCache = latest;
                _popularLastLoaded = DateTime.UtcNow;
            }
        }
        catch { }
    }

    private async Task RefreshNewFixAppIdsWithEtagAsync()
    {
        try
        {
            if (DateTime.UtcNow - _newFixLastLoaded < TimeSpan.FromMinutes(2))
                return;

            var latest = await FetchAppIdsWithEtagAsync(
                AppConstants.NewFixGamesUrl,
                _newFixAppIdsCacheFile,
                _newFixEtagFile,
                _newFixEtag,
                "NewFix AppIds",
                CancellationToken.None);

            if (latest.Count > 0)
            {
                _newFixAppIdsCache = latest;
                _newFixLastLoaded = DateTime.UtcNow;
            }
        }
        catch { }
    }

    private async Task<IReadOnlyList<int>> FetchAppIdsWithEtagAsync(
        string url,
        string cacheFile,
        string etagFile,
        string? currentEtag,
        string logScope,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(currentEtag))
            req.Headers.TryAddWithoutValidation("If-None-Match", currentEtag);

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (resp.StatusCode == HttpStatusCode.NotModified)
        {
            var cached = TryReadAppIdArray(cacheFile);
            if (cached is not null && cached.Count > 0)
                return cached;

            return Array.Empty<int>();
        }

        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        await File.WriteAllTextAsync(cacheFile, json, ct);

        var etag = resp.Headers.ETag?.Tag;
        if (!string.IsNullOrWhiteSpace(etag))
        {
            await File.WriteAllTextAsync(etagFile, etag!, ct);
            if (logScope.StartsWith("Popular", StringComparison.OrdinalIgnoreCase))
                _popularEtag = etag;
            else
                _newFixEtag = etag;
        }

        var parsed = ParseAppIdsFromJson(json);
        _log.Log("Metadata", $"{logScope} fetched: {parsed.Count} appids");
        return parsed;
    }

    private static IReadOnlyList<int> ParseAppIdsFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var ids = new HashSet<int>();
            CollectAppIds(doc.RootElement, ids);
            return ids.ToList();
        }
        catch
        {
            return Array.Empty<int>();
        }
    }

    private static IReadOnlyList<int>? TryReadAppIdArray(string path)
    {
        if (!File.Exists(path))
            return null;
        var json = TryReadText(path);
        return string.IsNullOrWhiteSpace(json) ? null : ParseAppIdsFromJson(json);
    }

    private static string? TryReadText(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task PerformBackgroundUpdateAsync()
    {
        if (_lastBackgroundCheckUtc != DateTime.MinValue &&
            DateTime.UtcNow - _lastBackgroundCheckUtc < TimeSpan.FromMinutes(5))
        {
            return;
        }

        if (!await _backgroundUpdateLock.WaitAsync(0))
            return;

        _lastBackgroundCheckUtc = DateTime.UtcNow;
        try
        {
            var updated = await RunWithSourceSyncLockAsync(
                () => SyncSourcesCoreAsync(force: false, useHeadCheck: true, CancellationToken.None),
                CancellationToken.None);

            if (!updated)
                return;

            await _loadLock.WaitAsync();
            try
            {
                await BuildIndexAsync(CancellationToken.None);
                _log.Log("Metadata", "Background metadata update applied (hot-reload index).");
            }
            finally
            {
                _loadLock.Release();
            }
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Background metadata update failed: {ex.Message}");
        }
        finally
        {
            _backgroundUpdateLock.Release();
        }
    }

    private static string? ReadString(JsonElement node, string propertyName)
    {
        return node.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
            return value;

        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out value))
            return value;

        return null;
    }

    private static bool? ReadProtection(JsonElement node)
    {
        if (!node.TryGetProperty("protection", out var prop))
            return null;

        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False or JsonValueKind.Null => false,
            JsonValueKind.String => prop.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true ||
                                    prop.GetString()?.Contains("denuvo", StringComparison.OrdinalIgnoreCase) == true,
            _ => false
        };
    }

    private static string? ReadAssetUrl(JsonElement node, string key)
    {
        if (node.TryGetProperty(key, out var topLevel))
        {
            var topLevelUrl = ReadUrlFromElement(topLevel);
            if (!string.IsNullOrWhiteSpace(topLevelUrl))
                return topLevelUrl;
        }

        if (node.TryGetProperty("assets", out var assets) &&
            assets.ValueKind == JsonValueKind.Object &&
            assets.TryGetProperty(key, out var nested))
        {
            var nestedUrl = ReadUrlFromElement(nested);
            if (!string.IsNullOrWhiteSpace(nestedUrl))
                return nestedUrl;
        }

        return null;
    }

    private static string? ReadUrlFromElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("url", out var directUrl) &&
            directUrl.ValueKind == JsonValueKind.String)
        {
            return directUrl.GetString();
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    return item.GetString();

                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("url", out var itemUrl) &&
                    itemUrl.ValueKind == JsonValueKind.String)
                {
                    return itemUrl.GetString();
                }
            }
        }

        return null;
    }

    private sealed class RuntimeCatalogEntry
    {
        public int AppId { get; init; }
        public string? Title { get; init; }
        public string? PriceDisplay { get; init; }
        public int PriceNormalized { get; init; }
        public bool Protection { get; init; }
        public string? LibraryCapsule2xUrl { get; init; }
        public string? HeaderImageUrl { get; init; }
        public string? Genre { get; init; }
    }

    private readonly record struct DownloadResult(bool Available, bool Updated);

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
}
