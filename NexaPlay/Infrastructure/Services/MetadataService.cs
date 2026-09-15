using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Helpers;
using NexaPlay.Core.Models;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

public sealed class MetadataService : IMetadataService
{
    private readonly IAppLogService _log;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private readonly ICatalogBootstrapService _catalogBootstrap;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IDynamicFeedRefreshService _dynamicFeeds;
    private readonly string _catalogDir;
    private readonly string _popularFile;
    private readonly string _newFixFile;
    private readonly string _fixGamesFile;
    private readonly string _steamGamesFile;
    private IReadOnlyList<int>? _popularAppIds;
    private IReadOnlyList<int>? _newFixAppIds;
    private IReadOnlyDictionary<int, NexaPlayCatalogOverride> _catalogOverrides =
        new Dictionary<int, NexaPlayCatalogOverride>();
    private IReadOnlyDictionary<int, CatalogRuntimeOverride> _runtimeOverrides =
        new Dictionary<int, CatalogRuntimeOverride>();

    public MetadataService(
        IAppLogService log,
        INexaPlayOverrideService nexaPlayOverride,
        ICatalogBootstrapService catalogBootstrap,
        ICatalogRepository catalogRepository,
        IDynamicFeedRefreshService dynamicFeeds)
    {
        _log = log;
        _nexaPlayOverride = nexaPlayOverride;
        _catalogBootstrap = catalogBootstrap;
        _catalogRepository = catalogRepository;
        _dynamicFeeds = dynamicFeeds;

        _catalogDir = Path.Combine(AppConstants.AppDataPath, "runtime_catalog_sources");
        Directory.CreateDirectory(_catalogDir);

        _popularFile = _dynamicFeeds.GetLocalPath(DynamicFeedKind.Popular);
        _newFixFile = _dynamicFeeds.GetLocalPath(DynamicFeedKind.NewFix);
        _fixGamesFile = _dynamicFeeds.GetLocalPath(DynamicFeedKind.FixGames);
        _steamGamesFile = _dynamicFeeds.GetLocalPath(DynamicFeedKind.SteamGames);

        ReloadFeedCachesFromDisk();
        _ = Task.Run(DeleteRetiredCoverIndexArtifacts);
    }

    public bool IsCacheAvailable => File.Exists(_catalogBootstrap.RuntimeDatabasePath);

    public async Task<GameEntry?> GetMetadataAsync(int appId, CancellationToken ct = default)
    {
        var entry = await _catalogRepository.GetByAppIdAsync(appId, ct);
        if (entry is not null)
            ApplyCachedCatalogOverride(entry);
        return entry;
    }

    public async Task<IReadOnlyList<GameEntry>> GetMetadataBatchAsync(
        IReadOnlyList<int> appIds,
        CancellationToken ct = default)
    {
        var entries = await _catalogRepository.GetByAppIdsAsync(appIds, ct);
        foreach (var entry in entries)
            ApplyCachedCatalogOverride(entry);
        return entries;
    }

    public async Task<IReadOnlyList<GameEntry>> SearchAsync(
        string query,
        int maxResults = 50,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<GameEntry>();

        var page = await _catalogRepository.QueryAsync(new CatalogQuery(
            query, null, null, Array.Empty<string>(), false, 0, maxResults, 1), ct);
        return page.Items;
    }

