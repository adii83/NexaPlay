using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Net.Http;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

/// <summary>Downloads and caches fix_games.json catalog with 24h TTL.
/// Also loads steam_games.json for the Steam Sharing tab.</summary>
public sealed class BypassGamesDataService : IBypassGamesDataService
{
    private readonly IAppLogService _log;
    private readonly string _cacheFile;
    private readonly string _newCacheFile;
    private readonly string _steamGamesFile;
    private readonly HttpClient _http;
    private IReadOnlyList<FixEntry>? _cached;
    private IReadOnlyList<FixEntry>? _newCached;
    private IReadOnlyList<FixEntry>? _steamCached;
    private DateTime _lastLoaded = DateTime.MinValue;
    private DateTime _newLastLoaded = DateTime.MinValue;
    private DateTime _steamLastLoaded = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public BypassGamesDataService(IAppLogService log)
    {
        _log = log;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);
        _cacheFile = Path.Combine(dir, AppConstants.BypassGamesCacheFileName);
        _newCacheFile = Path.Combine(dir, AppConstants.NewFixGamesCacheFileName);
        _steamGamesFile = Path.Combine(dir, AppConstants.SteamGamesCacheFileName);
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

    public async Task<IReadOnlyList<FixEntry>> GetNewFixesAsync(CancellationToken ct = default)
    {
        await EnsureNewLoadedAsync(ct);
        return _newCached!;
    }

    /// <summary>Load steam_games.json (Steam Account + Steam Sharing games)</summary>
    public async Task<IReadOnlyList<FixEntry>> GetSteamGamesAsync(CancellationToken ct = default)
    {
        await EnsureSteamLoadedAsync(ct);
        return _steamCached!;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        _cached = null;
        _newCached = null;
        _steamCached = null;
        _lastLoaded = DateTime.MinValue;
        _newLastLoaded = DateTime.MinValue;
        _steamLastLoaded = DateTime.MinValue;
        await EnsureLoadedAsync(ct);
        await EnsureNewLoadedAsync(ct);
        await EnsureSteamLoadedAsync(ct);
    }

    public void UpdateCacheItem(FixEntry updatedEntry)
    {
        // Update in _cached
        if (_cached is not null)
        {
            var list = _cached.ToList();
            var idx = list.FindIndex(f => f.AppId == updatedEntry.AppId && (updatedEntry.AppId != 0 || f.Title == updatedEntry.Title));
            if (idx >= 0)
            {
                list[idx] = updatedEntry;
                _cached = list;
            }
        }
        
        // Update in _steamCached
        if (_steamCached is not null)
        {
            var list = _steamCached.ToList();
            var idx = list.FindIndex(f => f.AppId == updatedEntry.AppId && (updatedEntry.AppId != 0 || f.Title == updatedEntry.Title));
            if (idx >= 0)
            {
                list[idx] = updatedEntry;
                _steamCached = list;
            }
        }
    }

