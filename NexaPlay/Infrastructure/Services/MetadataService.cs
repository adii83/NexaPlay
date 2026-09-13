using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Helpers;
using NexaPlay.Core.Models;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Lightweight runtime catalog for list surfaces.
/// Source order: steam_data.json.gz -> steam_data.json -> new_games_catalog.json -> override_data.json.
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
    private readonly string _newGamesFile;
    private readonly string _newGamesCatalogFile;
    private readonly string _catalogManifestFile;
    private readonly string _catalogManifestEtagFile;
    private readonly string _sourceEtagDir;
    private readonly string _sourceHashDir;
    private readonly string _r2EtagDir;
    private static readonly TimeSpan NewGamesR2Timeout = TimeSpan.FromSeconds(30);
    private const int SourceDownloadConcurrency = 3;

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
    private DateTime _lastNewGamesMetadataCheckUtc = DateTime.MinValue;

    public event EventHandler? CatalogUpdated;

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
        _newGamesFile = Path.Combine(_catalogDir, AppConstants.NewGamesCacheFileName);
        _newGamesCatalogFile = Path.Combine(_catalogDir, AppConstants.NewGamesCatalogFileName);
        _catalogManifestFile = Path.Combine(_catalogDir, AppConstants.CatalogManifestCacheFileName);
        _catalogManifestEtagFile = Path.Combine(_catalogDir, AppConstants.CatalogManifestEtagFileName);
        _sourceEtagDir = Path.Combine(_catalogDir, ".etags");
        _sourceHashDir = Path.Combine(_catalogDir, ".sha256");
        _r2EtagDir = Path.Combine(_catalogDir, "metadata_etags");
        _popularAppIdsCacheFile = Path.Combine(_catalogDir, "appid_populer.json");
        _newFixAppIdsCacheFile = _newFixGamesFile;
        _popularEtagFile = GetSourceEtagPath("appid_populer.json");
        _newFixEtagFile = GetSourceEtagPath("new_fix_games.json");

        Directory.CreateDirectory(_catalogDir);
        Directory.CreateDirectory(_sourceEtagDir);
        Directory.CreateDirectory(_sourceHashDir);
        Directory.CreateDirectory(_r2EtagDir);
        _popularEtag = TryReadText(_popularEtagFile);
        _newFixEtag = TryReadText(_newFixEtagFile);
        _popularAppIdsCache = TryReadAppIdArray(_popularAppIdsCacheFile);
        _newFixAppIdsCache = TryReadAppIdArray(_newFixAppIdsCacheFile);
        if (_popularAppIdsCache is { Count: > 0 })
            _popularLastLoaded = DateTime.UtcNow;
        if (_newFixAppIdsCache is { Count: > 0 })
            _newFixLastLoaded = DateTime.UtcNow;
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

    public async Task<IReadOnlyList<int>> GetAllCatalogAppIdsAsync(CancellationToken ct = default)
    {
        await EnsureIndexedAsync(ct);
        return _index!.Keys
            .OrderBy(id => id)
            .ToList();
    }

    public async Task<IReadOnlyList<GameEntry>> GetCatalogSnapshotAsync(CancellationToken ct = default)
    {
        await EnsureIndexedAsync(ct);
        return _index!.Values.ToList();
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
        await RunWithSourceSyncLockAsync(
            () => SyncSourcesCoreAsync(revalidate: forceDownload, progress: null, ct),
            ct);
        await _loadLock.WaitAsync(ct);
        try
        {
            await BuildIndexAsync(ct);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<NewGamesRefreshResult> RefreshDynamicSourcesAsync(
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        await RunWithSourceSyncLockAsync(
            () => SyncSourcesCoreAsync(revalidate: true, progress, ct),
            ct);
        ReloadAppIdCachesFromDisk();

        await _loadLock.WaitAsync(ct);
        try
        {
            var result = await BuildIndexAsync(ct, materializeNewGames: true, progress);
            _lastNewGamesMetadataCheckUtc = DateTime.UtcNow;
            return result;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task WarmupEssentialSourcesAsync(IProgress<MetadataWarmupProgress>? progress = null, CancellationToken ct = default)
    {
        if (HasEssentialLocalSources())
        {
            await _loadLock.WaitAsync(ct);
            try
            {
                if (_index is null)
                    await BuildIndexAsync(ct);
            }
            finally
            {
                _loadLock.Release();
            }

            _ = PerformBackgroundUpdateAsync();
            return;
        }

        var warmupProgress = progress is null
            ? null
            : new Progress<double>(value => progress.Report(new MetadataWarmupProgress
            {
                FileName = "catalog metadata",
                Stage = "downloading",
                CompletedFiles = 0,
                TotalFiles = 1,
                FilePercent = value,
                Message = "Downloading catalog metadata"
            }));

        await RunWithSourceSyncLockAsync(
            () => SyncSourcesCoreAsync(revalidate: true, warmupProgress, ct),
            ct);
        ReloadAppIdCachesFromDisk();

        await _loadLock.WaitAsync(ct);
        try
        {
            await BuildIndexAsync(ct, materializeNewGames: true, progress: null);
            _lastNewGamesMetadataCheckUtc = DateTime.UtcNow;
            _lastBackgroundCheckUtc = DateTime.UtcNow;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public Task ClearCacheAsync()
    {
        _index = null;
        _lastLoaded = DateTime.MinValue;
        _popularAppIdsCache = null;
        _popularLastLoaded = DateTime.MinValue;
        _newFixAppIdsCache = null;
        _newFixLastLoaded = DateTime.MinValue;
        _popularEtag = TryReadText(_popularEtagFile);
        _newFixEtag = TryReadText(_newFixEtagFile);

        _log.Log("Metadata", "Metadata derived cache cleared (sources retained)");
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

                await RunWithSourceSyncLockAsync(
                    () => SyncSourcesCoreAsync(revalidate: true, progress: null, ct),
                    ct);
                await BuildIndexAsync(ct);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            {
                _log.Log("Metadata", $"Catalog parse failed, trying force refresh: {ex.Message}");
                ClearCatalogValidationState();
                await RunWithSourceSyncLockAsync(
                    () => SyncSourcesCoreAsync(revalidate: true, progress: null, ct),
                    ct);
                await BuildIndexAsync(ct);
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task<bool> SyncSourcesCoreAsync(
        bool revalidate,
        IProgress<double>? progress,
        CancellationToken ct)
    {
        var sources = GetCatalogSources();
        var manifestResult = await TrySyncFromManifestAsync(sources, revalidate, progress, ct);
        var covered = manifestResult?.CoveredNames ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var remaining = sources.Where(source => !covered.Contains(source.RemoteName)).ToArray();
        var fallbackUpdated = await SyncSourcesWithEtagsAsync(remaining, revalidate, progress, ct);
        var anyUpdated = (manifestResult?.Updated ?? false) || fallbackUpdated;

        ReloadAppIdCachesFromDisk();

        var jsonResult = new DownloadResult(IsFileUsable(_steamDataFile), anyUpdated);
        var gzResult = new DownloadResult(IsFileUsable(_steamDataGzFile), anyUpdated);

        if (!gzResult.Available && jsonResult.Available)
        {
            _log.Log("Metadata", "Gzip source unavailable, fallback to steam_data.json");
        }

        if (!gzResult.Available && !jsonResult.Available)
        {
            throw new IOException("Both primary metadata sources are unavailable/corrupted.");
        }

        return anyUpdated;
    }

    private IReadOnlyList<CatalogSource> GetCatalogSources() =>
    [
        new("steam_data.json", AppConstants.SteamDataJsonUrl, _steamDataFile, false),
        new("steam_data.json.gz", AppConstants.SteamDataUrl, _steamDataGzFile, true),
        new("override_data.json", AppConstants.OverrideDataUrl, _overrideDataFile, false),
        new("fix_games.json", AppConstants.BypassGamesUrl, _fixGamesFile, false),
        new("new_fix_games.json", AppConstants.NewFixGamesUrl, _newFixGamesFile, false),
        new("steam_games/steam_games.json", AppConstants.SteamGamesUrl, _steamGamesFile, false),
        new("appid_populer.json", AppConstants.PopularGamesUrl, _popularAppIdsCacheFile, false),
        new("new_games.json", AppConstants.NewGamesUrl, _newGamesFile, false)
    ];

    private async Task<ManifestSyncResult?> TrySyncFromManifestAsync(
        IReadOnlyList<CatalogSource> sources,
        bool revalidate,
        IProgress<double>? progress,
        CancellationToken ct)
    {
        if (!revalidate)
            return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, AppConstants.CatalogManifestUrl);
            var currentEtag = TryReadText(_catalogManifestEtagFile);
            if (!string.IsNullOrWhiteSpace(currentEtag))
                request.Headers.TryAddWithoutValidation("If-None-Match", currentEtag);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(AppConstants.HttpDefaultTimeout);
            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _log.Log("Metadata", "catalog_manifest.json not published; using per-file ETags.");
                return null;
            }

            string? manifestJson;
            string? responseEtag = null;
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                manifestJson = TryReadText(_catalogManifestFile);
            }
            else
            {
                response.EnsureSuccessStatusCode();
                manifestJson = await response.Content.ReadAsStringAsync(timeout.Token);
                responseEtag = response.Headers.ETag?.ToString();
            }

            if (string.IsNullOrWhiteSpace(manifestJson))
                return null;

            var allowedNames = sources.Select(source => source.RemoteName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var manifest = CatalogManifestRules.Parse(manifestJson, allowedNames);
            if (manifest is null)
            {
                _log.Log("Metadata", "catalog_manifest.json invalid; using per-file ETags.");
                return null;
            }

            var sourceByName = sources.ToDictionary(
                source => source.RemoteName,
                StringComparer.OrdinalIgnoreCase);
            foreach (var entry in manifest.Files.Values)
            {
                if (!sourceByName.TryGetValue(entry.Name, out var source) ||
                    !string.Equals(entry.Url, source.Url, StringComparison.OrdinalIgnoreCase))
                {
                    _log.Log("Metadata", $"Manifest URL mismatch for {entry.Name}; using per-file ETags.");
                    return null;
                }
            }

            var localStates = new Dictionary<string, CatalogFileState>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in manifest.Files.Values)
            {
                var source = sourceByName[entry.Name];
                if (!IsFileUsable(source.LocalPath) || new FileInfo(source.LocalPath).Length != entry.Size)
                    continue;

                var knownHash = TryReadText(GetSourceHashPath(source.RemoteName));
                if (string.Equals(knownHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    localStates[entry.Name] = new CatalogFileState(entry.Size, entry.Sha256);
                    continue;
                }

                try
                {
                    var actualHash = await CatalogSourceFile.ValidateAsync(
                        source.LocalPath,
                        source.IsGzip,
                        entry.Size,
                        entry.Sha256,
                        ct,
                        source.RemoteName);
                    localStates[entry.Name] = new CatalogFileState(entry.Size, actualHash);
                    await WriteTextAtomicallyAsync(GetSourceHashPath(source.RemoteName), actualHash, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.Log("Metadata", $"Local manifest validation failed for {entry.Name}: {ex.Message}");
                }
            }

            var changedNames = CatalogManifestRules.SelectChangedFiles(manifest, localStates);
            var staged = new ConcurrentBag<StagedCatalogSource>();
            try
            {
                using var gate = new SemaphoreSlim(SourceDownloadConcurrency, SourceDownloadConcurrency);
                var completed = 0;
                var tasks = changedNames.Select(async name =>
                {
                    await gate.WaitAsync(ct);
                    try
                    {
                        var candidate = await DownloadManifestCandidateAsync(
                            sourceByName[name],
                            manifest.Files[name],
                            ct);
                        staged.Add(candidate);
                    }
                    finally
                    {
                        gate.Release();
                        var done = Interlocked.Increment(ref completed);
                        progress?.Report(changedNames.Length == 0 ? 100 : done * 100d / changedNames.Length);
                    }
                });
                await Task.WhenAll(tasks);

                var orderedCandidates = staged
                    .OrderBy(item => item.Source.RemoteName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                CatalogSourceFile.PublishBatch(orderedCandidates
                    .Select(candidate => (candidate.TempPath, candidate.Source.LocalPath))
                    .ToArray());
                foreach (var candidate in orderedCandidates)
                {
                    try
                    {
                        await WriteTextAtomicallyAsync(
                            GetSourceHashPath(candidate.Source.RemoteName),
                            candidate.Sha256,
                            ct);
                        if (!string.IsNullOrWhiteSpace(candidate.ETag))
                            await WriteTextAtomicallyAsync(GetSourceEtagPath(candidate.Source.RemoteName), candidate.ETag!, ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _log.Log("Metadata", $"Source validation state update failed for {candidate.Source.RemoteName}: {ex.Message}");
                    }
                }

                if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    await WriteTextAtomicallyAsync(_catalogManifestFile, manifestJson, ct);
                    if (!string.IsNullOrWhiteSpace(responseEtag))
                        await WriteTextAtomicallyAsync(_catalogManifestEtagFile, responseEtag!, ct);
                }

                _log.Log("Metadata", $"Catalog manifest {manifest.Revision}: changed={changedNames.Length}");
                return new ManifestSyncResult(changedNames.Length > 0, manifest.Files.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase));
            }
            finally
            {
                foreach (var candidate in staged)
                    TryDelete(candidate.TempPath);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _log.Log("Metadata", "Catalog manifest request timed out; using per-file ETags.");
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Log("Metadata", $"Catalog manifest sync failed; using per-file ETags: {ex.Message}");
            return null;
        }
    }

    private async Task<StagedCatalogSource> DownloadManifestCandidateAsync(
        CatalogSource source,
        CatalogManifestEntry entry,
        CancellationToken ct)
    {
        var tempPath = CreateTempPath(source.RemoteName);
        try
        {
            using var response = await _http.GetAsync(source.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            await using var remote = await response.Content.ReadAsStreamAsync(ct);
            await using (var local = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await remote.CopyToAsync(local, ct);
                await local.FlushAsync(ct);
            }

            var hash = await CatalogSourceFile.ValidateAsync(
                tempPath,
                source.IsGzip,
                entry.Size,
                entry.Sha256,
                ct,
                source.RemoteName);
            return new StagedCatalogSource(source, tempPath, hash, response.Headers.ETag?.ToString());
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private async Task<bool> SyncSourcesWithEtagsAsync(
        IReadOnlyList<CatalogSource> sources,
        bool revalidate,
        IProgress<double>? progress,
        CancellationToken ct)
    {
        if (sources.Count == 0)
        {
            progress?.Report(100);
            return false;
        }

        using var gate = new SemaphoreSlim(SourceDownloadConcurrency, SourceDownloadConcurrency);
        var completed = 0;
        var tasks = sources.Select(async source =>
        {
            await gate.WaitAsync(ct);
            try
            {
                return await DownloadIfNeededSafeAsync(
                    source.Url,
                    source.LocalPath,
                    revalidate,
                    ct,
                    source.RemoteName,
                    source.IsGzip,
                    null);
            }
            finally
            {
                gate.Release();
                var done = Interlocked.Increment(ref completed);
                progress?.Report(done * 100d / sources.Count);
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Any(result => result.Updated);
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
        bool revalidate,
        CancellationToken ct,
        string sourceName,
        bool isGzip,
        IProgress<double?>? progress)
    {
        var updated = false;
        try
        {
            updated = await DownloadIfNeededAsync(url, outputPath, revalidate, ct, sourceName, isGzip, progress);
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Download failed for {sourceName}: {ex.Message}");
            progress?.Report(null);
        }

        var available = File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
        return new DownloadResult(available, updated);
    }

    private async Task<NewGamesRefreshResult> BuildIndexAsync(
        CancellationToken ct,
        bool materializeNewGames = false,
        IProgress<double>? progress = null)
    {
        var catalog = new Dictionary<int, RuntimeCatalogEntry>(capacity: 160_000);
        var gzParsedOk = await MergeSourceSafeAsync(_steamDataGzFile, isGzip: true, catalog, ct);
        var jsonParsedOk = await MergeSourceSafeAsync(_steamDataFile, isGzip: false, catalog, ct);

        // Guard before additional sources so they can never disguise a broken primary catalog.
        if ((!gzParsedOk && !jsonParsedOk) || catalog.Count < 50_000)
        {
            throw new JsonException(
                $"Runtime catalog incomplete/corrupted (count={catalog.Count}, gzParsed={gzParsedOk}, jsonParsed={jsonParsedOk}).");
        }

        var primaryAppIds = catalog.Keys.ToHashSet();
        var materializeResult = default(NewGamesRefreshResult);
        if (materializeNewGames)
        {
            var requestedAppIds = ReadNewGamesAppIds();
            if (requestedAppIds is not null)
                materializeResult = await MaterializeNewGamesAsync(requestedAppIds, primaryAppIds, progress, ct);
        }

        var snapshotEntries = ReadNewGamesSnapshot();
        var addedSnapshotIds = new HashSet<int>();
        foreach (var entry in snapshotEntries)
        {
            if (catalog.TryAdd(entry.AppId, new RuntimeCatalogEntry
                {
                    AppId = entry.AppId,
                    Title = entry.Name,
                    PriceDisplay = entry.PriceDisplay,
                    PriceNormalized = entry.PriceNormalized,
                    Protection = entry.Protection,
                    LibraryCapsuleUrl = entry.LibraryCapsuleUrl,
                    HeaderImageUrl = entry.HeaderImageUrl,
                    Genre = entry.Genre
                }))
            {
                addedSnapshotIds.Add(entry.AppId);
            }
        }

        // Existing archive override remains later than the additive snapshot.
        await MergeSourceSafeAsync(_overrideDataFile, isGzip: false, catalog, ct);

        HashSet<int> protectedAppIds;
        try
        {
            protectedAppIds = await LoadProtectionAppIdsAsync(
                [_fixGamesFile, _newFixGamesFile, _steamGamesFile],
                ct);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
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
                LibraryCapsuleUrl = e.LibraryCapsuleUrl,
                Genre = e.Genre
            })
            .ToDictionary(e => e.AppId);

        foreach (var entry in snapshotEntries)
        {
            if (!addedSnapshotIds.Contains(entry.AppId) || !_index.TryGetValue(entry.AppId, out var indexed))
                continue;

            indexed.Developer = entry.Developer;
            indexed.Publisher = entry.Publisher;
            indexed.Developers = entry.Developers;
            indexed.Publishers = entry.Publishers;
            indexed.ShortDescription = entry.ShortDescription;
            indexed.ReleaseDate = entry.ReleaseDate;
            indexed.IconImageUrl = entry.IconImageUrl;
            indexed.LibraryHero2xUrl = entry.LibraryHero2xUrl;
            indexed.BackgroundRawImageUrl = entry.BackgroundRawImageUrl;
        }

        await ApplyNexaPlayCatalogOverridesAsync(ct);

        _lastLoaded = DateTime.UtcNow;
        _log.Log("Metadata", $"Runtime catalog built: {_index.Count:N0} unique appids, protected={protectedAppIds.Count:N0}");
        return materializeResult;
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
                if (ov.LibraryCapsule is not null) existing.LibraryCapsuleUrl = ov.LibraryCapsule;
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
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
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
                LibraryCapsuleUrl = ReadAssetUrl(node, "library_capsule") ?? ReadAssetUrl(node, "library_capsule_2x") ?? existing?.LibraryCapsuleUrl,
                HeaderImageUrl = ReadAssetUrl(node, "header") ?? existing?.HeaderImageUrl,
                Genre = ReadString(node, "genre") ?? existing?.Genre
            };
        }
    }

    private int[]? ReadNewGamesAppIds()
    {
        var json = TryReadText(_newGamesFile);
        return string.IsNullOrWhiteSpace(json) ? null : NewGamesCatalog.ParseAppIds(json);
    }

    private async Task<NewGamesRefreshResult> MaterializeNewGamesAsync(
        IReadOnlyCollection<int> requestedAppIds,
        ISet<int> primaryAppIds,
        IProgress<double>? progress,
        CancellationToken ct)
    {
        var previousEntries = ReadNewGamesSnapshot();
        var cachedById = previousEntries.ToDictionary(entry => entry.AppId);
        var fetchAppIds = NewGamesCatalog.SelectRefreshAppIds(requestedAppIds, primaryAppIds);
        progress?.Report(80);

        var completed = 0;
        using var gate = new SemaphoreSlim(4, 4);
        var fetchTasks = fetchAppIds.Select(async appId =>
        {
            await gate.WaitAsync(ct);
            try
            {
                cachedById.TryGetValue(appId, out var cachedEntry);
                return await FetchNewGameEntryAsync(appId, cachedEntry, ct);
            }
            finally
            {
                gate.Release();
                var done = Interlocked.Increment(ref completed);
                progress?.Report(80 + done * 20d / fetchAppIds.Length);
            }
        });

        var fetched = fetchAppIds.Length == 0
            ? Array.Empty<NewGamesCatalogEntry?>()
            : await Task.WhenAll(fetchTasks);
        var fetchedEntries = fetched.OfType<NewGamesCatalogEntry>().ToArray();
        var newlyAdded = fetchedEntries.Count(entry => !cachedById.ContainsKey(entry.AppId));
        var availableById = previousEntries.ToDictionary(entry => entry.AppId);
        foreach (var fetchedEntry in fetchedEntries)
            availableById[fetchedEntry.AppId] = fetchedEntry;
        var composed = NewGamesCatalog.ComposeSnapshot(requestedAppIds, primaryAppIds, availableById.Values);

        try
        {
            var json = NewGamesCatalog.SerializeSnapshot(composed);
            await NewGamesCatalogFile.PublishSnapshotAsync(json, _newGamesCatalogFile, ct);
            progress?.Report(100);
            var requestedAdditionalCount = requestedAppIds.Count(appId => !primaryAppIds.Contains(appId));
            return new NewGamesRefreshResult(
                newlyAdded,
                Math.Max(0, requestedAdditionalCount - composed.Length));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Log("Metadata", $"New Games snapshot publication failed; retaining previous snapshot: {ex.Message}");
            progress?.Report(100);
            var previousIds = previousEntries.Select(entry => entry.AppId).ToHashSet();
            var unavailable = requestedAppIds.Count(appId =>
                !primaryAppIds.Contains(appId) && !previousIds.Contains(appId));
            return new NewGamesRefreshResult(0, unavailable);
        }
    }

    private async Task<NewGamesCatalogEntry?> FetchNewGameEntryAsync(
        int appId,
        NewGamesCatalogEntry? cachedEntry,
        CancellationToken ct)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(NewGamesR2Timeout);
            var url = $"{AppConstants.R2MetadataBaseUrl}/{appId}.json";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var etagPath = Path.Combine(_r2EtagDir, $"{appId}.etag");
            var currentEtag = cachedEntry is null ? null : TryReadText(etagPath);
            if (cachedEntry is not null && !string.IsNullOrWhiteSpace(currentEtag) &&
                await RemoteEtagMatchesAsync(url, currentEtag, timeout.Token))
            {
                return cachedEntry;
            }

            if (!string.IsNullOrWhiteSpace(currentEtag))
                request.Headers.TryAddWithoutValidation("If-None-Match", currentEtag);

            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotModified)
                return cachedEntry;
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _log.Log("Metadata", $"New Games metadata unavailable appId={appId}: 404");
                return cachedEntry;
            }

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            var entry = NewGamesCatalog.ParseMetadata(appId, document.RootElement);
            if (entry is null)
            {
                _log.Log("Metadata", $"New Games metadata invalid appId={appId}");
                return cachedEntry;
            }

            var responseEtag = response.Headers.ETag?.ToString();
            if (!string.IsNullOrWhiteSpace(responseEtag))
                await WriteTextAtomicallyAsync(etagPath, responseEtag!, ct);
            return entry;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _log.Log("Metadata", $"New Games metadata timed out appId={appId}");
            return cachedEntry;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Log("Metadata", $"New Games metadata unavailable appId={appId}: {ex.Message}");
            return cachedEntry;
        }
    }

    private NewGamesCatalogEntry[] ReadNewGamesSnapshot()
    {
        if (!File.Exists(_newGamesCatalogFile))
            return [];

        try
        {
            var json = File.ReadAllText(_newGamesCatalogFile);
            var entries = NewGamesCatalog.ParseSnapshot(json);
            if (entries is not null)
                return entries;

            _log.Log("Metadata", "New Games snapshot is invalid; ignoring it.");
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"New Games snapshot read failed; ignoring it: {ex.Message}");
        }

        return [];
    }

    private async Task<bool> DownloadIfNeededAsync(
        string url,
        string outputPath,
        bool revalidate,
        CancellationToken ct,
        string sourceName,
        bool isGzip,
        IProgress<double?>? progress)
    {
        var localExists = File.Exists(outputPath);
        var localAge = localExists
            ? DateTime.UtcNow - File.GetLastWriteTimeUtc(outputPath)
            : TimeSpan.MaxValue;

        if (!revalidate && localExists)
        {
            var len = new FileInfo(outputPath).Length;
            if (localAge < AppConstants.SafetyNetTtl && len > 0)
            {
                _log.Log("Metadata", $"Skip download {sourceName}: cache valid ({len} bytes, age {localAge.TotalMinutes:F1}m)");
                progress?.Report(100);
                return false;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        _log.Log("Metadata", $"Revalidate source {sourceName}");
        try
        {
            var etagPath = GetSourceEtagPath(sourceName);
            var currentEtag = localExists ? TryReadText(etagPath) : null;
            if (localExists && !string.IsNullOrWhiteSpace(currentEtag) &&
                await RemoteEtagMatchesAsync(url, currentEtag, ct))
            {
                _log.Log("Metadata", $"Source unchanged {sourceName} (HEAD ETag)");
                progress?.Report(100);
                return false;
            }

            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var tempPath = Path.Combine(
                    Path.GetTempPath(),
                    "NexaPlay",
                    $"{SanitizeSourceName(sourceName)}.{Environment.ProcessId}.{attempt}.{Guid.NewGuid():N}.tmp");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    if (!string.IsNullOrWhiteSpace(currentEtag))
                        request.Headers.TryAddWithoutValidation("If-None-Match", currentEtag);

                    using var response = await _http.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        ct);
                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        _log.Log("Metadata", $"Source unchanged {sourceName} (ETag)");
                        progress?.Report(100);
                        return false;
                    }

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

                    var hash = await CatalogSourceFile.ValidateAsync(
                        tempPath,
                        isGzip,
                        ct: ct,
                        sourceName: sourceName);
                    var hashPath = GetSourceHashPath(sourceName);
                    var currentHash = localExists ? TryReadText(hashPath) : null;
                    if (localExists && string.IsNullOrWhiteSpace(currentHash))
                    {
                        try
                        {
                            currentHash = await CatalogSourceFile.ValidateAsync(
                                outputPath,
                                isGzip,
                                ct: ct,
                                sourceName: sourceName);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _log.Log("Metadata", $"Existing source validation failed for {sourceName}: {ex.Message}");
                        }
                    }

                    var responseEtag = response.Headers.ETag?.ToString();
                    if (string.Equals(currentHash, hash, StringComparison.OrdinalIgnoreCase))
                    {
                        await WriteTextAtomicallyAsync(hashPath, hash, ct);
                        if (!string.IsNullOrWhiteSpace(responseEtag))
                            await WriteTextAtomicallyAsync(etagPath, responseEtag!, ct);
                        _log.Log("Metadata", $"Source unchanged {sourceName} (SHA-256)");
                        progress?.Report(100);
                        return false;
                    }

                    CatalogSourceFile.Publish(tempPath, outputPath);
                    await WriteTextAtomicallyAsync(hashPath, hash, ct);
                    if (!string.IsNullOrWhiteSpace(responseEtag))
                        await WriteTextAtomicallyAsync(etagPath, responseEtag!, ct);
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

    private void ReloadAppIdCachesFromDisk()
    {
        _popularAppIdsCache = TryReadAppIdArray(_popularAppIdsCacheFile);
        _newFixAppIdsCache = TryReadAppIdArray(_newFixAppIdsCacheFile);
        _popularEtag = TryReadText(_popularEtagFile);
        _newFixEtag = TryReadText(_newFixEtagFile);
        _popularLastLoaded = DateTime.UtcNow;
        _newFixLastLoaded = DateTime.UtcNow;
    }

    private async Task<bool> RemoteEtagMatchesAsync(string url, string currentEtag, CancellationToken ct)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(AppConstants.HttpHeadTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            if (!response.IsSuccessStatusCode)
                return false;

            var remoteEtag = response.Headers.ETag?.ToString();
            return !string.IsNullOrWhiteSpace(remoteEtag) &&
                   string.Equals(remoteEtag, currentEtag, StringComparison.Ordinal);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Log("Metadata", $"HEAD ETag check failed for {url}: {ex.Message}");
            return false;
        }
    }

    private string GetSourceEtagPath(string sourceName) =>
        Path.Combine(_sourceEtagDir, $"{SanitizeSourceName(sourceName)}.etag");

    private string GetSourceHashPath(string sourceName) =>
        Path.Combine(_sourceHashDir, $"{SanitizeSourceName(sourceName)}.sha256");

    private static string SanitizeSourceName(string sourceName) =>
        sourceName.Replace('/', '_').Replace('\\', '_');

    private static string CreateTempPath(string sourceName)
    {
        var directory = Path.Combine(Path.GetTempPath(), "NexaPlay");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"{SanitizeSourceName(sourceName)}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");
    }

    private static async Task WriteTextAtomicallyAsync(string path, string value, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, value, ct);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private void ClearCatalogValidationState()
    {
        foreach (var directory in new[] { _sourceEtagDir, _sourceHashDir })
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(directory))
                    TryDelete(path);
            }
            catch { }
        }
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
        var parsed = ParseAppIdsFromJson(json);
        if (parsed.Count == 0)
            throw new JsonException($"{logScope} response contains no valid AppIDs.");
        await WriteTextAtomicallyAsync(cacheFile, json, ct);

        var etag = resp.Headers.ETag?.ToString();
        if (!string.IsNullOrWhiteSpace(etag))
        {
            await WriteTextAtomicallyAsync(etagFile, etag!, ct);
            if (logScope.StartsWith("Popular", StringComparison.OrdinalIgnoreCase))
                _popularEtag = etag;
            else
                _newFixEtag = etag;
        }

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
                () => SyncSourcesCoreAsync(revalidate: true, progress: null, CancellationToken.None),
                CancellationToken.None);

            var refreshNewGamesMetadata = updated ||
                DateTime.UtcNow - _lastNewGamesMetadataCheckUtc >= AppConstants.SafetyNetTtl;
            if (!updated && !refreshNewGamesMetadata)
                return;

            await _loadLock.WaitAsync();
            try
            {
                await BuildIndexAsync(
                    CancellationToken.None,
                    materializeNewGames: refreshNewGamesMetadata,
                    progress: null);
                if (refreshNewGamesMetadata)
                    _lastNewGamesMetadataCheckUtc = DateTime.UtcNow;
                _log.Log("Metadata", "Background metadata update applied (hot-reload index).");
            }
            finally
            {
                _loadLock.Release();
            }

            CatalogUpdated?.Invoke(this, EventArgs.Empty);
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
        public string? LibraryCapsuleUrl { get; init; }
        public string? HeaderImageUrl { get; init; }
        public string? Genre { get; init; }
    }

    private sealed record CatalogSource(string RemoteName, string Url, string LocalPath, bool IsGzip);

    private sealed record StagedCatalogSource(
        CatalogSource Source,
        string TempPath,
        string Sha256,
        string? ETag);

    private sealed record ManifestSyncResult(bool Updated, IReadOnlySet<string> CoveredNames);

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
