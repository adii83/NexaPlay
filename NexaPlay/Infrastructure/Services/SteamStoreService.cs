using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Resolves rich game detail on demand from Cloudflare R2 repository and stores a full merged raw payload per appid.
/// </summary>
public sealed partial class SteamStoreService : ISteamStoreService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);
    private const string R2MetadataBaseUrl = "https://meta.nexaplaymetadata.online/Metadata";

    private readonly IAppLogService _log;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private readonly string _cacheDir;
    private readonly HttpClient _http;

    public SteamStoreService(IAppLogService log, INexaPlayOverrideService nexaPlayOverride)
    {
        _log = log;
        _nexaPlayOverride = nexaPlayOverride;
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder,
            "metadata_detail");
        Directory.CreateDirectory(_cacheDir);

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NexaPlay/1.0");
    }

    public async Task<GameDetailEntry?> GetDetailAsync(int appId, CancellationToken ct = default)
    {
        var cacheFile = CacheFilePath(appId);

        if (File.Exists(cacheFile))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile);
            var cachedJson = await ReadRawCacheAsync(cacheFile, ct);
            if (!string.IsNullOrWhiteSpace(cachedJson) && age < CacheTtl)
            {
                _log.Log("StoreService", $"Detail cache hit appId={appId} age={age.TotalHours:F1}h");
                return await ApplyDetailOverrideAsync(ParseMergedDetail(appId, cachedJson, isFromCache: true), appId, ct);
            }
        }

        try
        {
            _log.Log("StoreService", $"Resolving merged detail from R2 appId={appId}");
            var result = await BuildMergedMetadataJsonAsync(appId, ct);
            if (!result.HasTransientError && !string.IsNullOrWhiteSpace(result.Json))
            {
                await File.WriteAllTextAsync(cacheFile, result.Json, ct);
                return await ApplyDetailOverrideAsync(ParseMergedDetail(appId, result.Json, isFromCache: false), appId, ct);
            }
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Log("StoreService", $"Detail resolve failed appId={appId}: {ex.Message}");
            var stale = await ReadRawCacheAsync(cacheFile, ct);
            return string.IsNullOrWhiteSpace(stale)
                ? null
                : await ApplyDetailOverrideAsync(ParseMergedDetail(appId, stale, isFromCache: true), appId, ct);
        }
    }

    public Task InvalidateCacheAsync(int appId)
    {
        var path = CacheFilePath(appId);
        try { if (File.Exists(path)) File.Delete(path); } catch { }
        return Task.CompletedTask;
    }

    private async Task<(string Json, bool HasTransientError)> BuildMergedMetadataJsonAsync(int appId, CancellationToken ct)
    {
        try
        {
            var url = $"{R2MetadataBaseUrl}/{appId}.json";
            using var response = await _http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _log.Log("StoreService", $"AppId {appId} not found in R2");
                return ("", false); // Not a transient error, just doesn't exist
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            return (json, false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Log("StoreService", $"R2 Fetch transient error appId={appId}: {ex.Message}");
            return ("", true);
        }
    }

    private static GameDetailEntry? ParseMergedDetail(int appId, string rawJson, bool isFromCache)
    {
        try
        {
            var root = JsonNode.Parse(rawJson) as JsonObject;
            if (root is null) return null;

            var store = root["store_data"] as JsonObject;
            var assets = root["assets"] as JsonObject;

            var screenshots = ParseScreenshots(assets?["screenshots"] as JsonArray);
            var movies = ParseMovies(assets?["movies"] as JsonArray);

            return new GameDetailEntry
            {
                AppId = appId,
                ShortDescription = ReadString(store, "short_description"),
                AboutTheGame = ReadString(store, "about_the_game"),
                DetailedDescription = ReadString(store, "detailed_description"),
                SupportedLanguages = ReadString(store, "supported_languages"),
                Website = ReadString(store, "website"),
                Developers = ReadStringArray(store?["developers"] as JsonArray),
                Publishers = ReadStringArray(store?["publishers"] as JsonArray),
                ReleaseDate = ReadString(store?["release_date"] as JsonObject, "date") ?? ReadString(store, "release_date"),
                BackgroundImageUrl = FirstAssetUrl(assets, "background_raw") ?? FirstAssetUrl(assets, "background"),
                Screenshots = screenshots,
                Movies = movies,
                PcRequirementsMinimum = ReadString(store?["pc_requirements"] as JsonObject, "minimum"),
                PcRequirementsRecommended = ReadString(store?["pc_requirements"] as JsonObject, "recommended"),
                MacRequirementsMinimum = ReadString(store?["mac_requirements"] as JsonObject, "minimum"),
                MacRequirementsRecommended = ReadString(store?["mac_requirements"] as JsonObject, "recommended"),
                LinuxRequirementsMinimum = ReadString(store?["linux_requirements"] as JsonObject, "minimum"),
                LinuxRequirementsRecommended = ReadString(store?["linux_requirements"] as JsonObject, "recommended"),
                Categories = ParseDescriptionArray(store?["categories"] as JsonArray),
                SupportUrl = ReadString(store?["support_info"] as JsonObject, "url"),
                SupportEmail = ReadString(store?["support_info"] as JsonObject, "email"),
                LegalNotice = ReadString(store, "legal_notice"),
                DrmNotice = ReadString(store, "drm_notice"),
                StorePriceFinalFormatted = ReadString(store?["price_overview"] as JsonObject, "final_formatted"),
                StorePriceCurrency = ReadString(store?["price_overview"] as JsonObject, "currency"),
                CachedAt = DateTime.UtcNow,
                IsFromCache = isFromCache,
                RawMetadataJson = rawJson
            };
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<ScreenshotEntry> ParseScreenshots(JsonArray? arr)
    {
        if (arr is null) return Array.Empty<ScreenshotEntry>();

        return arr.OfType<JsonObject>()
            .Select(item => new ScreenshotEntry
            {
                Id = ReadInt(item, "id") ?? 0,
                ThumbnailUrl = ReadString(item, "path_thumbnail") ?? string.Empty,
                FullUrl = ReadString(item, "path_full") ?? string.Empty
            })
            .Where(s => !string.IsNullOrWhiteSpace(s.ThumbnailUrl))
            .ToArray();
    }

    private static IReadOnlyList<MovieEntry> ParseMovies(JsonArray? arr)
    {
        if (arr is null) return Array.Empty<MovieEntry>();

        return arr.OfType<JsonObject>()
            .Select(item => new MovieEntry
            {
                Id = ReadInt(item, "id") ?? 0,
                Name = ReadString(item, "name") ?? string.Empty,
                ThumbnailUrl = ReadString(item, "thumbnail") ?? string.Empty,
                HlsUrl = ReadString(item, "hls_h264"),
                DashH264Url = ReadString(item, "dash_h264"),
                IsHighlight = ReadBool(item, "highlight")
            })
            .Where(m => !string.IsNullOrWhiteSpace(m.ThumbnailUrl))
            .ToArray();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonArray? arr)
    {
        return arr?.Select(n => n?.GetValue<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToArray() ?? Array.Empty<string>();
    }

    private static IReadOnlyList<string> ParseDescriptionArray(JsonArray? arr)
    {
        return arr?.OfType<JsonObject>()
            .Select(item => ReadString(item, "description"))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToArray() ?? Array.Empty<string>();
    }

    private static string? FirstAssetUrl(JsonObject? assets, string key)
    {
        var arr = assets?[key] as JsonArray;
        var first = arr?.OfType<JsonObject>().FirstOrDefault();
        return ReadString(first, "url");
    }

    private static string? ReadString(JsonObject? node, string propertyName)
    {
        return node is not null &&
               node.TryGetPropertyValue(propertyName, out var value) &&
               value is JsonValue jsonValue &&
               jsonValue.TryGetValue<string>(out var result)
            ? result
            : null;
    }

    private static int? ReadInt(JsonObject? node, string propertyName)
    {
        if (node is null || !node.TryGetPropertyValue(propertyName, out var value) || value is not JsonValue jsonValue)
            return null;

        if (jsonValue.TryGetValue<int>(out var intValue))
            return intValue;

        return jsonValue.TryGetValue<string>(out var text) && int.TryParse(text, out intValue)
            ? intValue
            : null;
    }

    private static bool ReadBool(JsonObject? node, string propertyName)
    {
        return node is not null &&
               node.TryGetPropertyValue(propertyName, out var value) &&
               value is JsonValue jsonValue &&
               jsonValue.TryGetValue<bool>(out var result) &&
               result;
    }

    private static async Task<string?> ReadRawCacheAsync(string path, CancellationToken ct)
    {
        try
        {
            return File.Exists(path) ? await File.ReadAllTextAsync(path, ct) : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<GameDetailEntry?> ApplyDetailOverrideAsync(GameDetailEntry? detail, int appId, CancellationToken ct)
    {
        if (detail is null) return null;

        try
        {
            var ov = await _nexaPlayOverride.GetDetailOverrideAsync(appId, ct);
            if (ov is null) return detail;

            _log.Log("StoreService", $"Applying NexaPlay detail override for appId={appId}");

            return new GameDetailEntry
            {
                AppId = detail.AppId,
                ShortDescription = ov.ShortDescription ?? detail.ShortDescription,
                AboutTheGame = ov.AboutTheGame ?? detail.AboutTheGame,
                DetailedDescription = ov.DetailedDescription ?? detail.DetailedDescription,
                SupportedLanguages = ov.SupportedLanguages ?? detail.SupportedLanguages,
                Website = ov.Website ?? detail.Website,
                Developers = ov.Developers ?? detail.Developers,
                Publishers = ov.Publishers ?? detail.Publishers,
                ReleaseDate = ov.ReleaseDate ?? detail.ReleaseDate,
                Screenshots = ov.Screenshots is not null
                    ? ov.Screenshots.Select((s, i) => new ScreenshotEntry
                      {
                          Id = s.Id > 0 ? s.Id : i,
                          ThumbnailUrl = s.Thumbnail,
                          FullUrl = s.Full
                      }).ToArray()
                    : detail.Screenshots,
                Movies = ov.Movies is not null
                    ? ov.Movies.Select((m, i) => new MovieEntry
                      {
                          Id = m.Id > 0 ? m.Id : i,
                          Name = m.Name ?? $"Movie {i}",
                          ThumbnailUrl = m.Thumbnail,
                          HlsUrl = m.HlsUrl,
                          DashH264Url = m.DashH264Url,
                          IsHighlight = m.IsHighlight
                      }).ToArray()
                    : detail.Movies,
                BackgroundImageUrl = ov.BackgroundImage ?? detail.BackgroundImageUrl,
                PcRequirementsMinimum = ov.PcRequirementsMinimum ?? detail.PcRequirementsMinimum,
                PcRequirementsRecommended = ov.PcRequirementsRecommended ?? detail.PcRequirementsRecommended,
                MacRequirementsMinimum = ov.MacRequirementsMinimum ?? detail.MacRequirementsMinimum,
                MacRequirementsRecommended = ov.MacRequirementsRecommended ?? detail.MacRequirementsRecommended,
                LinuxRequirementsMinimum = ov.LinuxRequirementsMinimum ?? detail.LinuxRequirementsMinimum,
                LinuxRequirementsRecommended = ov.LinuxRequirementsRecommended ?? detail.LinuxRequirementsRecommended,
                Categories = ov.Categories ?? detail.Categories,
                SupportUrl = ov.SupportUrl ?? detail.SupportUrl,
                SupportEmail = ov.SupportEmail ?? detail.SupportEmail,
                LegalNotice = ov.LegalNotice ?? detail.LegalNotice,
                DrmNotice = ov.DrmNotice ?? detail.DrmNotice,
                StorePriceFinalFormatted = ov.StorePriceFinalFormatted ?? detail.StorePriceFinalFormatted,
                StorePriceCurrency = ov.StorePriceCurrency ?? detail.StorePriceCurrency,
                CachedAt = detail.CachedAt,
                IsFromCache = detail.IsFromCache,
                RawMetadataJson = detail.RawMetadataJson
            };
        }
        catch (Exception ex)
        {
            _log.Log("StoreService", $"Detail override failed: {ex.Message}");
            return detail;
        }
    }

    private string CacheFilePath(int appId) => Path.Combine(_cacheDir, $"{appId}.json");
}
