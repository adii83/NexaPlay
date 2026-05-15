using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Models;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

/// <summary>Downloads, caches, and indexes steam_data.json.gz from GitHub.
/// Uses ETag/LastModified for bandwidth-efficient updates and builds an
/// in-memory dictionary for O(1) lookups — same strategy as GameHub GitHubRawService.</summary>
public sealed class MetadataService : IMetadataService
{
    private readonly IAppLogService _log;
    private readonly string _cacheFile;
    private readonly string _etagFile;
    private readonly HttpClient _http;
    private Dictionary<int, GameEntry>? _index;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private DateTime _lastLoaded = DateTime.MinValue;

    public MetadataService(IAppLogService log)
    {
        _log = log;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);
        _cacheFile = Path.Combine(dir, AppConstants.SteamDataCacheFileName);
        _etagFile  = Path.Combine(dir, AppConstants.SteamDataCacheFileName + ".etag");
        _http = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip })
        {
            Timeout = AppConstants.HttpDefaultTimeout
        };
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
        var q = query.ToLowerInvariant();
        return _index!.Values
            .Where(g => g.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
    }

    public async Task RefreshAsync(bool forceDownload = false, CancellationToken ct = default)
    {
        await LoadFromGitHubAsync(force: forceDownload, ct: ct);
    }

    public async Task ClearCacheAsync()
    {
        _index = null;
        _lastLoaded = DateTime.MinValue;
        try { if (File.Exists(_cacheFile)) File.Delete(_cacheFile); } catch { }
        try { if (File.Exists(_etagFile))  File.Delete(_etagFile); }  catch { }
        _log.Log("Metadata", "Cache cleared");
    }

    private async Task EnsureIndexedAsync(CancellationToken ct)
    {
        if (_index is not null && DateTime.UtcNow - _lastLoaded < AppConstants.SteamDataCacheTtl) return;
        await _loadLock.WaitAsync(ct);
        try
        {
            if (_index is not null && DateTime.UtcNow - _lastLoaded < AppConstants.SteamDataCacheTtl) return;
            await LoadFromGitHubAsync(force: false, ct: ct);
        }
        finally { _loadLock.Release(); }
    }

    private async Task LoadFromGitHubAsync(bool force, CancellationToken ct)
    {
        // Try disk cache first
        if (!force && File.Exists(_cacheFile))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(_cacheFile);
            if (age < AppConstants.SteamDataCacheTtl)
            {
                await BuildIndexFromCacheAsync(ct);
                return;
            }
        }

        _log.Log("Metadata", "Downloading steam_data.json.gz from GitHub...");
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, AppConstants.SteamDataUrl);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");

            // Conditional request with ETag
            if (!force && File.Exists(_etagFile))
            {
                var etag = await File.ReadAllTextAsync(_etagFile, ct);
                if (!string.IsNullOrWhiteSpace(etag))
                    req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag, true));
            }

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (resp.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                _log.Log("Metadata", "Cache is up-to-date (304 Not Modified)");
                await BuildIndexFromCacheAsync(ct);
                return;
            }

            if (!resp.IsSuccessStatusCode)
            {
                _log.Log("Metadata", $"HTTP error {resp.StatusCode}, falling back to disk cache");
                await BuildIndexFromCacheAsync(ct);
                return;
            }

            // Save ETag
            var responseEtag = resp.Headers.ETag?.Tag;
            if (!string.IsNullOrEmpty(responseEtag))
                await File.WriteAllTextAsync(_etagFile, responseEtag, ct);

            // The HTTP client already decompresses gzip via handler
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            await using var fs = File.Create(_cacheFile);
            await stream.CopyToAsync(fs, ct);

            _log.Log("Metadata", "Download complete, building index...");
            await BuildIndexFromCacheAsync(ct);
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Download error: {ex.Message}. Using disk cache if available.");
            await BuildIndexFromCacheAsync(ct);
        }
    }

    private async Task BuildIndexFromCacheAsync(CancellationToken ct)
    {
        if (!File.Exists(_cacheFile)) { _index = new(); return; }
        try
        {
            await using var fs = File.OpenRead(_cacheFile);
            using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: ct);
            var dict = new Dictionary<int, GameEntry>(capacity: 100_000);

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("appid", out var appidProp) || !appidProp.TryGetInt32(out var appid)) continue;
                var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                var dev  = item.TryGetProperty("developer", out var d) ? d.GetString() : null;
                var pub  = item.TryGetProperty("publisher", out var p) ? p.GetString() : null;

                dict[appid] = new GameEntry { AppId = appid, Name = name, Developer = dev, Publisher = pub };
            }

            _index = dict;
            _lastLoaded = DateTime.UtcNow;
            _log.Log("Metadata", $"Index built: {_index.Count:N0} games");
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Index build error: {ex.Message}");
            _index = new();
        }
    }
}
