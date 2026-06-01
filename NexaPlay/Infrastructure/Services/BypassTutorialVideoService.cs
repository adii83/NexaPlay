using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Loads tutorial-video metadata from remote JSON with ETag-based cache validation.
/// </summary>
public sealed class BypassTutorialVideoService : IBypassTutorialVideoService
{
    private readonly IAppLogService _log;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _cacheFile;
    private readonly string _etagFile;

    private TutorialCatalog _catalog = new();
    private string? _etag;
    private DateTime _lastSyncUtc = DateTime.MinValue;

    private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromMinutes(2);

    public BypassTutorialVideoService(IAppLogService log)
    {
        _log = log;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);

        _cacheFile = Path.Combine(dir, AppConstants.YoutubeTutorialCacheFileName);
        _etagFile = Path.Combine(dir, AppConstants.YoutubeTutorialEtagFileName);
        _etag = SafeReadText(_etagFile);
        _http = new HttpClient { Timeout = AppConstants.HttpDefaultTimeout };
    }

    public async Task<BypassTutorialVideo> GetTutorialVideoAsync(int appId, GameCategory category, CancellationToken ct = default)
    {
        await EnsureCatalogReadyAsync(ct);

        if (_catalog.ByAppId.TryGetValue(appId, out var appSpecific))
            return appSpecific;

        var categoryKey = category == GameCategory.SteamSharing ? "steam-sharing" : "steam-account";
        if (_catalog.ByCategory.TryGetValue(categoryKey, out var byCategory))
            return byCategory;

        return _catalog.Default;
    }

    private async Task EnsureCatalogReadyAsync(CancellationToken ct)
    {
        if (DateTime.UtcNow - _lastSyncUtc < MinRefreshInterval)
            return;

        await _lock.WaitAsync(ct);
        try
        {
            if (DateTime.UtcNow - _lastSyncUtc < MinRefreshInterval)
                return;

            await RefreshCatalogAsync(ct);
            _lastSyncUtc = DateTime.UtcNow;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefreshCatalogAsync(CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, AppConstants.YoutubeTutorialUrl);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
            if (!string.IsNullOrWhiteSpace(_etag))
                req.Headers.TryAddWithoutValidation("If-None-Match", _etag);

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.StatusCode == HttpStatusCode.NotModified)
            {
                if (!LoadFromDiskCache())
                    _catalog = new TutorialCatalog();
                return;
            }

            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            await File.WriteAllTextAsync(_cacheFile, json, ct);

            var etag = resp.Headers.ETag?.Tag;
            if (!string.IsNullOrWhiteSpace(etag))
            {
                _etag = etag;
                await File.WriteAllTextAsync(_etagFile, etag, ct);
            }

            _catalog = ParseCatalog(json);
            _log.Log("BypassTutorial", "youtube.json updated from remote.");
        }
        catch (Exception ex)
        {
            if (!LoadFromDiskCache())
                _catalog = new TutorialCatalog();
            _log.Log("BypassTutorial", $"Remote sync failed, using cache: {ex.Message}");
        }
    }

    private bool LoadFromDiskCache()
    {
        try
        {
            var json = SafeReadText(_cacheFile);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            _catalog = ParseCatalog(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static TutorialCatalog ParseCatalog(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new TutorialCatalog();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Backward compatibility:
            // { "youtubeId": "...", "url": "...", "embedUrl": "..." }
            if (TryParseSingleVideo(root) is { } single)
            {
                return new TutorialCatalog
                {
                    Default = single
                };
            }

            var catalog = new TutorialCatalog
            {
                Default = TryReadVideoNode(root, "default") ?? TutorialCatalog.FallbackDefault
            };

            if (root.TryGetProperty("byCategory", out var byCategory) && byCategory.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in byCategory.EnumerateObject())
                {
                    var key = prop.Name.Trim().ToLowerInvariant();
                    var entry = TryParseSingleVideo(prop.Value);
                    if (entry is not null)
                        catalog.ByCategory[key] = entry;
                }
            }

            if (root.TryGetProperty("byAppId", out var byAppId) && byAppId.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in byAppId.EnumerateObject())
                {
                    if (!int.TryParse(prop.Name, out var appId))
                        continue;
                    var entry = TryParseSingleVideo(prop.Value);
                    if (entry is not null)
                        catalog.ByAppId[appId] = entry;
                }
            }

            return catalog;
        }
        catch
        {
            return new TutorialCatalog();
        }
    }

    private static BypassTutorialVideo? TryReadVideoNode(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var node))
            return TryParseSingleVideo(node);
        return null;
    }

    private static BypassTutorialVideo? TryParseSingleVideo(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return null;

        var title = TryGetString(node, "title") ?? "Tutorial Video";
        var videoIdRaw = TryGetString(node, "videoId")
            ?? TryGetString(node, "youtubeId")
            ?? ExtractVideoIdFromUrl(TryGetString(node, "url"))
            ?? ExtractVideoIdFromUrl(TryGetString(node, "watchUrl"))
            ?? ExtractVideoIdFromUrl(TryGetString(node, "embedUrl"));

        if (!TryNormalizeVideoId(videoIdRaw, out var videoId))
            return null;

        var watchUrl = $"https://www.youtube.com/watch?v={videoId}";
        var embedUrl = $"https://www.youtube.com/embed/{videoId}?autoplay=1&rel=0&modestbranding=1&playsinline=1";
        var thumbnail = $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg";

        return new BypassTutorialVideo
        {
            Title = title,
            VideoId = videoId,
            WatchUrl = watchUrl,
            EmbedUrl = embedUrl,
            ThumbnailUrl = thumbnail
        };
    }

    private static string? TryGetString(JsonElement node, string name)
    {
        if (node.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString();
        return null;
    }

    private static string? ExtractVideoIdFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            var host = uri.Host.ToLowerInvariant();
            if (host.Contains("youtu.be"))
            {
                var pathId = uri.AbsolutePath.Trim('/').Split('/').FirstOrDefault();
                return pathId;
            }

            if (host.Contains("youtube.com"))
            {
                if (uri.AbsolutePath.Contains("/embed/", StringComparison.OrdinalIgnoreCase))
                {
                    var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var embedIndex = Array.FindIndex(segments, s => string.Equals(s, "embed", StringComparison.OrdinalIgnoreCase));
                    if (embedIndex >= 0 && embedIndex + 1 < segments.Length)
                        return segments[embedIndex + 1];
                }

                var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in query)
                {
                    var kv = item.Split('=', 2);
                    if (kv.Length == 2 && kv[0] == "v")
                        return Uri.UnescapeDataString(kv[1]);
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static bool TryNormalizeVideoId(string? source, out string videoId)
    {
        videoId = string.Empty;
        if (string.IsNullOrWhiteSpace(source))
            return false;

        var match = Regex.Match(source.Trim(), @"^[A-Za-z0-9_-]{6,20}$");
        if (!match.Success)
            return false;

        videoId = match.Value;
        return true;
    }

    private static string? SafeReadText(string path)
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

    private sealed class TutorialCatalog
    {
        public static readonly BypassTutorialVideo FallbackDefault = new()
        {
            Title = "Tutorial Video",
            VideoId = "lkETeFanN7c",
            WatchUrl = "https://www.youtube.com/watch?v=lkETeFanN7c",
            EmbedUrl = "https://www.youtube.com/embed/lkETeFanN7c?autoplay=1&rel=0&modestbranding=1&playsinline=1",
            ThumbnailUrl = "https://img.youtube.com/vi/lkETeFanN7c/maxresdefault.jpg"
        };

        public BypassTutorialVideo Default { get; init; } = FallbackDefault;
        public Dictionary<string, BypassTutorialVideo> ByCategory { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, BypassTutorialVideo> ByAppId { get; } = new();
    }
}
