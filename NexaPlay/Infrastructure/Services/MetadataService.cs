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
    private readonly string _overrideCacheFile;
    private readonly string _overrideEtagFile;
    private readonly string _userOverrideCacheFile;
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
        _overrideCacheFile = Path.Combine(dir, AppConstants.OverrideDataCacheFileName);
        _overrideEtagFile = Path.Combine(dir, AppConstants.OverrideDataCacheFileName + ".etag");
        _userOverrideCacheFile = Path.Combine(dir, AppConstants.UserOverrideDataCacheFileName);
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

    private IReadOnlyList<int>? _popularAppIdsCache;
    private DateTime _popularLastLoaded = DateTime.MinValue;

    public async Task<IReadOnlyList<int>> GetPopularAppIdsAsync(CancellationToken ct = default)
    {
        if (_popularAppIdsCache is not null && DateTime.UtcNow - _popularLastLoaded < AppConstants.BypassGamesCacheTtl)
            return _popularAppIdsCache;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, AppConstants.PopularGamesUrl);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                var arr = JsonSerializer.Deserialize<int[]>(json) ?? Array.Empty<int>();
                _popularAppIdsCache = arr;
                _popularLastLoaded = DateTime.UtcNow;
                return arr;
            }
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Popular AppIds download error: {ex.Message}");
        }

        return _popularAppIdsCache ?? Array.Empty<int>();
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
        try { if (File.Exists(_overrideCacheFile)) File.Delete(_overrideCacheFile); } catch { }
        try { if (File.Exists(_overrideEtagFile))  File.Delete(_overrideEtagFile); }  catch { }
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
            await LoadOverrideFromGitHubAsync(force: false, ct: ct);
            await LoadSteamGamesFromGitHubAsync(force: false, ct: ct);
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

    private async Task LoadOverrideFromGitHubAsync(bool force, CancellationToken ct)
    {
        if (!force && File.Exists(_overrideCacheFile))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(_overrideCacheFile);
            if (age < AppConstants.OverrideDataCacheTtl) return;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, AppConstants.OverrideDataUrl);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");

            if (!force && File.Exists(_overrideEtagFile))
            {
                var etag = await File.ReadAllTextAsync(_overrideEtagFile, ct);
                if (!string.IsNullOrWhiteSpace(etag)) req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag, true));
            }

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotModified) return;

            if (resp.IsSuccessStatusCode)
            {
                var responseEtag = resp.Headers.ETag?.Tag;
                if (!string.IsNullOrEmpty(responseEtag)) await File.WriteAllTextAsync(_overrideEtagFile, responseEtag, ct);

                var json = await resp.Content.ReadAsStringAsync(ct);
                await File.WriteAllTextAsync(_overrideCacheFile, json, ct);
            }
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Override download error: {ex.Message}");
        }
    }

    private async Task LoadSteamGamesFromGitHubAsync(bool force, CancellationToken ct)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.AppDataFolder);
        var steamGamesCacheFile = Path.Combine(dir, AppConstants.SteamGamesCacheFileName);

        if (!force && File.Exists(steamGamesCacheFile))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(steamGamesCacheFile);
            if (age < AppConstants.OverrideDataCacheTtl) return;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, AppConstants.SteamGamesUrl);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                await File.WriteAllTextAsync(steamGamesCacheFile, json, ct);
            }
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Steam Games download error: {ex.Message}");
        }
    }

    private async Task BuildIndexFromCacheAsync(CancellationToken ct)
    {
        if (!File.Exists(_cacheFile)) { _index = new(); return; }
        try
        {
            await using var fs = File.OpenRead(_cacheFile);
            await using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var doc = await JsonDocument.ParseAsync(gz, cancellationToken: ct);
            var dict = new Dictionary<int, GameEntry>(capacity: 100_000);

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                var item = property.Value;
                if (!item.TryGetProperty("appid", out var appidProp) || !appidProp.TryGetInt32(out var appid)) continue;
                var name = item.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(name)) 
                    name = item.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;

                var headerUrl = item.TryGetProperty("header", out var h) ? h.GetString() : null;
                var dev  = item.TryGetProperty("developer", out var d) ? d.GetString() : null;
                var pub  = item.TryGetProperty("publisher", out var p) ? p.GetString() : null;

                // Price checking logic
                var priceNorm = item.TryGetProperty("price_normalized", out var pNorm) ? (pNorm.ValueKind == JsonValueKind.Number ? pNorm.GetInt32() : 0) : 0;
                var priceInit = item.TryGetProperty("price_initial", out var pInit) ? (pInit.ValueKind == JsonValueKind.Number ? pInit.GetInt32() : 0) : 0;
                var price = Math.Max(priceNorm, priceInit);

                // Protection checking logic (can be true or a string containing 'denuvo')
                var protection = false;
                if (item.TryGetProperty("protection", out var prot))
                {
                    if (prot.ValueKind == JsonValueKind.True) protection = true;
                    else if (prot.ValueKind == JsonValueKind.String && prot.GetString()?.Contains("Denuvo", StringComparison.OrdinalIgnoreCase) == true) protection = true;
                }

                dict[appid] = new GameEntry 
                { 
                    AppId = appid, 
                    Name = name, 
                    Developer = dev, 
                    Publisher = pub,
                    PriceNormalized = price,
                    Protection = protection,
                    HeaderImageUrl = headerUrl! // null fallback handled in GameEntry.cs
                };
            }

            // 1. Auto-Denuvo from Fix Games & Steam Games
            await ApplyAutoDenuvoFromListsAsync(dict, ct);

            // 2. Global Override Strategy (menimpa auto-denuvo jika diperlukan)
            await ApplyOverrideDataAsync(dict, _overrideCacheFile, ct);
            
            // 3. User Override Strategy (prioritas tertinggi)
            await ApplyOverrideDataAsync(dict, _userOverrideCacheFile, ct);

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

    private async Task ApplyOverrideDataAsync(Dictionary<int, GameEntry> dict, string overrideFile, CancellationToken ct)
    {
        if (!File.Exists(overrideFile)) return;
        try
        {
            var overrideJson = await File.ReadAllTextAsync(overrideFile, ct);
            if (string.IsNullOrWhiteSpace(overrideJson)) return;

            using var doc = JsonDocument.Parse(overrideJson);
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                var item = property.Value;
                if (!item.TryGetProperty("appid", out var appidProp) || !appidProp.TryGetInt32(out var appid)) continue;

                if (!dict.TryGetValue(appid, out var entry))
                {
                    entry = new GameEntry { AppId = appid };
                    dict[appid] = entry;
                }

                if (item.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                    entry.Name = t.GetString() ?? entry.Name;
                else if (item.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                    entry.Name = n.GetString() ?? entry.Name;

                if (item.TryGetProperty("header", out var h) && h.ValueKind == JsonValueKind.String)
                    entry.HeaderImageUrl = h.GetString() ?? entry.HeaderImageUrl;

                if (item.TryGetProperty("protection", out var prot))
                {
                    if (prot.ValueKind == JsonValueKind.True) entry.Protection = true;
                    else if (prot.ValueKind == JsonValueKind.False || prot.ValueKind == JsonValueKind.Null) entry.Protection = false;
                    else if (prot.ValueKind == JsonValueKind.String && prot.GetString()?.Contains("Denuvo", StringComparison.OrdinalIgnoreCase) == true) entry.Protection = true;
                }

                var priceNorm = item.TryGetProperty("price_normalized", out var pNorm) ? (pNorm.ValueKind == JsonValueKind.Number ? pNorm.GetInt32() : -1) : -1;
                var priceInit = item.TryGetProperty("price_initial", out var pInit) ? (pInit.ValueKind == JsonValueKind.Number ? pInit.GetInt32() : -1) : -1;
                var price = Math.Max(priceNorm, priceInit);
                if (price >= 0) entry.PriceNormalized = price;
            }
            _log.Log("Metadata", $"Applied overrides from {Path.GetFileName(overrideFile)}");
        }
        catch (Exception ex)
        {
            _log.Log("Metadata", $"Error applying override {Path.GetFileName(overrideFile)}: {ex.Message}");
        }
    }

    private async Task ApplyAutoDenuvoFromListsAsync(Dictionary<int, GameEntry> dict, CancellationToken ct)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.AppDataFolder);
        
        // 1. Fix Games
        var fixCacheFile = Path.Combine(dir, AppConstants.BypassGamesCacheFileName);
        if (File.Exists(fixCacheFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(fixCacheFile, ct);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("games", out var games))
                {
                    foreach (var game in games.EnumerateArray())
                    {
                        if (game.TryGetProperty("appid", out var appidProp) && appidProp.TryGetInt32(out var appid))
                        {
                            if (dict.TryGetValue(appid, out var entry)) entry.Protection = true;
                        }
                    }
                }
            }
            catch (Exception ex) { _log.Log("Metadata", $"Error applying Fix Games auto-protection: {ex.Message}"); }
        }

        // 2. Steam Games
        var steamGamesCacheFile = Path.Combine(dir, AppConstants.SteamGamesCacheFileName);
        if (File.Exists(steamGamesCacheFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(steamGamesCacheFile, ct);
                using var doc = JsonDocument.Parse(json);
                
                var root = doc.RootElement;
                var gamesArray = root.ValueKind == JsonValueKind.Array ? root : 
                                (root.TryGetProperty("games", out var g) ? g : default);

                if (gamesArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var game in gamesArray.EnumerateArray())
                    {
                        if (game.TryGetProperty("appid", out var appidProp) && appidProp.TryGetInt32(out var appid))
                        {
                            if (dict.TryGetValue(appid, out var entry)) entry.Protection = true;
                        }
                    }
                }
            }
            catch (Exception ex) { _log.Log("Metadata", $"Error applying Steam Games auto-protection: {ex.Message}"); }
        }
        
        _log.Log("Metadata", "Applied auto-protection flags from Fix Games & Steam Games lists");
    }
}
