using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Resolves rich game detail on demand and stores a full merged raw payload per appid.
/// Shape intentionally follows the Python metadata generator output.
/// </summary>
public sealed partial class SteamStoreService : ISteamStoreService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);
    private const string SteamAppDetailsUrl = "https://store.steampowered.com/api/appdetails";
    private const string SteamGridDbApiV2 = "https://www.steamgriddb.com/api/v2";
    private const string SteamGridDbPublic = "https://www.steamgriddb.com/api/public";
    private const string Language = "english";
    private const string Country = "us";

    private static readonly JsonSerializerOptions CompactJson = new()
    {
        WriteIndented = false
    };

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
            _log.Log("StoreService", $"Resolving merged detail appId={appId}");
            var raw = await BuildMergedMetadataJsonAsync(appId, ct);
            await File.WriteAllTextAsync(cacheFile, raw, ct);
            return await ApplyDetailOverrideAsync(ParseMergedDetail(appId, raw, isFromCache: false), appId, ct);
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

    private async Task<string> BuildMergedMetadataJsonAsync(int appId, CancellationToken ct)
    {
        var failures = new JsonArray();
        JsonObject? storeData = null;
        JsonObject originalAssets = BuildOriginalAssetsFailure(appId, "not_started", "Not fetched");

        try
        {
            storeData = await FetchSteamAppDetailsAsync(appId, ct);
            if (storeData is not null)
                storeData["embedded_media"] = BuildEmbeddedMedia(storeData);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failures.Add(BuildFailure(appId, "steam_appdetails", ex.Message));
        }

        try
        {
            originalAssets = await FetchOriginalAssetsBySteamAppidAsync(appId, ct);
            if (originalAssets["success"]?.GetValue<bool>() != true)
                failures.Add(BuildFailure(appId, ReadString(originalAssets, "error_stage") ?? "steam_original_assets", ReadString(originalAssets, "error") ?? "Unknown error"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            originalAssets = BuildOriginalAssetsFailure(appId, "steam_original_assets", ex.Message);
            failures.Add(BuildFailure(appId, "steam_original_assets", ex.Message));
        }

        var mergedAssets = CloneObject(originalAssets["assets"] as JsonObject) ?? new JsonObject();
        string? storeName = null;

        if (storeData is not null)
        {
            storeName = ReadString(storeData, "name");
            var storeAssets = BuildStoreAssets(storeData);
            foreach (var item in storeAssets)
                mergedAssets[item.Key] = item.Value?.DeepClone();

            storeData.Remove("name");
            storeData.Remove("steam_appid");
            storeData.Remove("header_image");
            storeData = ReorderStoreData(storeData);
        }

        mergedAssets = ReorderAssets(mergedAssets);

        var payload = new JsonObject
        {
            ["success"] = storeData is not null || originalAssets["success"]?.GetValue<bool>() == true,
            ["source_priority"] = "api_on_demand",
            ["fetch_status"] = new JsonObject
            {
                ["steam_appdetails"] = storeData is not null,
                ["steam_original_assets"] = originalAssets["success"]?.GetValue<bool>() == true
            },
            ["name"] = ReadString(originalAssets, "name") ?? storeName,
            ["steam_appid"] = ReadString(originalAssets, "steam_appid") ?? appId.ToString(),
            ["steamgriddb_game_id"] = originalAssets["steamgriddb_game_id"]?.DeepClone(),
            ["store_asset_mtime"] = originalAssets["store_asset_mtime"]?.DeepClone(),
            ["assets_count"] = CountAssets(mergedAssets),
            ["assets"] = mergedAssets,
            ["store_data"] = storeData ?? new JsonObject()
        };

        if (failures.Count > 0)
            payload["failures"] = failures;

        return payload.ToJsonString(CompactJson);
    }

    private async Task<JsonObject?> FetchSteamAppDetailsAsync(int appId, CancellationToken ct)
    {
        var url = $"{SteamAppDetailsUrl}?appids={appId}&l={Language}&cc={Country}";
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty(appId.ToString(), out var appNode) ||
            !appNode.TryGetProperty("success", out var success) ||
            success.ValueKind != JsonValueKind.True ||
            !appNode.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return JsonNode.Parse(data.GetRawText()) as JsonObject;
    }

    private async Task<JsonObject> FetchOriginalAssetsBySteamAppidAsync(int appId, CancellationToken ct)
    {
        var apiKey = LoadSteamGridDbApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return BuildOriginalAssetsFailure(appId, "steamgriddb_lookup", "STEAMGRIDDB_API_KEY not found");

        var sgdbGame = await GetSteamGridDbGameFromSteamAppidAsync(appId, apiKey, ct);
        if (sgdbGame is null)
            return BuildOriginalAssetsFailure(appId, "steamgriddb_lookup", "Steam appid not found in SteamGridDB");

        var sgdbGameId = ReadInt(sgdbGame, "id");
        if (sgdbGameId is null)
            return BuildOriginalAssetsFailure(appId, "steamgriddb_lookup", "SteamGridDB game id missing");

        try
        {
            var publicPayload = await FetchPublicGameAsync(sgdbGameId.Value, ct);
            var extracted = ExtractOriginalAssets(publicPayload);
            if (extracted is null)
            {
                return new JsonObject
                {
                    ["success"] = false,
                    ["steam_appid"] = appId.ToString(),
                    ["steamgriddb_game_id"] = sgdbGameId.Value,
                    ["name"] = ReadString(sgdbGame, "name"),
                    ["error_stage"] = "steamgriddb_extract",
                    ["error"] = "Original Steam metadata not found",
                    ["assets_count"] = 0,
                    ["assets"] = new JsonObject()
                };
            }

            extracted["name"] ??= ReadString(sgdbGame, "name");
            return extracted;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new JsonObject
            {
                ["success"] = false,
                ["steam_appid"] = appId.ToString(),
                ["steamgriddb_game_id"] = sgdbGameId.Value,
                ["name"] = ReadString(sgdbGame, "name"),
                ["error_stage"] = "steamgriddb_public",
                ["error"] = ex.Message,
                ["assets_count"] = 0,
                ["assets"] = new JsonObject()
            };
        }
    }

    private async Task<JsonObject?> GetSteamGridDbGameFromSteamAppidAsync(int appId, string apiKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{SteamGridDbApiV2}/games/steam/{appId}");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _http.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var root = JsonNode.Parse(json) as JsonObject;
        return root?["success"]?.GetValue<bool>() == true
            ? root["data"] as JsonObject
            : null;
    }

    private async Task<JsonObject> FetchPublicGameAsync(int sgdbGameId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{SteamGridDbPublic}/game/{sgdbGameId}");
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        request.Headers.TryAddWithoutValidation("Referer", $"https://www.steamgriddb.com/game/{sgdbGameId}/grids");
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0");

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
    }

    private static JsonObject? ExtractOriginalAssets(JsonObject publicPayload)
    {
        var data = publicPayload["data"] as JsonObject;
        var steamData = data?["platforms"]?["steam"] as JsonObject;
        var metadata = steamData?["metadata"] as JsonObject;
        var steamAppid = steamData?["id"]?.ToString();

        if (string.IsNullOrWhiteSpace(steamAppid) || metadata is null)
            return null;

        var mtime = ReadLong(metadata, "store_asset_mtime");
        var assets = new JsonObject();

        AddEnglishAssetsFromDict(assets, "header", steamAppid, metadata["header_image_full"] as JsonObject, mtime);

        var capsule = metadata["library_capsule_full"] as JsonObject;
        AddEnglishAssetsFromDict(assets, "library_capsule", steamAppid, capsule?["image"] as JsonObject, mtime);
        AddEnglishAssetsFromDict(assets, "library_capsule_2x", steamAppid, capsule?["image2x"] as JsonObject, mtime);

        var hero = metadata["library_hero_full"] as JsonObject;
        AddEnglishAssetsFromDict(assets, "library_hero", steamAppid, hero?["image"] as JsonObject, mtime);
        AddEnglishAssetsFromDict(assets, "library_hero_2x", steamAppid, hero?["image2x"] as JsonObject, mtime);

        var logo = metadata["library_logo_full"] as JsonObject;
        AddEnglishAssetsFromDict(assets, "library_logo", steamAppid, logo?["image"] as JsonObject, mtime);
        AddEnglishAssetsFromDict(assets, "library_logo_2x", steamAppid, logo?["image2x"] as JsonObject, mtime);

        var clientIcon = ReadString(metadata, "clienticon");
        if (!string.IsNullOrWhiteSpace(clientIcon))
        {
            assets["clienticon"] = new JsonArray(new JsonObject
            {
                ["hash"] = clientIcon,
                ["url"] = $"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{steamAppid}/{clientIcon}.ico"
            });
        }

        var icon = ReadString(metadata, "icon");
        if (!string.IsNullOrWhiteSpace(icon))
        {
            assets["icon"] = new JsonArray(new JsonObject
            {
                ["hash"] = icon,
                ["url"] = $"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{steamAppid}/{icon}.jpg"
            });
        }

        return new JsonObject
        {
            ["success"] = true,
            ["name"] = ReadString(data, "name") ?? ReadString(steamData, "name"),
            ["steam_appid"] = steamAppid,
            ["steamgriddb_game_id"] = steamData?["gameId"]?.DeepClone(),
            ["store_asset_mtime"] = mtime,
            ["assets_count"] = CountAssets(assets),
            ["assets"] = assets
        };
    }

    private static void AddEnglishAssetsFromDict(JsonObject assets, string key, string steamAppid, JsonObject? dataDict, long? mtime)
    {
        var path = ReadString(dataDict, Language);
        if (string.IsNullOrWhiteSpace(path))
            return;

        assets[key] = new JsonArray(new JsonObject
        {
            ["path"] = path,
            ["url"] = BuildSteamAssetUrl(steamAppid, path, mtime),
            ["language"] = Language
        });
    }

    private static JsonObject BuildStoreAssets(JsonObject storeData)
    {
        var result = new JsonObject
        {
            ["capsule_image"] = WrapSingleAsset(RemoveNode(storeData, "capsule_image")?.ToString()),
            ["capsule_imagev5"] = WrapSingleAsset(RemoveNode(storeData, "capsule_imagev5")?.ToString()),
            ["background"] = WrapSingleAsset(RemoveNode(storeData, "background")?.ToString()),
            ["background_raw"] = WrapSingleAsset(RemoveNode(storeData, "background_raw")?.ToString()),
            ["screenshots"] = RemoveNode(storeData, "screenshots") ?? new JsonArray(),
            ["movies"] = RemoveNode(storeData, "movies") ?? new JsonArray(),
            ["embedded_media"] = RemoveNode(storeData, "embedded_media") ?? new JsonObject()
        };

        return result;
    }

    private static JsonObject BuildEmbeddedMedia(JsonObject storeData)
    {
        var aboutUrls = ExtractUrlsFromHtml(ReadString(storeData, "about_the_game"));
        var detailUrls = ExtractUrlsFromHtml(ReadString(storeData, "detailed_description"));
        var all = aboutUrls.Concat(detailUrls).Distinct(StringComparer.Ordinal).ToArray();

        return new JsonObject
        {
            ["about_the_game_urls"] = ToJsonArray(aboutUrls),
            ["detailed_description_urls"] = ToJsonArray(detailUrls),
            ["all_urls"] = ToJsonArray(all),
            ["videos_mp4"] = ToJsonArray(all.Where(u => u.Contains(".mp4", StringComparison.OrdinalIgnoreCase))),
            ["videos_webm"] = ToJsonArray(all.Where(u => u.Contains(".webm", StringComparison.OrdinalIgnoreCase))),
            ["images"] = ToJsonArray(all.Where(u => ImageUrlRegex().IsMatch(u)))
        };
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

    private static JsonObject ReorderAssets(JsonObject assets)
    {
        string[] preferred =
        [
            "header", "library_capsule", "library_capsule_2x", "library_hero", "library_hero_2x",
            "library_logo", "library_logo_2x", "clienticon", "icon", "capsule_image",
            "capsule_imagev5", "background", "background_raw", "screenshots", "movies", "embedded_media"
        ];

        return ReorderObject(assets, preferred);
    }

    private static JsonObject ReorderStoreData(JsonObject storeData)
    {
        string[] preferred =
        [
            "type", "required_age", "is_free", "controller_support", "dlc", "demos",
            "about_the_game", "detailed_description", "short_description", "supported_languages",
            "website", "pc_requirements", "mac_requirements", "linux_requirements", "developers",
            "publishers", "support_info", "legal_notice", "drm_notice", "price_overview",
            "packages", "package_groups", "platforms", "categories", "genres", "recommendations",
            "achievements", "release_date", "content_descriptors", "ratings"
        ];

        return ReorderObject(storeData, preferred);
    }

    private static JsonObject ReorderObject(JsonObject source, IEnumerable<string> preferred)
    {
        var result = new JsonObject();
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in preferred)
        {
            var node = RemoveNode(source, key);
            if (node is not null)
            {
                result[key] = node;
                used.Add(key);
            }
        }

        foreach (var item in source.ToArray())
        {
            if (!used.Contains(item.Key))
            {
                source.Remove(item.Key);
                result[item.Key] = item.Value;
            }
        }

        return result;
    }

    private static JsonNode? RemoveNode(JsonObject source, string key)
    {
        if (!source.TryGetPropertyValue(key, out var node))
            return null;

        source.Remove(key);
        return node;
    }

    private static JsonArray WrapSingleAsset(string? url)
    {
        return string.IsNullOrWhiteSpace(url)
            ? new JsonArray()
            : new JsonArray(new JsonObject { ["url"] = url });
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var arr = new JsonArray();
        foreach (var value in values)
            arr.Add(value);
        return arr;
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

    private static int CountAssets(JsonObject assets)
    {
        var total = 0;
        foreach (var item in assets)
        {
            if (item.Value is JsonArray arr)
                total += arr.Count;
            else if (item.Value is not null)
                total++;
        }
        return total;
    }

    private static JsonObject? CloneObject(JsonObject? source)
    {
        return source?.DeepClone() as JsonObject;
    }

    private static JsonObject BuildOriginalAssetsFailure(int appId, string stage, string error)
    {
        return new JsonObject
        {
            ["success"] = false,
            ["steam_appid"] = appId.ToString(),
            ["error_stage"] = stage,
            ["error"] = error,
            ["assets_count"] = 0,
            ["assets"] = new JsonObject()
        };
    }

    private static JsonObject BuildFailure(int appId, string stage, string error)
    {
        return new JsonObject
        {
            ["appid"] = appId,
            ["stage"] = stage,
            ["error"] = error,
            ["source_priority"] = "api_on_demand",
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
        };
    }

    private static string? BuildSteamAssetUrl(string steamAppid, string assetPath, long? mtime)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return null;

        return $"https://shared.steamstatic.com/store_item_assets/steam/apps/{steamAppid}/{assetPath}?t={mtime}";
    }

    private static IReadOnlyList<string> ExtractUrlsFromHtml(string? rawHtml)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
            return Array.Empty<string>();

        return UrlRegex().Matches(rawHtml)
            .Select(m => m.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
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

    private static long? ReadLong(JsonObject? node, string propertyName)
    {
        if (node is null || !node.TryGetPropertyValue(propertyName, out var value) || value is not JsonValue jsonValue)
            return null;

        if (jsonValue.TryGetValue<long>(out var longValue))
            return longValue;

        return jsonValue.TryGetValue<string>(out var text) && long.TryParse(text, out longValue)
            ? longValue
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

    private static string? LoadSteamGridDbApiKey()
    {
        var env = Environment.GetEnvironmentVariable("STEAMGRIDDB_API_KEY");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        foreach (var file in CandidateEnvFiles())
        {
            if (!File.Exists(file))
                continue;

            foreach (var raw in File.ReadLines(file))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = line[..separator].Trim();
                if (!key.Equals("STEAMGRIDDB_API_KEY", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = line[(separator + 1)..].Trim().Trim('"');
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateEnvFiles()
    {
        var dirs = new List<string?>
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        var cursor = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && cursor is not null; i++, cursor = cursor.Parent)
            dirs.Add(cursor.FullName);

        cursor = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 8 && cursor is not null; i++, cursor = cursor.Parent)
            dirs.Add(cursor.FullName);

        return dirs
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(d => Path.Combine(d!, ".env"));
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
                          Name = m.Name,
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
            _log.Log("StoreService", $"NexaPlay detail override failed (non-blocking) appId={appId}: {ex.Message}");
            return detail;
        }
    }


    private string CacheFilePath(int appId) => Path.Combine(_cacheDir, $"{appId}.json");

    [GeneratedRegex("https://[^\\\"'>\\s]+", RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    [GeneratedRegex("\\.(jpg|jpeg|png|gif|webp|avif)(\\?|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ImageUrlRegex();
}
