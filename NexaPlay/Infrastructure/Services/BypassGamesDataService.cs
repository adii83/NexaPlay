using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Net.Http;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

/// <summary>Downloads and caches fix_games.json catalog with 24h TTL.</summary>
public sealed class BypassGamesDataService : IBypassGamesDataService
{
    private readonly IAppLogService _log;
    private readonly string _cacheFile;
    private readonly HttpClient _http;
    private IReadOnlyList<FixEntry>? _cached;
    private DateTime _lastLoaded = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public BypassGamesDataService(IAppLogService log)
    {
        _log = log;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);
        _cacheFile = Path.Combine(dir, AppConstants.BypassGamesCacheFileName);
        _http = new HttpClient { Timeout = AppConstants.HttpDefaultTimeout };
    }

    public async Task<IReadOnlyList<FixEntry>> GetAllFixesAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _cached!;
    }

    public async Task<FixEntry?> GetFixAsync(int appId, CancellationToken ct = default)
    {
        var all = await GetAllFixesAsync(ct);
        return all.FirstOrDefault(f => f.AppId == appId);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        _cached = null;
        _lastLoaded = DateTime.MinValue;
        await EnsureLoadedAsync(ct);
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_cached is not null && DateTime.UtcNow - _lastLoaded < AppConstants.BypassGamesCacheTtl) return;
        await _lock.WaitAsync(ct);
        try
        {
            if (_cached is not null && DateTime.UtcNow - _lastLoaded < AppConstants.BypassGamesCacheTtl) return;
            await LoadAsync(ct);
        }
        finally { _lock.Release(); }
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        // Try disk cache
        if (File.Exists(_cacheFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(_cacheFile) < AppConstants.BypassGamesCacheTtl)
        {
            _cached = ParseFromFile(_cacheFile);
            _lastLoaded = DateTime.UtcNow;
            _log.Log("FixData", $"Loaded {_cached.Count} fixes from disk cache");
            return;
        }

        _log.Log("FixData", "Downloading fix_games.json...");
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, AppConstants.BypassGamesUrl);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            await File.WriteAllTextAsync(_cacheFile, json, ct);
            _cached = ParseJson(json);
            _lastLoaded = DateTime.UtcNow;
            _log.Log("FixData", $"Downloaded {_cached.Count} fixes");
        }
        catch (Exception ex)
        {
            _log.Log("FixData", $"Download failed: {ex.Message}. Using disk cache.");
            _cached = File.Exists(_cacheFile) ? ParseFromFile(_cacheFile) : Array.Empty<FixEntry>();
            _lastLoaded = DateTime.UtcNow;
        }
    }

    private static IReadOnlyList<FixEntry> ParseFromFile(string path)
    {
        try { return ParseJson(File.ReadAllText(path)); }
        catch { return Array.Empty<FixEntry>(); }
    }

    private static IReadOnlyList<FixEntry> ParseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("games", out var games)) return Array.Empty<FixEntry>();

        var list = new List<FixEntry>();
        foreach (var g in games.EnumerateArray())
        {
            try
            {
                var appId    = g.TryGetProperty("appid",     out var aid)  ? aid.GetInt32() : 0;
                var title    = g.TryGetProperty("title",     out var t)    ? t.GetString()  ?? "" : "";
                var pub      = g.TryGetProperty("publisher", out var p)    ? p.GetString()  ?? "" : "";
                var cat      = g.TryGetProperty("category",  out var c)    ? c.GetString()  ?? "" : "";
                var poster   = g.TryGetProperty("poster",    out var po)   ? po.GetString() : null;
                var password = g.TryGetProperty("password",  out var pw)   ? pw.GetString() : null;
                var premium  = g.TryGetProperty("premium",   out var pr) && pr.GetBoolean();
                var exeHint  = g.TryGetProperty("exe_hint",  out var ex)   ? ex.GetString() : null;
                var shortcut = g.TryGetProperty("use_shortcut", out var sc) && sc.GetBoolean();

                var files = new List<FixFile>();
                if (g.TryGetProperty("files", out var fa))
                {
                    foreach (var f in fa.EnumerateArray())
                    {
                        files.Add(new FixFile
                        {
                            Part      = f.TryGetProperty("part",       out var pt)  ? pt.GetInt32() : 1,
                            Filename  = f.TryGetProperty("filename",   out var fn)  ? fn.GetString() ?? "" : "",
                            GDriveId  = f.TryGetProperty("gdrive_id",  out var gi)  ? gi.GetString() ?? "" : "",
                            GDriveUrl = f.TryGetProperty("gdrive_url", out var gu)  ? gu.GetString() ?? "" : "",
                        });
                    }
                }

                var category = cat.ToLowerInvariant() switch
                {
                    "ubisoft"    => GameCategory.Ubisoft,
                    "ea"         => GameCategory.EA,
                    "rockstar"   => GameCategory.Rockstar,
                    "playstation"=> GameCategory.PlayStation,
                    "activision" => GameCategory.Activision,
                    _            => GameCategory.Other
                };

                list.Add(new FixEntry
                {
                    AppId = appId, Title = title, Publisher = pub, Category = category,
                    PosterUrl = poster, Password = password, IsPremium = premium,
                    ExeHint = exeHint, UseShortcut = shortcut, Files = files
                });
            }
            catch { /* skip malformed entries */ }
        }
        return list;
    }
}