    // ─── fix_games.json ────────────────────────────────────────────

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_cached is not null && DateTime.UtcNow - _lastLoaded < AppConstants.SafetyNetTtl) return;
        await _lock.WaitAsync(ct);
        try
        {
            if (_cached is not null && DateTime.UtcNow - _lastLoaded < AppConstants.SafetyNetTtl) return;
            await LoadFixGamesAsync(ct);
        }
        finally { _lock.Release(); }
    }

    private async Task LoadFixGamesAsync(CancellationToken ct)
    {
        if (File.Exists(_cacheFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(_cacheFile) < AppConstants.SafetyNetTtl)
        {
            _cached = ParseFixGamesJson(File.ReadAllText(_cacheFile));
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
            _cached = ParseFixGamesJson(json);
            _lastLoaded = DateTime.UtcNow;
            _log.Log("FixData", $"Downloaded {_cached.Count} fixes");
        }
        catch (Exception ex)
        {
            _log.Log("FixData", $"Download failed: {ex.Message}. Using disk cache.");
            _cached = File.Exists(_cacheFile) ? ParseFixGamesJson(File.ReadAllText(_cacheFile)) : Array.Empty<FixEntry>();
            _lastLoaded = DateTime.UtcNow;
        }
    }

    // ─── new_fix_games.json ────────────────────────────────────────

    private async Task EnsureNewLoadedAsync(CancellationToken ct)
    {
        if (_newCached is not null && DateTime.UtcNow - _newLastLoaded < AppConstants.SafetyNetTtl) return;
        await _lock.WaitAsync(ct);
        try
        {
            if (_newCached is not null && DateTime.UtcNow - _newLastLoaded < AppConstants.SafetyNetTtl) return;
            await LoadNewAsync(ct);
        }
        finally { _lock.Release(); }
    }

    private async Task LoadNewAsync(CancellationToken ct)
    {
        if (File.Exists(_newCacheFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(_newCacheFile) < AppConstants.SafetyNetTtl)
        {
            _newCached = ParseFixGamesJson(File.ReadAllText(_newCacheFile));
            _newLastLoaded = DateTime.UtcNow;
            _log.Log("FixData", $"Loaded {_newCached.Count} new fixes from disk cache");
            return;
        }

        _log.Log("FixData", "Downloading new_fix_games.json...");
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, AppConstants.NewFixGamesUrl);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            await File.WriteAllTextAsync(_newCacheFile, json, ct);
            _newCached = ParseFixGamesJson(json);
            _newLastLoaded = DateTime.UtcNow;
            _log.Log("FixData", $"Downloaded {_newCached.Count} new fixes");
        }
        catch (Exception ex)
        {
            _log.Log("FixData", $"Download new_fix_games failed: {ex.Message}. Using disk cache.");
            _newCached = File.Exists(_newCacheFile) ? ParseFixGamesJson(File.ReadAllText(_newCacheFile)) : Array.Empty<FixEntry>();
            _newLastLoaded = DateTime.UtcNow;
        }
    }

    // ─── steam_games.json ────────────────────────────────────────

    private async Task EnsureSteamLoadedAsync(CancellationToken ct)
    {
        if (_steamCached is not null && DateTime.UtcNow - _steamLastLoaded < AppConstants.SafetyNetTtl) return;
        await _lock.WaitAsync(ct);
        try
        {
            if (_steamCached is not null && DateTime.UtcNow - _steamLastLoaded < AppConstants.SafetyNetTtl) return;
            await LoadSteamGamesAsync(ct);
        }
        finally { _lock.Release(); }
    }

    private async Task LoadSteamGamesAsync(CancellationToken ct)
    {
        if (File.Exists(_steamGamesFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(_steamGamesFile) < AppConstants.SafetyNetTtl)
        {
            _steamCached = ParseSteamGamesJson(File.ReadAllText(_steamGamesFile));
            _steamLastLoaded = DateTime.UtcNow;
            _log.Log("FixData", $"Loaded {_steamCached.Count} steam games from disk cache");
            return;
        }

        _log.Log("FixData", "Downloading steam_games.json...");
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, AppConstants.SteamGamesUrl);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            await File.WriteAllTextAsync(_steamGamesFile, json, ct);
            _steamCached = ParseSteamGamesJson(json);
            _steamLastLoaded = DateTime.UtcNow;
            _log.Log("FixData", $"Downloaded {_steamCached.Count} steam games");
        }
        catch (Exception ex)
        {
            _log.Log("FixData", $"Download steam_games failed: {ex.Message}. Using disk cache.");
            _steamCached = File.Exists(_steamGamesFile) ? ParseSteamGamesJson(File.ReadAllText(_steamGamesFile)) : Array.Empty<FixEntry>();
            _steamLastLoaded = DateTime.UtcNow;
        }
    }

    // ─── Parsers ────────────────────────────────────────────────────

    private static IReadOnlyList<FixEntry> ParseFixGamesJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("games", out var games)) return Array.Empty<FixEntry>();

            var list = new List<FixEntry>();
            foreach (var g in games.EnumerateArray())
            {
                try
                {
                    var appId    = g.TryGetProperty("appid",      out var aid) ? aid.GetInt32()  : 0;
                    var title    = g.TryGetProperty("title",      out var t)   ? t.GetString()   ?? "" : "";
                    var pub      = g.TryGetProperty("publisher",  out var p)   ? p.GetString()   ?? "" : "";
                    var catStr   = g.TryGetProperty("category",   out var c)   ? c.GetString()   ?? "" : "";
                    var poster   = g.TryGetProperty("poster",     out var po)  ? po.GetString()  : null;
                    var password = g.TryGetProperty("password",   out var pw)  ? pw.GetString()  : null;
                    var premium  = g.TryGetProperty("premium",    out var pr)  && pr.GetBoolean();
                    var offline  = g.TryGetProperty("aktivasi_offline", out var aof) && aof.GetBoolean();
                    var exeHint  = g.TryGetProperty("exe_hint",   out var ex)  ? ex.GetString()  : null;
                    var shortcut = g.TryGetProperty("use_shortcut", out var sc) && sc.GetBoolean();

                    var files = new List<FixFile>();
                    if (g.TryGetProperty("files", out var fa))
                    {
                        foreach (var f in fa.EnumerateArray())
                        {
                            files.Add(new FixFile
                            {
                                Part      = f.TryGetProperty("part",       out var pt) ? pt.GetInt32()  : 1,
                                Filename  = f.TryGetProperty("filename",   out var fn) ? fn.GetString() ?? "" : "",
                                GDriveId  = f.TryGetProperty("gdrive_id",  out var gi) ? gi.GetString() ?? "" : "",
                                GDriveUrl = f.TryGetProperty("gdrive_url", out var gu) ? gu.GetString() ?? "" : "",
                            });
                        }
                    }

                    var category = catStr.ToLowerInvariant() switch
                    {
                        "ubisoft"       => GameCategory.Ubisoft,
                        "ea"            => GameCategory.EA,
                        "rockstar"      => GameCategory.Rockstar,
                        "playstation"   => GameCategory.PlayStation,
                        "activision"    => GameCategory.Activision,
                        "steam-account" => GameCategory.SteamAccount,
                        "steam-sharing" => GameCategory.SteamSharing,
                        _               => GameCategory.Other
                    };

                    list.Add(new FixEntry
                    {
                        AppId = appId, Title = title, Publisher = pub, Category = category,
                        PosterUrl = poster, Password = password, IsPremium = premium,
                        AktivasiOffline = offline, ExeHint = exeHint, UseShortcut = shortcut, Files = files
                    });
                }
                catch { /* skip malformed entries */ }
            }
            return list;
        }
        catch { return Array.Empty<FixEntry>(); }
    }

    /// <summary>Parses steam_games.json — flat array format (no "games" wrapper)</summary>
    private static IReadOnlyList<FixEntry> ParseSteamGamesJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Format bisa array langsung atau object dengan wrapper
            JsonElement gamesArray;
            if (root.ValueKind == JsonValueKind.Array)
                gamesArray = root;
            else if (root.TryGetProperty("games", out var g))
                gamesArray = g;
            else
                return Array.Empty<FixEntry>();

            var list = new List<FixEntry>();
            foreach (var g in gamesArray.EnumerateArray())
            {
                try
                {
                    // Wajib: appid, title, poster, username, password
                    if (!g.TryGetProperty("appid",    out var aidProp))  continue;
                    if (!g.TryGetProperty("title",    out var titleProp)) continue;
                    if (!g.TryGetProperty("poster",   out var postProp))  continue;
                    if (!g.TryGetProperty("password", out var pwProp))    continue;

                    var appId    = aidProp.ValueKind == JsonValueKind.Number ? aidProp.GetInt32()
                                  : int.TryParse(aidProp.GetString(), out var p) ? p : 0;
                    var title    = titleProp.GetString() ?? "";
                    var poster   = postProp.GetString();
                    var password = pwProp.GetString();
                    var pub      = g.TryGetProperty("publisher", out var pubProp) ? pubProp.GetString() ?? "" : "";
                    var premium  = g.TryGetProperty("premium",   out var prProp)  && prProp.GetBoolean();
                    var offline  = g.TryGetProperty("aktivasi_offline", out var aof) && aof.GetBoolean();

                    var catStr   = g.TryGetProperty("category", out var catProp) ? catProp.GetString() ?? "" : "";
                    var category = catStr.ToLowerInvariant() switch
                    {
                        "steam-sharing" => GameCategory.SteamSharing,
                        _               => GameCategory.SteamAccount
                    };

                    list.Add(new FixEntry
                    {
                        AppId = appId, Title = title, Publisher = pub, Category = category,
                        PosterUrl = poster, Password = password, IsPremium = premium,
                        AktivasiOffline = offline, Files = Array.Empty<FixFile>()
                    });
                }
                catch { /* skip malformed entries */ }
            }

            // Sort alphabetical seperti GameHub
            return list
                .OrderBy(e => System.Text.RegularExpressions.Regex.Replace(e.Title, @"[®™:]", "").Trim(),
                         StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch { return Array.Empty<FixEntry>(); }
    }
}
