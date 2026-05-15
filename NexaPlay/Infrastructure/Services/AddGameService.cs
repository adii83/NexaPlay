using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using Microsoft.Win32;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace NexaPlay.Infrastructure.Services;

/// <summary>Downloads and installs Steam Lua bypass scripts to stplug-in folder.
/// Migrated from GameHub AddGameService.</summary>
public sealed class AddGameService : IAddGameService
{
    private readonly IAppLogService _log;
    private readonly ISteamService _steam;
    private readonly HttpClient _http;
    private readonly string _apiJsonPath;

    public AddGameService(IAppLogService log, ISteamService steam)
    {
        _log = log;
        _steam = steam;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        // api.json resides in app data
        _apiJsonPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder, "api.json");
    }

    public bool IsGameInstalled(string appId)
    {
        try
        {
            var steamPath = _steam.GetSteamBasePath();
            if (string.IsNullOrEmpty(steamPath)) return false;
            var dir = Path.Combine(steamPath, "config", "stplug-in");
            return File.Exists(Path.Combine(dir, appId + ".lua"))
                || File.Exists(Path.Combine(dir, appId + ".lua.disabled"));
        }
        catch { return false; }
    }

    public IReadOnlyList<string> ListLibraryGames()
    {
        try
        {
            var steamPath = _steam.GetSteamBasePath();
            if (string.IsNullOrEmpty(steamPath)) return Array.Empty<string>();
            var dir = Path.Combine(steamPath, "config", "stplug-in");
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            return Directory.GetFiles(dir, "*.lua", SearchOption.TopDirectoryOnly)
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
        }
        catch { return Array.Empty<string>(); }
    }

    public async Task AddGameAsync(string appId, IProgress<FixProgressState> progress, CancellationToken ct = default)
    {
        _log.Log("AddGame", $"Starting add for appId={appId}");
        progress.Report(new FixProgressState { Status = FixStatus.Downloading, Phase = "start", Percent = 0 });

        // Primary URL pattern
        var url = $"https://api.luatools.work/v1/{appId}";
        string? downloadedPath = null;
        var tempDir = Path.Combine(Path.GetTempPath(), "nexaplay-addgame");
        Directory.CreateDirectory(tempDir);
        var outPath = Path.Combine(tempDir, appId + ".zip");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("luatools-v61-stplugin-hoe");
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!resp.IsSuccessStatusCode)
            {
                progress.Report(new FixProgressState { Status = FixStatus.Failed, Error = "Game script not available from server" });
                return;
            }

            var total  = resp.Content.Headers.ContentLength ?? 0;
            long read  = 0;
            var buffer = new byte[81920];
            int lastPct = -1;

            await using var input  = await resp.Content.ReadAsStreamAsync(ct);
            await using var output = File.Create(outPath);
            int n;
            while ((n = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, n), ct);
                read += n;
                var pct = total > 0 ? (int)(read * 100.0 / total) : -1;
                if (pct >= lastPct + 5) { lastPct = pct; progress.Report(new FixProgressState { Status = FixStatus.Downloading, Phase = "download", Percent = pct, BytesRead = read, TotalBytes = total }); }
            }
            downloadedPath = outPath;
        }
        catch (Exception ex)
        {
            progress.Report(new FixProgressState { Status = FixStatus.Failed, Error = ex.Message });
            return;
        }

        // Validate ZIP magic bytes
        progress.Report(new FixProgressState { Status = FixStatus.Applying, Phase = "validate", Percent = 100 });
        using (var fh = File.OpenRead(downloadedPath))
        {
            var magic = new byte[4];
            await fh.ReadAsync(magic.AsMemory(0, 4), ct);
            if (magic[0] != 'P' || magic[1] != 'K')
            {
                progress.Report(new FixProgressState { Status = FixStatus.Failed, Error = "Downloaded file is not a valid ZIP" });
                return;
            }
        }

        // Install
        var steamPath = _steam.GetSteamBasePath();
        if (string.IsNullOrEmpty(steamPath))
        {
            progress.Report(new FixProgressState { Status = FixStatus.Failed, Error = "Steam not found" });
            return;
        }

        try
        {
            var installed = InstallLuaFromZip(appId, downloadedPath, steamPath);
            progress.Report(new FixProgressState { Status = FixStatus.Applied, Phase = "done", Percent = 100, Message = $"Installed: {Path.GetFileName(installed)}" });
            _log.Log("AddGame", $"Installed {Path.GetFileName(installed)} for appId={appId}");
        }
        catch (Exception ex)
        {
            progress.Report(new FixProgressState { Status = FixStatus.Failed, Error = ex.Message });
            _log.Log("AddGame", $"Install error: {ex.Message}");
        }
        finally
        {
            try { if (File.Exists(downloadedPath)) File.Delete(downloadedPath); } catch { }
        }
    }

    public async Task RemoveGameAsync(string appId)
    {
        _log.Log("AddGame", $"Removing appId={appId}");
        var steamPath = _steam.GetSteamBasePath();
        if (string.IsNullOrEmpty(steamPath)) return;
        var dir = Path.Combine(steamPath, "config", "stplug-in");
        foreach (var fn in new[] { appId + ".lua", appId + ".lua.disabled" })
        {
            var p = Path.Combine(dir, fn);
            try { if (File.Exists(p)) File.Delete(p); } catch { }
        }
        await Task.CompletedTask;
    }

    private static string InstallLuaFromZip(string appId, string zipPath, string steamPath)
    {
        var targetDir    = Path.Combine(steamPath, "config", "stplug-in");
        var depotDir     = Path.Combine(steamPath, "depotcache");
        Directory.CreateDirectory(targetDir);
        Directory.CreateDirectory(depotDir);

        using var zf = ZipFile.OpenRead(zipPath);
        // Extract .manifest files
        foreach (var entry in zf.Entries.Where(e => e.FullName.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)))
            entry.ExtractToFile(Path.Combine(depotDir, Path.GetFileName(entry.FullName)), overwrite: true);

        // Find the numeric .lua file
        var preferred = appId + ".lua";
        var chosen = zf.Entries.FirstOrDefault(e => Path.GetFileName(e.FullName) == preferred)
                  ?? zf.Entries.FirstOrDefault(e => Regex.IsMatch(Path.GetFileName(e.FullName), @"^\d+\.lua$"));

        if (chosen is null) throw new InvalidOperationException("No numeric .lua file found in ZIP");

        using var ms = new MemoryStream();
        using (var s = chosen.Open()) s.CopyTo(ms);
        ms.Position = 0;
        string text;
        using (var sr = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            text = sr.ReadToEnd();

        // Comment out setManifestid calls (GameHub pattern)
        var sb = new StringBuilder(text.Length + 64);
        using (var reader = new StringReader(text))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (Regex.IsMatch(line, @"^\s*setManifestid\(") && !Regex.IsMatch(line, @"^\s*--"))
                    sb.AppendLine("--" + line);
                else
                    sb.AppendLine(line);
            }
        }

        var processed = sb.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
        var destFile  = Path.Combine(targetDir, appId + ".lua");
        File.WriteAllText(destFile, processed, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return destFile;
    }
}
