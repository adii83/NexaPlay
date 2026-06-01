using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Infrastructure.Persistence;
using System.IO.Compression;
using System.Net.Http;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace NexaPlay.Infrastructure.Services;

/// <summary>Applies and reverts online fixes from files.luatools.work.
/// Migrated from GameHub OnlineFixService with WinUI 3 IProgress pattern.</summary>
public sealed class OnlineFixService : IOnlineFixService
{
    private readonly IAppLogService _log;
    private readonly ISteamService _steam;
    private readonly AppliedStateStore _appliedStore;
    private readonly HttpClient _http;

    public OnlineFixService(IAppLogService log, ISteamService steam, AppliedStateStore appliedStore)
    {
        _log = log;
        _steam = steam;
        _appliedStore = appliedStore;
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true })
        {
            Timeout = AppConstants.HttpDefaultTimeout
        };
    }

    public bool IsApplied(int appId) => _appliedStore.IsApplied(appId);

    public async Task<bool> CheckAvailabilityAsync(int appId, CancellationToken ct = default)
    {
        var primaryUrl = $"{AppConstants.OnlineFixBaseUrl}{appId}.zip";
        var fallbackUrl = AppConstants.OnlineFixFallbackUrl;
        _log.Log("OnlineFix", $"Checking availability for appId={appId}");
        for (int i = 0; i < 3; i++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Head, primaryUrl);
                req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
                using var resp = await _http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode)
                {
                    _log.Log("OnlineFix", $"Fix available for appId={appId} source=primary");
                    return true;
                }
            }
            catch { }
            await Task.Delay(300, ct);
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Head, fallbackUrl);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
            using var resp = await _http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                _log.Log("OnlineFix", $"Fix available for appId={appId} source=fallback");
                return true;
            }
        }
        catch { }

        _log.Log("OnlineFix", $"Fix not available for appId={appId} on all sources");
        return false;
    }

    public async Task ApplyAsync(int appId, IProgress<BypassProgressState> progress, CancellationToken ct = default)
    {
        _log.Log("OnlineFix", $"Starting apply for appId={appId}");
        var primaryUrl = $"{AppConstants.OnlineFixBaseUrl}{appId}.zip";
        var fallbackUrl = AppConstants.OnlineFixFallbackUrl;

        var installPath = _steam.ResolveGameInstallPath(appId);
        if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
        {
            progress.Report(new BypassProgressState { AppId = appId, Status = BypassStatus.Failed, Error = "Game belum terinstall atau folder game tidak ditemukan." });
            return;
        }

        progress.Report(new BypassProgressState { AppId = appId, Status = BypassStatus.Downloading, Phase = "download", Percent = 0 });
        var tempDir = Path.Combine(Path.GetTempPath(), "nexaplay-onlinefix");
        Directory.CreateDirectory(tempDir);
        var zipPath = Path.Combine(tempDir, $"fix_{appId}.zip");

        try
        {
            HttpResponseMessage? resp = null;
            var sourceLabel = "primary";
            try
            {
                var reqPrimary = new HttpRequestMessage(HttpMethod.Get, primaryUrl);
                reqPrimary.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
                resp = await _http.SendAsync(reqPrimary, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    resp.Dispose();
                    resp = null;
                }
            }
            catch
            {
                resp?.Dispose();
                resp = null;
            }

            if (resp is null)
            {
                sourceLabel = "fallback";
                var reqFallback = new HttpRequestMessage(HttpMethod.Get, fallbackUrl);
                reqFallback.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
                resp = await _http.SendAsync(reqFallback, HttpCompletionOption.ResponseHeadersRead, ct);
            }

            using (resp)
            {
                if (resp is null || !resp.IsSuccessStatusCode)
                {
                    progress.Report(new BypassProgressState { AppId = appId, Status = BypassStatus.Failed, Error = "File Online-Fix belum tersedia." });
                    return;
                }

                _log.Log("OnlineFix", $"Download source selected appId={appId} source={sourceLabel}");

                var total = resp.Content.Headers.ContentLength ?? 0;
                long read = 0;
                var buffer = new byte[81920];
                var lastSentPct = -1;

                await using var input = await resp.Content.ReadAsStreamAsync(ct);
                await using var output = File.Create(zipPath);
                int n;
                while ((n = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, n), ct);
                    read += n;
                    var pct = total > 0 ? (int)(read * 100.0 / total) : -1;
                    if (pct >= lastSentPct + 1)
                    {
                        lastSentPct = pct;
                        progress.Report(new BypassProgressState { AppId = appId, Status = BypassStatus.Downloading, Phase = "download", Percent = pct, BytesRead = read, TotalBytes = total });
                    }
                }
            }

            // Extract
            progress.Report(new BypassProgressState { AppId = appId, Status = BypassStatus.Extracting, Phase = "extract", Percent = 0 });
            _log.Log("OnlineFix", "Extracting...");
            var extracted = await ExtractAsync(zipPath, installPath, appId, ct);
            await PatchUnsteamIniAsync(installPath, appId, extracted, ct);

            // Write fix log
            var logPath = Path.Combine(installPath, $"{AppConstants.FixLogPrefix}{appId}.log");
            await WriteFixLogAsync(logPath, appId, extracted, ct);

            // Update state
            await _appliedStore.SetAppliedAsync(appId, true);
            progress.Report(new BypassProgressState { AppId = appId, Status = BypassStatus.Applied, Phase = "done", Percent = 100, Message = "Online-Fix berhasil diterapkan." });
            _log.Log("OnlineFix", $"Apply done for appId={appId}, {extracted.Count} files extracted");
        }
        catch (OperationCanceledException)
        {
            progress.Report(new BypassProgressState { AppId = appId, Status = BypassStatus.Cancelled });
            _log.Log("OnlineFix", $"Apply cancelled for appId={appId}");
        }
        catch (Exception ex)
        {
            progress.Report(new BypassProgressState { AppId = appId, Status = BypassStatus.Failed, Error = ex.Message });
            _log.Log("OnlineFix", $"Apply failed for appId={appId}: {ex.Message}");
        }
        finally
        {
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
        }
    }

    public async Task UnfixAsync(int appId, CancellationToken ct = default)
    {
        _log.Log("OnlineFix", $"Unfix appId={appId}");
        var installPath = _steam.ResolveGameInstallPath(appId);
        if (string.IsNullOrEmpty(installPath)) return;

        var logPath = Path.Combine(installPath, $"{AppConstants.FixLogPrefix}{appId}.log");
        if (!File.Exists(logPath)) { _log.Log("OnlineFix", "No fix log found"); return; }

        var lines = await File.ReadAllLinesAsync(logPath, ct);
        int deleted = 0;
        foreach (var line in lines.Where(l =>
                     !l.StartsWith('[') &&
                     !string.IsNullOrWhiteSpace(l) &&
                     l != "---" &&
                     !l.StartsWith("Date:", StringComparison.OrdinalIgnoreCase) &&
                     !l.StartsWith("AppId:", StringComparison.OrdinalIgnoreCase) &&
                     !l.StartsWith("Files:", StringComparison.OrdinalIgnoreCase)))
        {
            var full = Path.Combine(installPath, line.Replace('/', Path.DirectorySeparatorChar));
            try { if (File.Exists(full)) { File.Delete(full); deleted++; } } catch { }
        }
        try { File.Delete(logPath); } catch { }
        await _appliedStore.SetAppliedAsync(appId, false);
        _log.Log("OnlineFix", $"Unfix done: {deleted} files removed");
    }

    private static async Task<List<string>> ExtractAsync(string zipPath, string installPath, int appId, CancellationToken ct)
    {
        var extracted = new List<string>();
        try
        {
            using var za = ZipFile.OpenRead(zipPath);
            var prefix = appId + "/";
            var allUnder = za.Entries.All(e => e.FullName.EndsWith("/") || e.FullName.StartsWith(prefix));
            foreach (var entry in za.Entries)
            {
                ct.ThrowIfCancellationRequested();
                if (entry.FullName.EndsWith("/")) continue;
                var rel = allUnder && entry.FullName.StartsWith(prefix)
                    ? entry.FullName[prefix.Length..] : entry.FullName;
                var target = Path.Combine(installPath, rel.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: true);
                extracted.Add(rel.Replace('\\', '/'));
            }
        }
        catch (InvalidDataException)
        {
            // Fallback to SharpCompress for LZMA zips
            using var stream = File.OpenRead(zipPath);
            var prefix = appId + "/";
            using var archive = ArchiveFactory.OpenArchive(stream, new ReaderOptions());
            var entries = archive.Entries
                .Where(entry => !entry.IsDirectory)
                .ToList();

            var allUnder = entries.All(entry => (entry.Key ?? string.Empty).Replace('\\', '/').StartsWith(prefix, StringComparison.Ordinal));
            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                var key = (entry.Key ?? string.Empty).Replace('\\', '/');
                var rel = allUnder && key.StartsWith(prefix, StringComparison.Ordinal)
                    ? key[prefix.Length..]
                    : key;

                var target = Path.Combine(installPath, rel.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.WriteToFile(target, new ExtractionOptions { Overwrite = true });
                extracted.Add(rel);
            }
        }
        return extracted;
    }

    private static async Task WriteFixLogAsync(string logPath, int appId, List<string> files, CancellationToken ct)
    {
        var lines = new List<string>
        {
            "[FIX]",
            $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"AppId: {appId}",
            "Files:"
        };
        lines.AddRange(files);
        lines.Add("[/FIX]");
        await File.WriteAllLinesAsync(logPath, lines, ct);
    }

    private async Task PatchUnsteamIniAsync(string installPath, int appId, List<string> extractedFiles, CancellationToken ct)
    {
        var relIni = extractedFiles
            .FirstOrDefault(f => f.EndsWith("unsteam.ini", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(relIni))
            return;

        var iniPath = Path.Combine(installPath, relIni.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(iniPath))
            return;

        try
        {
            var contents = await File.ReadAllTextAsync(iniPath, ct);
            var updated = contents.Replace("<appid>", appId.ToString(), StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(contents, updated, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(iniPath, updated, ct);
                _log.Log("OnlineFix", $"Updated unsteam.ini placeholder for appId={appId}");
            }
        }
        catch (Exception ex)
        {
            _log.Log("OnlineFix", $"Failed to patch unsteam.ini for appId={appId}: {ex.Message}");
        }
    }
}
