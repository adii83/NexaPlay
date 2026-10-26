using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Collections.Concurrent;
using System.IO.Compression;
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
    private readonly HttpClient? _http;
    private readonly string? _apiJsonPath;
    private readonly string? _downloadsRoot;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _running = new(StringComparer.Ordinal);

    public AddGameService(IAppLogService log, ISteamService steam)
    {
        _log = log;
        _steam = steam;
    }

    internal AddGameService(IAppLogService log, ISteamService steam, HttpClient http, string apiJsonPath, string downloadsRoot)
        : this(log, steam)
    {
        _http = http;
        _apiJsonPath = apiJsonPath;
        _downloadsRoot = downloadsRoot;
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

        if (_running.TryGetValue(appId, out var cts))
        {
            try { cts.Cancel(); } catch { }
            LogInfo($"Pembatalan Add-Game diminta appid={appId}");
        }
    }

    public async Task AddGameAsync(string appId, IProgress<BypassProgressState> progress, CancellationToken ct = default)
    {
        string? downloadedZipPath = null;
        string? downloadedProviderDir = null;

        if (string.IsNullOrWhiteSpace(appId) || !Regex.IsMatch(appId, "^[0-9]+$"))
        {
            progress.Report(FailedState("start", "AppID harus berupa angka"));
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

            var downloadsRoot = _downloadsRoot ?? Path.Combine(AppConstants.LocalAppDataPath, "GameHub", "downloads");
            Directory.CreateDirectory(downloadsRoot);

            using var ownedHttp = _http is null ? new HttpClient { Timeout = TimeSpan.FromSeconds(45) } : null;
            var http = _http ?? ownedHttp!;
            var sourceIndex = 0;
            foreach (var entry in apiList.EnumerateArray())
            {
                localCts.Token.ThrowIfCancellationRequested();
                sourceIndex++;

                var enabled = entry.TryGetProperty("enabled", out var en) && en.ValueKind == JsonValueKind.True;
                if (!enabled) continue;

                var name = entry.TryGetProperty("name", out var nm) ? nm.GetString() ?? "unknown" : "unknown";
                var urlTemplate = entry.TryGetProperty("url", out var ut) ? ut.GetString() ?? string.Empty : string.Empty;
                var successCode = entry.TryGetProperty("success_code", out var sc) ? sc : default;
                var unavailableCode = entry.TryGetProperty("unavailable_code", out var uc) && uc.TryGetInt32(out var uci) ? uci : 404;
                var sourceLabel = $"sumber #{sourceIndex}";
                var url = urlTemplate.Replace("<appid>", appId, StringComparison.Ordinal);
                var preparePayload = entry.TryGetProperty("lua_processing", out var processing);
                string? attemptDir = null;

                LogInfo($"Coba unduh dari {sourceLabel} ({name}) url={RedactUrl(url)}");
                try
                {
                    var mode = preparePayload
                        ? AddGamePayloadBuilder.ResolveMode(processing.GetString() ?? "auto", url)
                        : null;
                    using var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token);
                    downloadCts.CancelAfter(TimeSpan.FromSeconds(preparePayload ? 135 : 30));
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.UserAgent.ParseAdd(preparePayload ? "Project-Lightning" : "luatools-v61-stplugin-hoe");
                    req.Headers.TryAddWithoutValidation("Accept", "application/zip,application/octet-stream,*/*");
                    using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, downloadCts.Token);
                    LogInfo($"Respon {sourceLabel} status={(int)resp.StatusCode}");

                    if (!IsSuccessStatus((int)resp.StatusCode, successCode))
                    {
                        if ((int)resp.StatusCode == unavailableCode)
                            LogInfo($"{sourceLabel} melaporkan tidak tersedia");
                        continue;
                    }

                    if (resp.Content.Headers.ContentLength > AddGamePayloadBuilder.MaxZipBytes)
                        throw new InvalidDataException("Unduhan melebihi 128 MiB.");
                    attemptDir = Path.Combine(downloadsRoot, SafeName(name), appId + "-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(attemptDir);
                    var finalPath = Path.Combine(attemptDir, appId + ".zip");
                    var outPath = preparePayload ? Path.Combine(attemptDir, "source.zip") : finalPath;

                    await using var body = await resp.Content.ReadAsStreamAsync(downloadCts.Token);
                    await using (var outFs = File.Create(outPath))
                    {
                        var total = resp.Content.Headers.ContentLength ?? 0L;
                        var read = 0L;
                        var buffer = new byte[81920];
                        var lastPct = -1;
                        var lastReport = Environment.TickCount64 - 250;

                        int n;
                        while ((n = await body.ReadAsync(buffer.AsMemory(0, buffer.Length), downloadCts.Token)) > 0)
                        {
                            read += n;
                            if (read > AddGamePayloadBuilder.MaxZipBytes)
                                throw new InvalidDataException("Unduhan melebihi 128 MiB.");
                            await outFs.WriteAsync(buffer.AsMemory(0, n), downloadCts.Token);

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
                            else if (Environment.TickCount64 - lastReport >= 250)
                            {
                                lastReport = Environment.TickCount64;
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

                    progress.Report(new BypassProgressState
                    {
                        AppId = ParseAppId(appId), Status = BypassStatus.Applying,
                        Phase = "validate", Percent = -1,
                        Message = preparePayload ? "Preparing files..." : "Validating..."
                    });
                    await Task.Run(() =>
                    {
                        if (preparePayload)
                            AddGamePayloadBuilder.Build(outPath, finalPath, mode!, localCts.Token);
                        AddGamePayloadBuilder.ValidateInstallable(finalPath, appId, localCts.Token);
                    }, localCts.Token);
                    if (preparePayload)
                        File.Delete(outPath);
                    downloadedZipPath = finalPath;
                    downloadedProviderDir = attemptDir;
                    attemptDir = null; // The outer finally owns the accepted payload.
                    LogInfo($"Unduhan selesai dari {sourceLabel} file={RedactPath(downloadedZipPath)}");
                    break;
                }
                catch (OperationCanceledException) when (localCts.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogInfo($"Gagal unduh dari {sourceLabel}: {ex.Message}");
                }
                finally
                {
                    TryCleanupDownloaded(null, attemptDir);
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

            var installedPath = await Task.Run(() => InstallLuaFromZip(appId, downloadedZipPath, steamPath, localCts.Token), localCts.Token);
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
        if (_apiJsonPath is not null)
            return _apiJsonPath;
        var candidates = new[]
        {
            Path.Combine(AppConstants.AppDataPath, "api.json"),
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

    internal static string InstallLuaFromZip(string appId, string zipPath, string steamPath, CancellationToken ct = default)
    {
        if (!File.Exists(zipPath)) throw new FileNotFoundException(zipPath);
        if (string.IsNullOrWhiteSpace(steamPath)) throw new InvalidOperationException("Steam path kosong");

        var targetDir = Path.Combine(steamPath, "config", "stplug-in");
        Directory.CreateDirectory(targetDir);

        var depotcacheDir = Path.Combine(steamPath, "depotcache");
        Directory.CreateDirectory(depotcacheDir);

        using var zf = ZipFile.OpenRead(zipPath);
        var chosen = AddGamePayloadBuilder.SelectLua(zf, appId);
        foreach (var entry in zf.Entries.Where(e => e.FullName.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)))
        {
            WriteEntryAtomically(entry, Path.Combine(depotcacheDir, Path.GetFileName(entry.FullName.Replace('\\', '/'))), ct);
        }

        var destFile = Path.Combine(targetDir, appId + ".lua");
        WriteEntryAtomically(chosen, destFile, ct);
        return destFile;
    }

    private static void WriteEntryAtomically(ZipArchiveEntry entry, string destination, CancellationToken ct)
    {
        var staged = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            ct.ThrowIfCancellationRequested();
            using (var input = entry.Open())
            using (var output = File.Create(staged))
                AddGamePayloadBuilder.CopyBounded(input, output, entry.Length, ct);
            ct.ThrowIfCancellationRequested();
            File.Move(staged, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(staged))
                File.Delete(staged);
        }
    }

    internal static bool IsSuccessStatus(int status, JsonElement configured)
    {
        if (configured.ValueKind == JsonValueKind.String && configured.GetString() == "2xx")
            return status is >= 200 and < 300;
        return status == (configured.ValueKind == JsonValueKind.Number && configured.TryGetInt32(out var code) ? code : 200);
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
                // providerDir is a private GUID directory created for this attempt.
                Directory.Delete(providerDir, recursive: true);
            }
        }
        catch { }
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
