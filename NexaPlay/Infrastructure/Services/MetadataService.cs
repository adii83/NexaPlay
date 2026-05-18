using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Models;
using System.IO.Compression;
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
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

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

    public MetadataService(IAppLogService log)
    {
        _log = log;
        _http = new HttpClient { Timeout = AppConstants.HttpDefaultTimeout };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NexaPlay/1.0");

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

        Directory.CreateDirectory(_catalogDir);
    }

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
        if (_popularAppIdsCache is not null && DateTime.UtcNow - _popularLastLoaded < AppConstants.BypassGamesCacheTtl)
            return _popularAppIdsCache;

        try
        {
            var json = await DownloadStringAsync(AppConstants.PopularGamesUrl, ct);
            var arr = JsonSerializer.Deserialize<int[]>(json) ?? Array.Empty<int>();
            _popularAppIdsCache = arr;
            _popularLastLoaded = DateTime.UtcNow;
            return arr;
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Popular AppIds download error: {ex.Message}");
            return _popularAppIdsCache ?? Array.Empty<int>();
        }
    }

    public async Task RefreshAsync(bool forceDownload = false, CancellationToken ct = default)
    {
        await SyncSourcesAsync(forceDownload, ct);
        await BuildIndexAsync(ct);
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
        if (_index is not null && DateTime.UtcNow - _lastLoaded < AppConstants.SteamDataCacheTtl)
            return;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_index is not null && DateTime.UtcNow - _lastLoaded < AppConstants.SteamDataCacheTtl)
                return;

            await SyncSourcesAsync(force: false, ct);
            await BuildIndexAsync(ct);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task SyncSourcesAsync(bool force, CancellationToken ct)
    {
        await DownloadIfNeededAsync(AppConstants.SteamDataUrl, _steamDataGzFile, force, ct);
        await DownloadIfNeededAsync(AppConstants.SteamDataJsonUrl, _steamDataFile, force, ct);
        await DownloadIfNeededAsync(AppConstants.OverrideDataUrl, _overrideDataFile, force, ct);
        await DownloadIfNeededAsync(AppConstants.BypassGamesUrl, _fixGamesFile, force, ct);
        await DownloadIfNeededAsync(AppConstants.NewFixGamesUrl, _newFixGamesFile, force, ct);
        await DownloadIfNeededAsync(AppConstants.SteamGamesUrl, _steamGamesFile, force, ct);
    }

    private async Task BuildIndexAsync(CancellationToken ct)
    {
        var catalog = new Dictionary<int, RuntimeCatalogEntry>(capacity: 160_000);

        await MergeSourceAsync(_steamDataGzFile, isGzip: true, catalog, ct);
        await MergeSourceAsync(_steamDataFile, isGzip: false, catalog, ct);
        await MergeSourceAsync(_overrideDataFile, isGzip: false, catalog, ct);
        var protectedAppIds = await LoadProtectionAppIdsAsync(
            [_fixGamesFile, _newFixGamesFile, _steamGamesFile],
            ct);

        _index = catalog.Values
            .Select(e => new GameEntry
            {
                AppId = e.AppId,
                Name = e.Title ?? $"App {e.AppId}",
                PriceDisplay = e.PriceDisplay,
                PriceNormalized = e.PriceNormalized,
                Protection = e.Protection || protectedAppIds.Contains(e.AppId),
                HeaderImageUrl = e.HeaderImageUrl ?? string.Empty,
                Genre = e.Genre
            })
            .ToDictionary(e => e.AppId);

        _lastLoaded = DateTime.UtcNow;
        _log.Log("Metadata", $"Runtime catalog built: {_index.Count:N0} unique appids, protected={protectedAppIds.Count:N0}");
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
                HeaderImageUrl = ReadString(node, "header") ?? existing?.HeaderImageUrl,
                Genre = ReadString(node, "genre") ?? existing?.Genre
            };
        }
    }

    private async Task DownloadIfNeededAsync(string url, string outputPath, bool force, CancellationToken ct)
    {
        if (!force && File.Exists(outputPath))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(outputPath);
            if (age < AppConstants.SteamDataCacheTtl)
                return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        try
        {
            await using var source = await _http.GetStreamAsync(url, ct);
            await using var target = File.Create(outputPath);
            await source.CopyToAsync(target, ct);
        }
        catch (Exception ex)
        {
            if (!File.Exists(outputPath))
                throw;

            _log.Log("Metadata", $"Using stale source for {Path.GetFileName(outputPath)} after download error: {ex.Message}");
        }
    }

    private async Task<string> DownloadStringAsync(string url, CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
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

    private sealed class RuntimeCatalogEntry
    {
        public int AppId { get; init; }
        public string? Title { get; init; }
        public string? PriceDisplay { get; init; }
        public int PriceNormalized { get; init; }
        public bool Protection { get; init; }
        public string? HeaderImageUrl { get; init; }
        public string? Genre { get; init; }
    }
}
