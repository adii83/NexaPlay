using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Add/Remove game script service ported from GameHub AddGameService with parity-focused flow.
/// UI/ViewModel only orchestrates state; IO and API handling stay in this service.
/// </summary>
public sealed class AddGameService : IAddGameService
{
    private readonly IAppLogService _log;
    private readonly ISteamService _steam;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _running = new(StringComparer.Ordinal);

    public AddGameService(IAppLogService log, ISteamService steam)
    {
        _log = log;
        _steam = steam;
    }

    public bool IsGameInstalled(string appId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(appId)) return false;
            var steamPath = _steam.GetSteamBasePath();
            if (string.IsNullOrWhiteSpace(steamPath)) return false;

            var dir = Path.Combine(steamPath, "config", "stplug-in");
            return File.Exists(Path.Combine(dir, appId + ".lua"))
                || File.Exists(Path.Combine(dir, appId + ".lua.disabled"));
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<string> ListLibraryGames()
    {
        try
        {
            var steamPath = _steam.GetSteamBasePath();
            if (string.IsNullOrWhiteSpace(steamPath)) return Array.Empty<string>();

            var dir = Path.Combine(steamPath, "config", "stplug-in");
            if (!Directory.Exists(dir)) return Array.Empty<string>();

            var list = new List<string>();
            var files = new DirectoryInfo(dir)
                .GetFiles("*.lua", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ThenByDescending(f => f.CreationTimeUtc)
                .Select(f => f.FullName);

            foreach (var file in files)
            {
                var appId = Path.GetFileNameWithoutExtension(file);
                if (!string.IsNullOrWhiteSpace(appId))
                    list.Add(appId);
            }

            LogInfo($"ListLibraryGames menemukan {list.Count} skrip LUA");
            return list;
        }
        catch (Exception ex)
        {
            LogInfo($"ListLibraryGames gagal: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    public void CancelAdd(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId)) return;

        if (_running.TryRemove(appId, out var cts))
        {
            try { cts.Cancel(); } catch { }
            try { cts.Dispose(); } catch { }
            LogInfo($"Pembatalan Add-Game diminta appid={appId}");
        }
    }

    public async Task AddGameAsync(string appId, IProgress<BypassProgressState> progress, CancellationToken ct = default)
    {
        string? downloadedZipPath = null;
        string? downloadedProviderDir = null;
        string? partialPath = null;

        if (string.IsNullOrWhiteSpace(appId))
        {
            progress.Report(FailedState("start", "AppID kosong"));
            return;
        }

        if (_running.ContainsKey(appId))
        {
            progress.Report(FailedState("start", "Proses sedang berjalan"));
            return;
        }

        var localCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (!_running.TryAdd(appId, localCts))
        {
            progress.Report(FailedState("start", "Tidak dapat memulai proses"));
            localCts.Dispose();
            return;
        }

        try
        {
            LogInfo($"Mulai Add-Game appid={appId}");
            progress.Report(new BypassProgressState
            {
                AppId = ParseAppId(appId),
                Status = BypassStatus.Pending,
                Phase = "start",
                Percent = 0,
                Message = "Starting..."
            });

            var apiJsonPath = ResolveApiJsonPath();
            if (string.IsNullOrWhiteSpace(apiJsonPath) || !File.Exists(apiJsonPath))
            {
                LogInfo("api.json tidak ditemukan");
                progress.Report(FailedState("start", "api.json tidak ditemukan"));
                return;
            }

            JsonElement apiList;
            await using (var fs = File.OpenRead(apiJsonPath))
            using (var doc = await JsonDocument.ParseAsync(fs, cancellationToken: localCts.Token))
            {
                apiList = doc.RootElement.TryGetProperty("api_list", out var list) ? list.Clone() : default;
            }

            if (apiList.ValueKind != JsonValueKind.Array)
            {
                LogInfo("Format api.json tidak valid");
                progress.Report(FailedState("start", "Format api.json tidak valid"));
                return;
            }

            var downloadsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GameHub",
                "downloads");
            Directory.CreateDirectory(downloadsRoot);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var sourceIndex = 0;
            foreach (var entry in apiList.EnumerateArray())
            {
                localCts.Token.ThrowIfCancellationRequested();
                sourceIndex++;

                var enabled = entry.TryGetProperty("enabled", out var en) && en.ValueKind == JsonValueKind.True;
                if (!enabled) continue;

                var name = entry.TryGetProperty("name", out var nm) ? nm.GetString() ?? "unknown" : "unknown";
                var urlTemplate = entry.TryGetProperty("url", out var ut) ? ut.GetString() ?? string.Empty : string.Empty;
                var successCode = entry.TryGetProperty("success_code", out var sc) && sc.TryGetInt32(out var sci) ? sci : 200;
                var unavailableCode = entry.TryGetProperty("unavailable_code", out var uc) && uc.TryGetInt32(out var uci) ? uci : 404;
                var sourceLabel = $"sumber #{sourceIndex}";
                var url = urlTemplate.Replace("<appid>", appId, StringComparison.Ordinal);

                LogInfo($"Coba unduh dari {sourceLabel} ({name}) url={RedactUrl(url)}");
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.UserAgent.ParseAdd("luatools-v61-stplugin-hoe");
                    using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, localCts.Token);
                    LogInfo($"Respon {sourceLabel} status={(int)resp.StatusCode}");

                    if ((int)resp.StatusCode != successCode)
                    {
                        if ((int)resp.StatusCode == unavailableCode)
                            LogInfo($"{sourceLabel} melaporkan tidak tersedia");
                        continue;
                    }

                    var targetDir = Path.Combine(downloadsRoot, SafeName(name));
                    Directory.CreateDirectory(targetDir);

                    var ext = GuessExtension(resp, url);
                    var outPath = Path.Combine(targetDir, appId + ext);
                    partialPath = outPath;

                    await using var body = await resp.Content.ReadAsStreamAsync(localCts.Token);
                    await using (var outFs = File.Create(outPath))
                    {
                        var total = resp.Content.Headers.ContentLength ?? 0L;
                        var read = 0L;
                        var buffer = new byte[81920];
                        var lastPct = -1;

                        int n;
                        while ((n = await body.ReadAsync(buffer.AsMemory(0, buffer.Length), localCts.Token)) > 0)
                        {
                            await outFs.WriteAsync(buffer.AsMemory(0, n), localCts.Token);
                            read += n;

                            if (total > 0)
                            {
                                var pct = (int)Math.Clamp(read * 100.0 / total, 0, 100);
                                if (pct >= lastPct + 5)
                                {
                                    lastPct = pct;
                                    progress.Report(new BypassProgressState
                                    {
                                        AppId = ParseAppId(appId),
                                        Status = BypassStatus.Downloading,
                                        Phase = "download",
                                        Percent = pct,
                                        BytesRead = read,
                                        TotalBytes = total,
                                        Message = $"Downloading from {name}..."
                                    });
                                }
                            }
                            else
                            {
                                progress.Report(new BypassProgressState
                                {
                                    AppId = ParseAppId(appId),
                                    Status = BypassStatus.Downloading,
                                    Phase = "download",
                                    Percent = -1,
                                    BytesRead = read,
                                    TotalBytes = 0,
                                    Message = $"Downloading from {name}..."
                                });
                            }
                        }
                    }

                    downloadedZipPath = outPath;
                    downloadedProviderDir = targetDir;
                    LogInfo($"Unduhan selesai dari {sourceLabel} file={RedactPath(downloadedZipPath)}");
                    break;
                }
                catch (Exception ex)
                {
                    LogInfo($"Gagal unduh dari {sourceLabel}: {ex.Message}");
                }
            }

            if (string.IsNullOrWhiteSpace(downloadedZipPath) || !File.Exists(downloadedZipPath))
            {
                LogInfo("Semua API gagal atau tidak tersedia");
                progress.Report(FailedState("download", "Semua API gagal atau tidak tersedia"));
                return;
            }

            progress.Report(new BypassProgressState
            {
                AppId = ParseAppId(appId),
                Status = BypassStatus.Applying,
                Phase = "validate",
                Percent = 100,
                Message = "Validating..."
            });

            await using (var fh = File.OpenRead(downloadedZipPath))
            {
                var magic = new byte[4];
                await fh.ReadAsync(magic.AsMemory(0, 4), localCts.Token);
                if (!(magic[0] == (byte)'P' && magic[1] == (byte)'K'))
                {
                    LogInfo("File unduhan bukan ZIP valid");
                    progress.Report(FailedState("validate", "File unduhan bukan ZIP"));
                    return;
                }
            }

            var steamPath = _steam.GetSteamBasePath();
            if (string.IsNullOrWhiteSpace(steamPath))
            {
                LogInfo("Steam tidak ditemukan saat instalasi");
                progress.Report(FailedState("install", "Steam tidak ditemukan"));
                return;
            }

            progress.Report(new BypassProgressState
            {
                AppId = ParseAppId(appId),
                Status = BypassStatus.Applying,
                Phase = "install",
                Percent = 0,
                Message = "Installing..."
            });

            var installedPath = InstallLuaFromZip(appId, downloadedZipPath, steamPath);
            LogInfo($"Instalasi selesai file={RedactPath(installedPath)}");

            progress.Report(new BypassProgressState
            {
                AppId = ParseAppId(appId),
                Status = BypassStatus.Applied,
                Phase = "done",
                Percent = 100,
                Message = $"Installed: {Path.GetFileName(installedPath)}"
            });
        }
        catch (OperationCanceledException)
        {
            LogInfo("Add-Game dibatalkan");
            progress.Report(new BypassProgressState
            {
                AppId = ParseAppId(appId),
                Status = BypassStatus.Cancelled,
                Phase = "cancel",
                Percent = 0,
                Error = "Dibatalkan",
                Message = "Cancelled."
            });
            TryCleanupPartial(partialPath, downloadedProviderDir);
        }
        catch (Exception ex)
        {
            LogInfo($"Add-Game gagal: {ex.Message}");
            progress.Report(FailedState("error", ex.Message));
        }
        finally
        {
            TryCleanupDownloaded(downloadedZipPath, downloadedProviderDir);

            if (_running.TryRemove(appId, out var removed))
            {
                try { removed.Dispose(); } catch { }
            }
            else
            {
                try { localCts.Dispose(); } catch { }
            }
        }
    }

