using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Infrastructure.Persistence;
using System.IO.Compression;
using System.Net.Http;
using SharpCompress.Archives;

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
        var url = $"{AppConstants.OnlineFixBaseUrl}{appId}.zip";
        _log.Log("OnlineFix", $"Checking availability for appId={appId}");
        for (int i = 0; i < 3; i++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
                using var resp = await _http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode)
                {
                    _log.Log("OnlineFix", $"Fix available for appId={appId}");
                    return true;
                }
            }
            catch { }
            await Task.Delay(300, ct);
        }
        _log.Log("OnlineFix", $"Fix not available for appId={appId}");
        return false;
    }

    public async Task ApplyAsync(int appId, IProgress<FixProgressState> progress, CancellationToken ct = default)
    {
        _log.Log("OnlineFix", $"Starting apply for appId={appId}");
        var url = $"{AppConstants.OnlineFixBaseUrl}{appId}.zip";

        var installPath = _steam.ResolveGameInstallPath(appId);
        if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
        {
            progress.Report(new FixProgressState { AppId = appId, Status = FixStatus.Failed, Error = "Game not installed or path not found" });
            return;
        }

        progress.Report(new FixProgressState { AppId = appId, Status = FixStatus.Downloading, Phase = "download", Percent = 0 });
        var tempDir = Path.Combine(Path.GetTempPath(), "nexaplay-onlinefix");
        Directory.CreateDirectory(tempDir);
        var zipPath = Path.Combine(tempDir, $"fix_{appId}.zip");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode)
            {
                progress.Report(new FixProgressState { AppId = appId, Status = FixStatus.Failed, Error = "Fix file not available" });
                return;
            }

            var total = resp.Content.Headers.ContentLength ?? 0;
            long read = 0;
            var buffer = new byte[81920];
            var lastSentPct = -1;

            await using var input  = await resp.Content.ReadAsStreamAsync(ct);
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
                    progress.Report(new FixProgressState { AppId = appId, Status = FixStatus.Downloading, Phase = "download", Percent = pct, BytesRead = read, TotalBytes = total });
                }
            }

            // Extract
            progress.Report(new FixProgressState { AppId = appId, Status = FixStatus.Extracting, Phase = "extract", Percent = 0 });
            _log.Log("OnlineFix", "Extracting...");
            var extracted = await ExtractAsync(zipPath, installPath, appId, ct);

            // Write fix log
            var logPath = Path.Combine(installPath, $"{AppConstants.FixLogPrefix}{appId}.log");
            await WriteFixLogAsync(logPath, appId, extracted, ct);

            // Update state
            await _appliedStore.SetAppliedAsync(appId, true);
            progress.Report(new FixProgressState { AppId = appId, Status = FixStatus.Applied, Phase = "done", Percent = 100, Message = "Fix applied successfully" });
            _log.Log("OnlineFix", $"Apply done for appId={appId}, {extracted.Count} files extracted");
        }
        catch (OperationCanceledException)
        {
            progress.Report(new FixProgressState { AppId = appId, Status = FixStatus.Cancelled });
            _log.Log("OnlineFix", $"Apply cancelled for appId={appId}");
        }
        catch (Exception ex)
        {
            progress.Report(new FixProgressState { AppId = appId, Status = FixStatus.Failed, Error = ex.Message });
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
        foreach (var line in lines.Where(l => !l.StartsWith('[') && !string.IsNullOrWhiteSpace(l) && l != "---"))
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
            using var archive = ArchiveFactory.Open(stream);
            var prefix = appId + "/";
            var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
            var allUnder = entries.All(e => (e.Key ?? "").Replace('\\', '/').StartsWith(prefix));
            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                var key = (entry.Key ?? "").Replace('\\', '/');
                var rel = allUnder && key.StartsWith(prefix) ? key[prefix.Length..] : key;
                var target = Path.Combine(installPath, rel.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                await using var es = entry.OpenEntryStream();
                await using var fs = File.Create(target);
                await es.CopyToAsync(fs, ct);
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
}