    public Task<IReadOnlyList<int>> GetPopularAppIdsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _popularAppIds ??= TryReadAppIds(_popularFile) ?? Array.Empty<int>();
        return Task.FromResult(_popularAppIds);
    }

    public Task<IReadOnlyList<int>> GetNewFixAppIdsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _newFixAppIds ??= TryReadAppIds(_newFixFile) ?? Array.Empty<int>();
        return Task.FromResult(_newFixAppIds);
    }

    public async Task RefreshAsync(bool forceDownload = false, CancellationToken ct = default)
    {
        await _catalogBootstrap.EnsureReadyAsync(ct);
        if (forceDownload)
            await RefreshDynamicSourcesAsync(force: true, ct: ct);
        else
            ReloadFeedCachesFromDisk();
        await RefreshCatalogOverlaysAsync(ct);
    }

    public async Task<MetadataRefreshResult> RefreshDynamicSourcesAsync(
        bool force = false,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        await _catalogBootstrap.EnsureReadyAsync(ct);
        progress?.Report(10);
        var feeds = await _dynamicFeeds.RefreshAsync(force, ct);
        progress?.Report(60);

        var beforeOverrides = JsonSerializer.Serialize(_catalogOverrides.OrderBy(pair => pair.Key));
        await _nexaPlayOverride.RefreshAsync(ct);
        var refreshedOverrides = await _nexaPlayOverride.GetCachedCatalogOverridesAsync(ct);
        var overrideChanged = !string.Equals(
            beforeOverrides,
            JsonSerializer.Serialize(refreshedOverrides.OrderBy(pair => pair.Key)),
            StringComparison.Ordinal);

        if ((feeds.Changes & DynamicFeedChanges.Popular) != 0)
            _popularAppIds = TryReadAppIds(_popularFile) ?? Array.Empty<int>();
        if ((feeds.Changes & DynamicFeedChanges.NewFix) != 0)
            _newFixAppIds = TryReadAppIds(_newFixFile) ?? Array.Empty<int>();

        progress?.Report(80);
        var protectionFeedsChanged = (feeds.Changes &
            (DynamicFeedChanges.NewFix | DynamicFeedChanges.FixGames | DynamicFeedChanges.SteamGames)) != 0;
        var overlayChanged = false;
        if (protectionFeedsChanged || overrideChanged)
            overlayChanged = await RefreshCatalogOverlaysAsync(ct);
        progress?.Report(100);
        return new MetadataRefreshResult(feeds, overrideChanged, overlayChanged);
    }

    public async Task WarmupEssentialSourcesAsync(
        IProgress<MetadataWarmupProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new MetadataWarmupProgress
        {
            FileName = "catalog.sqlite",
            Stage = "local",
            CompletedFiles = 0,
            TotalFiles = 1,
            FilePercent = 20,
            Message = "Preparing local catalog"
        });
        await _catalogBootstrap.EnsureReadyAsync(ct);
        ReloadFeedCachesFromDisk();
        await RefreshCatalogOverlaysAsync(ct);
        progress?.Report(new MetadataWarmupProgress
        {
            FileName = "catalog.sqlite",
            Stage = "ready",
            CompletedFiles = 1,
            TotalFiles = 1,
            FilePercent = 100,
            Message = "Local catalog ready"
        });
    }

    public Task ClearCacheAsync()
    {
        ReloadFeedCachesFromDisk();
        try
        {
            File.Delete(Path.Combine(_catalogDir, AppConstants.LegacyGamesFilterCacheFileName));
        }
        catch
        {
        }
        DeleteRetiredCoverIndexArtifacts();
        _log.Log("Metadata", "Disposable metadata cache cleared; runtime catalog retained");
        return Task.CompletedTask;
    }

    private void DeleteRetiredCoverIndexArtifacts()
    {
        var etagDir = Path.Combine(_catalogDir, ".etags");
        string[] paths =
        [
            Path.Combine(_catalogDir, AppConstants.RetiredLibraryCapsuleCacheFileName),
            Path.Combine(_catalogDir, AppConstants.RetiredLibraryCapsuleGzipCacheFileName),
            Path.Combine(etagDir, $"{AppConstants.RetiredLibraryCapsuleCacheFileName}.etag"),
            Path.Combine(etagDir, $"{AppConstants.RetiredLibraryCapsuleGzipCacheFileName}.etag")
        ];

        foreach (var path in paths)
        {
            try { File.Delete(path); }
            catch (Exception ex) { _log.Log("Metadata", $"Retired cover-index cleanup failed: {ex.Message}"); }
        }
    }

    private async Task<bool> RefreshCatalogOverlaysAsync(CancellationToken ct)
    {
        HashSet<int> protectedAppIds;
        try
        {
            protectedAppIds = await LoadProtectionAppIdsAsync(
                [_fixGamesFile, _newFixFile, _steamGamesFile], ct);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            _log.Log("Catalog", $"Protection overlay parse failed; base SQLite values retained: {ex.Message}");
            protectedAppIds = [];
        }

        _catalogOverrides = await _nexaPlayOverride.GetCachedCatalogOverridesAsync(ct);
        var runtime = protectedAppIds.ToDictionary(
            appId => appId,
            _ => new CatalogRuntimeOverride(Protection: true));

        foreach (var pair in _catalogOverrides)
        {
            runtime.TryGetValue(pair.Key, out var current);
            var ov = pair.Value;
            var price = ov.PriceNormalized;
            runtime[pair.Key] = new CatalogRuntimeOverride(
                ov.Title ?? current?.Name,
                ov.Title is null ? current?.NormalizedName : ov.Title.NormalizeForSearch(),
                ov.LibraryCapsule ?? current?.LibraryCapsule,
                ov.Genre is null ? current?.GenreJson : JsonSerializer.Serialize(
                    ov.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                price ?? current?.PriceNormalized,
                ov.PriceDisplay ?? current?.PriceDisplay,
                ov.Protection ?? current?.Protection,
                current?.Tier);
        }

        var before = JsonSerializer.Serialize(_runtimeOverrides.OrderBy(pair => pair.Key));
        var after = JsonSerializer.Serialize(runtime.OrderBy(pair => pair.Key));
        if (string.Equals(before, after, StringComparison.Ordinal))
            return false;

        await _catalogRepository.ReplaceRuntimeOverridesAsync(runtime, ct);
        _runtimeOverrides = runtime;
        _log.Log("Catalog", $"Runtime catalog overlays refreshed: protected={protectedAppIds.Count}, overrides={_catalogOverrides.Count}");
        return true;
    }

    private void ApplyCachedCatalogOverride(GameEntry entry)
    {
        if (!_catalogOverrides.TryGetValue(entry.AppId, out var ov))
            return;

        if (ov.Developer is not null) entry.Developer = ov.Developer;
        if (ov.Publisher is not null) entry.Publisher = ov.Publisher;
        if (ov.Developers is not null) entry.Developers = ov.Developers;
        if (ov.Publishers is not null) entry.Publishers = ov.Publishers;
        if (ov.ShortDescription is not null) entry.ShortDescription = ov.ShortDescription;
        if (ov.ReleaseDate is not null) entry.ReleaseDate = ov.ReleaseDate;
        if (ov.Header is not null) entry.HeaderImageUrl = ov.Header;
        if (ov.Icon is not null) entry.IconImageUrl = ov.Icon;
        if (ov.LibraryHero2x is not null) entry.LibraryHero2xUrl = ov.LibraryHero2x;
        if (ov.BackgroundRaw is not null) entry.BackgroundRawImageUrl = ov.BackgroundRaw;
    }

    private void ReloadFeedCachesFromDisk()
    {
        _popularAppIds = TryReadAppIds(_popularFile);
        _newFixAppIds = TryReadAppIds(_newFixFile);
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
            using var document = await JsonDocument.ParseAsync(file, cancellationToken: ct);
            CollectAppIds(document.RootElement, appIds);
        }
        return appIds;
    }

    private static IReadOnlyList<int> ParseAppIds(string json)
    {
        using var document = JsonDocument.Parse(json);
        var appIds = new List<int>();
        CollectOrderedAppIds(document.RootElement, appIds, new HashSet<int>());
        return appIds;
    }

    private static void CollectOrderedAppIds(JsonElement node, ICollection<int> appIds, ISet<int> seen)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in node.EnumerateObject())
            {
                if (int.TryParse(property.Name, out var keyedAppId) && keyedAppId > 0 && seen.Add(keyedAppId))
                    appIds.Add(keyedAppId);
                if (property.NameEquals("appid") && TryReadAppId(property.Value, out var appId) && seen.Add(appId))
                    appIds.Add(appId);
                CollectOrderedAppIds(property.Value, appIds, seen);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                if (TryReadAppId(item, out var appId) && seen.Add(appId))
                    appIds.Add(appId);
                else if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    CollectOrderedAppIds(item, appIds, seen);
            }
        }
    }

    private static void CollectAppIds(JsonElement node, ISet<int> appIds)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in node.EnumerateObject())
            {
                if (int.TryParse(property.Name, out var keyedAppId) && keyedAppId > 0)
                    appIds.Add(keyedAppId);
                if (property.NameEquals("appid") && TryReadAppId(property.Value, out var appId))
                    appIds.Add(appId);
                CollectAppIds(property.Value, appIds);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                if (TryReadAppId(item, out var appId))
                    appIds.Add(appId);
                else
                    CollectAppIds(item, appIds);
            }
        }
    }

    private static bool TryReadAppId(JsonElement node, out int appId)
    {
        appId = 0;
        return node.ValueKind switch
        {
            JsonValueKind.Number => node.TryGetInt32(out appId) && appId > 0,
            JsonValueKind.String => int.TryParse(node.GetString(), out appId) && appId > 0,
            _ => false
        };
    }

    private static IReadOnlyList<int>? TryReadAppIds(string path)
    {
        try
        {
            return File.Exists(path) ? ParseAppIds(File.ReadAllText(path)) : null;
        }
        catch
        {
            return null;
        }
    }

}