    public async Task<RemoveGameResult> RemoveGameAsync(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
            return new RemoveGameResult { Success = false, Error = "AppID kosong." };

        LogInfo($"Mulai Remove-Game appid={appId}");
        var steamPath = _steam.GetSteamBasePath();
        if (string.IsNullOrWhiteSpace(steamPath))
            return new RemoveGameResult { Success = false, Error = "Steam tidak ditemukan." };

        if (IsAppManifestPresent(steamPath, appId))
        {
            LogInfo($"Remove-Game diblokir karena appmanifest ada appid={appId}");
            return new RemoveGameResult
            {
                Success = false,
                BlockedByInstalledGame = true,
                Error = "Game masih terinstall di Steam. Uninstall dulu dari Steam sebelum Remove Game."
            };
        }

        var dir = Path.Combine(steamPath, "config", "stplug-in");
        foreach (var fn in new[] { appId + ".lua", appId + ".lua.disabled" })
        {
            try
            {
                var path = Path.Combine(dir, fn);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }

        await Task.CompletedTask;
        return new RemoveGameResult { Success = true };
    }

    private static bool IsAppManifestPresent(string steamBasePath, string appId)
    {
        try
        {
            var libraries = new List<string> { steamBasePath };
            var vdf = Path.Combine(steamBasePath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                foreach (var raw in File.ReadAllLines(vdf))
                {
                    var line = raw.Trim();
                    if (!line.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    var path = parts[^1].Replace("\\\\", "\\", StringComparison.Ordinal).Trim();
                    if (Directory.Exists(path))
                        libraries.Add(path);
                }
            }

            foreach (var lib in libraries.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var manifest = Path.Combine(lib, "steamapps", $"appmanifest_{appId}.acf");
                if (File.Exists(manifest))
                    return true;
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private string? ResolveApiJsonPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.AppDataFolder, "api.json"),
            Path.Combine(AppContext.BaseDirectory, "data", "api.json"),
            Path.Combine(AppContext.BaseDirectory, "public", "data", "api.json")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static string InstallLuaFromZip(string appId, string zipPath, string steamPath)
    {
        if (!File.Exists(zipPath)) throw new FileNotFoundException(zipPath);
        if (string.IsNullOrWhiteSpace(steamPath)) throw new InvalidOperationException("Steam path kosong");

        var targetDir = Path.Combine(steamPath, "config", "stplug-in");
        Directory.CreateDirectory(targetDir);

        var depotcacheDir = Path.Combine(steamPath, "depotcache");
        Directory.CreateDirectory(depotcacheDir);

        using var zf = ZipFile.OpenRead(zipPath);
        foreach (var entry in zf.Entries.Where(e => e.FullName.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)))
            entry.ExtractToFile(Path.Combine(depotcacheDir, Path.GetFileName(entry.FullName)), overwrite: true);

        var preferred = appId + ".lua";
        var chosen = zf.Entries.FirstOrDefault(e => Path.GetFileName(e.FullName) == preferred)
            ?? zf.Entries.FirstOrDefault(e => Regex.IsMatch(Path.GetFileName(e.FullName) ?? string.Empty, "^\\d+\\.lua$"));

        if (chosen is null)
            throw new InvalidOperationException("Tidak ada file .lua numerik dalam ZIP");

        string text;
        using (var ms = new MemoryStream())
        {
            using (var stream = chosen.Open())
                stream.CopyTo(ms);
            ms.Position = 0;
            using var reader = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            text = reader.ReadToEnd();
        }

        var sb = new StringBuilder(text.Length + 64);
        using (var reader = new StringReader(text))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (Regex.IsMatch(line, "^\\s*setManifestid\\(") && !Regex.IsMatch(line, "^\\s*--"))
                    sb.Append("--").AppendLine(line);
                else
                    sb.AppendLine(line);
            }
        }

        var processed = sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);
        var destFile = Path.Combine(targetDir, appId + ".lua");
        File.WriteAllText(destFile, processed, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return destFile;
    }

    private static string GuessExtension(HttpResponseMessage response, string url)
    {
        var fileNameStar = response.Content.Headers.ContentDisposition?.FileNameStar;
        if (!string.IsNullOrWhiteSpace(fileNameStar))
            return Path.GetExtension(fileNameStar);

        var fileName = response.Content.Headers.ContentDisposition?.FileName;
        if (!string.IsNullOrWhiteSpace(fileName))
            return Path.GetExtension(fileName);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (string.Equals(contentType, "application/zip", StringComparison.OrdinalIgnoreCase))
            return ".zip";

        var ext = Path.GetExtension(url);
        if (!string.IsNullOrWhiteSpace(ext))
            return ext;

        return ".bin";
    }

    private static string SafeName(string name)
    {
        var chars = new List<char>(name.Length);
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_' or '.')
                chars.Add(c);
        }

        return new string(chars.ToArray()).TrimEnd();
    }

