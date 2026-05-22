using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Models;
using System.Net;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Downloads and caches nexaplay_override.json from a dedicated GitHub repo.
/// Uses ETag caching — checks for updates only once at app startup.
/// </summary>
public sealed class NexaPlayOverrideService : INexaPlayOverrideService
{
    private readonly IAppLogService _log;
    private readonly HttpClient _http;
    private readonly string _cacheFile;
    private readonly string _etagFile;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private Dictionary<int, OverrideEntry>? _index;
    private string? _etag;
    private bool _startupCheckDone;

    public NexaPlayOverrideService(IAppLogService log)
    {
        _log = log;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NexaPlay/1.0");

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder,
            "runtime_catalog_sources");
        Directory.CreateDirectory(dir);
        _cacheFile = Path.Combine(dir, AppConstants.NexaPlayOverrideCacheFileName);
        _etagFile = Path.Combine(dir, "nexaplay_override.etag");

        _etag = TryReadText(_etagFile);
    }

    public async Task<NexaPlayCatalogOverride?> GetCatalogOverrideAsync(int appId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _index!.TryGetValue(appId, out var entry) ? entry.Catalog : null;
    }

    public async Task<NexaPlayDetailOverride?> GetDetailOverrideAsync(int appId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _index!.TryGetValue(appId, out var entry) ? entry.Detail : null;
    }

    public async Task<IReadOnlySet<int>> GetOverriddenAppIdsAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _index!.Keys.ToHashSet();
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        _index = null;
        _startupCheckDone = false;
        await EnsureLoadedAsync(ct);
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_index is not null && _startupCheckDone)
            return;

        await _lock.WaitAsync(ct);
        try
        {
            if (_index is not null && _startupCheckDone)
                return;

            if (_index is null)
                LoadFromDiskCache();

            if (!_startupCheckDone)
            {
                await FetchWithEtagAsync(ct);
                _startupCheckDone = true;
            }
        }
        finally { _lock.Release(); }
    }

    private void LoadFromDiskCache()
    {
        if (!File.Exists(_cacheFile))
        {
            _index = new Dictionary<int, OverrideEntry>();
            return;
        }

        try
        {
            var json = File.ReadAllText(_cacheFile);
            _index = ParseJson(json);
            _log.Log("NexaPlayOverride", $"Loaded {_index.Count} overrides from disk cache");
        }
        catch (Exception ex)
        {
            _log.Log("NexaPlayOverride", $"Disk cache parse failed: {ex.Message}");
            _index = new Dictionary<int, OverrideEntry>();
        }
    }

    private async Task FetchWithEtagAsync(CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, AppConstants.NexaPlayOverrideUrl);
            if (!string.IsNullOrWhiteSpace(_etag))
                req.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(_etag));

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (resp.StatusCode == HttpStatusCode.NotModified)
            {
                _log.Log("NexaPlayOverride", "ETag matched — data not modified, using cache.");
                return;
            }

            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);

            var newEtag = resp.Headers.ETag?.Tag;
            if (!string.IsNullOrWhiteSpace(newEtag))
            {
                _etag = newEtag;
                TryWriteText(_etagFile, newEtag);
            }

            await File.WriteAllTextAsync(_cacheFile, json, ct);
            _index = ParseJson(json);
            _log.Log("NexaPlayOverride", $"Downloaded {_index.Count} overrides (ETag updated)");
        }
        catch (Exception ex)
        {
            _log.Log("NexaPlayOverride", $"ETag fetch failed (non-blocking): {ex.Message}");
        }
    }

    private Dictionary<int, OverrideEntry> ParseJson(string json)
    {
        var result = new Dictionary<int, OverrideEntry>();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out var appId) || prop.Value.ValueKind != JsonValueKind.Object)
                continue;

            var entry = new OverrideEntry();

            if (prop.Value.TryGetProperty("catalog", out var catalogNode) &&
                catalogNode.ValueKind == JsonValueKind.Object)
            {
                entry.Catalog = ParseCatalog(catalogNode);
            }

            if (prop.Value.TryGetProperty("detail", out var detailNode) &&
                detailNode.ValueKind == JsonValueKind.Object)
            {
                entry.Detail = ParseDetail(detailNode);
            }

            result[appId] = entry;
        }

        return result;
    }

    private static NexaPlayCatalogOverride ParseCatalog(JsonElement node)
    {
        return new NexaPlayCatalogOverride
        {
            Title = ReadStr(node, "title"),
            Developer = ReadStr(node, "developer"),
            Publisher = ReadStr(node, "publisher"),
            Developers = ReadStrArray(node, "developers"),
            Publishers = ReadStrArray(node, "publishers"),
            Genre = ReadStr(node, "genre"),
            ShortDescription = ReadStr(node, "short_description"),
            ReleaseDate = ReadStr(node, "release_date"),
            PriceNormalized = ReadNullableInt(node, "price_normalized"),
            PriceDisplay = ReadStr(node, "price_display"),
            Protection = ReadNullableBool(node, "protection"),
            Header = ReadStr(node, "header"),
            Icon = ReadStr(node, "icon"),
            LibraryCapsule2x = ReadStr(node, "library_capsule_2x"),
            LibraryHero2x = ReadStr(node, "library_hero_2x"),
            BackgroundRaw = ReadStr(node, "background_raw")
        };
    }

    private static NexaPlayDetailOverride ParseDetail(JsonElement node)
    {
        return new NexaPlayDetailOverride
        {
            ShortDescription = ReadStr(node, "short_description"),
            AboutTheGame = ReadStr(node, "about_the_game"),
            DetailedDescription = ReadStr(node, "detailed_description"),
            SupportedLanguages = ReadStr(node, "supported_languages"),
            Website = ReadStr(node, "website"),
            Developers = ReadStrArray(node, "developers"),
            Publishers = ReadStrArray(node, "publishers"),
            ReleaseDate = ReadStr(node, "release_date"),
            Screenshots = ReadScreenshots(node),
            Movies = ReadMovies(node),
            BackgroundImage = ReadStr(node, "background_image"),
            PcRequirementsMinimum = ReadStr(node, "pc_requirements_minimum"),
            PcRequirementsRecommended = ReadStr(node, "pc_requirements_recommended"),
            MacRequirementsMinimum = ReadStr(node, "mac_requirements_minimum"),
            MacRequirementsRecommended = ReadStr(node, "mac_requirements_recommended"),
            LinuxRequirementsMinimum = ReadStr(node, "linux_requirements_minimum"),
            LinuxRequirementsRecommended = ReadStr(node, "linux_requirements_recommended"),
            Categories = ReadStrArray(node, "categories"),
            SupportUrl = ReadStr(node, "support_url"),
            SupportEmail = ReadStr(node, "support_email"),
            LegalNotice = ReadStr(node, "legal_notice"),
            DrmNotice = ReadStr(node, "drm_notice"),
            StorePriceFinalFormatted = ReadStr(node, "store_price_final_formatted"),
            StorePriceCurrency = ReadStr(node, "store_price_currency")
        };
    }

    private static IReadOnlyList<NexaPlayScreenshotOverride>? ReadScreenshots(JsonElement node)
    {
        if (!node.TryGetProperty("screenshots", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return null;

        var list = new List<NexaPlayScreenshotOverride>();
        foreach (var item in arr.EnumerateArray())
        {
            list.Add(new NexaPlayScreenshotOverride
            {
                Id = ReadInt(item, "id"),
                Thumbnail = ReadStr(item, "thumbnail") ?? string.Empty,
                Full = ReadStr(item, "full") ?? string.Empty
            });
        }
        return list;
    }

    private static IReadOnlyList<NexaPlayMovieOverride>? ReadMovies(JsonElement node)
    {
        if (!node.TryGetProperty("movies", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return null;

        var list = new List<NexaPlayMovieOverride>();
        foreach (var item in arr.EnumerateArray())
        {
            list.Add(new NexaPlayMovieOverride
            {
                Id = ReadInt(item, "id"),
                Name = ReadStr(item, "name") ?? string.Empty,
                Thumbnail = ReadStr(item, "thumbnail") ?? string.Empty,
                HlsUrl = ReadStr(item, "hls_url"),
                DashH264Url = ReadStr(item, "dash_h264_url"),
                IsHighlight = ReadBool(item, "is_highlight")
            });
        }
        return list;
    }

    private static string? ReadStr(JsonElement node, string key)
    {
        return node.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString()
            : null;
    }

    private static int ReadInt(JsonElement node, string key)
    {
        return node.TryGetProperty(key, out var val) && val.TryGetInt32(out var i) ? i : 0;
    }

    private static int? ReadNullableInt(JsonElement node, string key)
    {
        return node.TryGetProperty(key, out var val) && val.TryGetInt32(out var i) ? i : null;
    }

    private static bool ReadBool(JsonElement node, string key)
    {
        return node.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.True;
    }

    private static bool? ReadNullableBool(JsonElement node, string key)
    {
        if (!node.TryGetProperty(key, out var val))
            return null;
        return val.ValueKind == JsonValueKind.True;
    }

    private static IReadOnlyList<string>? ReadStrArray(JsonElement node, string key)
    {
        if (!node.TryGetProperty(key, out var val) || val.ValueKind != JsonValueKind.Array)
            return null;

        var list = new List<string>();
        foreach (var item in val.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
                list.Add(item.GetString() ?? string.Empty);
        }
        return list;
    }

    private static string? TryReadText(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path).Trim() : null; }
        catch { return null; }
    }

    private static void TryWriteText(string path, string text)
    {
        try { File.WriteAllText(path, text); }
        catch { }
    }

    private sealed class OverrideEntry
    {
        public NexaPlayCatalogOverride? Catalog { get; set; }
        public NexaPlayDetailOverride? Detail { get; set; }
    }
}