    private static int ParseAppId(string appId)
        => int.TryParse(appId, out var value) ? value : 0;

    private static void TryCleanupDownloaded(string? zipPath, string? providerDir)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(zipPath) && File.Exists(zipPath))
                File.Delete(zipPath);
        }
        catch { }

        try
        {
            if (!string.IsNullOrWhiteSpace(providerDir) && Directory.Exists(providerDir))
            {
                if (Directory.GetFiles(providerDir).Length == 0 && Directory.GetDirectories(providerDir).Length == 0)
                    Directory.Delete(providerDir, false);
            }
        }
        catch { }
    }

    private static void TryCleanupPartial(string? partialPath, string? providerDir)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(partialPath) && File.Exists(partialPath))
                File.Delete(partialPath);
        }
        catch { }

        TryCleanupDownloaded(null, providerDir);
    }

    private static string RedactUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}/(disamarkan)";
        }
        catch
        {
            return "(url disamarkan)";
        }
    }

    private static string RedactPath(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return "(path disamarkan)";
            var fileName = Path.GetFileName(path);
            var root = Path.GetPathRoot(path);
            var prefix = string.IsNullOrWhiteSpace(root) ? string.Empty : $"{root}...\\";
            return string.IsNullOrWhiteSpace(fileName) ? "(path disamarkan)" : $"{prefix}{fileName}";
        }
        catch
        {
            return "(path disamarkan)";
        }
    }

    private void LogInfo(string message)
    {
        try { _log.Log("AddGame", message); } catch { }
    }

    private static BypassProgressState FailedState(string phase, string error) => new()
    {
        Status = BypassStatus.Failed,
        Phase = phase,
        Error = error,
        Message = error
    };
}
