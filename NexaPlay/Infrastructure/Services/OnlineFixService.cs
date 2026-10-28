using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;

namespace NexaPlay.Infrastructure.Services;

/// <summary>Installs only packages explicitly mapped by online_fix.json.</summary>
public sealed class OnlineFixService : IOnlineFixService
{
    private readonly IAppLogService _log;
    private readonly ISteamService _steam;
    private readonly IOnlineFixCatalogService _catalog;
    private readonly OnlineFixArchiveDownloader _downloader;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly string _downloadsRoot;

    public OnlineFixService(IAppLogService log, ISteamService steam, IOnlineFixCatalogService catalog)
        : this(log, steam, catalog, new HttpClient(new HttpClientHandler { AllowAutoRedirect = true })
        { Timeout = TimeSpan.FromSeconds(60) }, Path.Combine(Path.GetTempPath(), "nexaplay-onlinefix")) { }

    internal OnlineFixService(IAppLogService log, ISteamService steam, IOnlineFixCatalogService catalog,
        HttpClient http, string downloadsRoot)
    {
        _log = log;
        _steam = steam;
        _catalog = catalog;
        _downloader = new OnlineFixArchiveDownloader(http);
        _downloadsRoot = downloadsRoot;
    }

    public Task RefreshCatalogAsync(CancellationToken ct = default) => _catalog.RefreshAsync(ct);

    public async Task<bool> CheckAvailabilityAsync(int appId, CancellationToken ct = default) =>
        (await _catalog.GetAsync(appId, ct).ConfigureAwait(false))?.Files.Count > 0;

    public bool IsApplied(int appId)
    {
        try
        {
            var root = _steam.ResolveGameInstallPath(appId);
            return root is not null && (OnlineFixFileTransaction.HasTransaction(root, appId) || File.Exists(LegacyLogPath(root, appId)));
        }
        catch (Exception ex)
        {
            _log.Log("OnlineFix", $"Cannot inspect installed state appId={appId}: {ex.Message}");
            return false;
        }
    }

    public Task<OnlineFixResult> ApplyAsync(int appId, IProgress<BypassProgressState> progress, CancellationToken ct = default) =>
        RunAsync(appId, false, progress, ct);

    public Task<OnlineFixResult> UnfixAsync(int appId, CancellationToken ct = default) => RunAsync(appId, true, null, ct);

    private async Task<OnlineFixResult> RunAsync(int appId, bool remove, IProgress<BypassProgressState>? progress, CancellationToken ct)
    {
        var entered = false;
        try
        {
            entered = await _operationGate.WaitAsync(0, ct).ConfigureAwait(false);
            if (!entered) return new(OnlineFixOutcome.Failed, "Proses Online-Fix lain masih berjalan. Tunggu hingga selesai.");
            // Filesystem resolution, decompression, hashing, and rollback stay off the UI thread.
            return await Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();
                if (appId <= 0) throw new InvalidDataException("AppID tidak valid.");
                var root = _steam.ResolveGameInstallPath(appId);
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                    return new OnlineFixResult(OnlineFixOutcome.NotInstalled, "Game belum terinstall atau folder game tidak ditemukan.");
                if (remove)
                {
                    if (OnlineFixFileTransaction.HasTransaction(root, appId)) OnlineFixFileTransaction.Remove(root, appId, ct);
                    else if (File.Exists(LegacyLogPath(root, appId)))
                    {
                        OnlineFixFileTransaction.RemoveLegacy(root, LegacyLogPath(root, appId), ct);
                        return new OnlineFixResult(OnlineFixOutcome.Removed,
                            "File Online-Fix lama berhasil dihapus. Backup file asli tidak tersedia untuk pemasangan versi lama.");
                    }
                    return new OnlineFixResult(OnlineFixOutcome.Removed, "Online-Fix dihapus dan file asli dipulihkan.");
                }
                if (IsApplied(appId)) throw new IOException("Online-Fix atau data pemulihan masih ada. Jalankan Remove Online-Fix dahulu.");
                var entry = await _catalog.GetAsync(appId, ct).ConfigureAwait(false);
                if (entry is null || entry.Files.Count == 0)
                    return new OnlineFixResult(OnlineFixOutcome.NotAvailable, "Online-Fix pada game ini belum tersedia di online_fix.json.");
                Directory.CreateDirectory(_downloadsRoot);
                var work = Path.Combine(_downloadsRoot, $"{appId}-{Guid.NewGuid():N}");
                Directory.CreateDirectory(work);
                try
                {
                    var packageRoot = Path.Combine(work, "packages");
                    Directory.CreateDirectory(packageRoot);
                    var paths = new List<string>();
                    for (var i = 0; i < entry.Files.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var package = entry.Files[i];
                        var path = OnlineFixFileTransaction.SafePath(packageRoot, package.Filename);
                        var index = i;
                        var lastPercent = -1;
                        var lastReport = DateTime.UtcNow;
                        Report(BypassStatus.Downloading, "download", i * 65 / entry.Files.Count);
                        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        deadline.CancelAfter(TimeSpan.FromMinutes(20));
                        try
                        {
                            await _downloader.DownloadAsync(package, path, (read, total) =>
                            {
                                var percent = (int)((index + (total > 0 ? Math.Min(1, (double)read / total) : 0)) * 65 / entry.Files.Count);
                                if (percent > lastPercent || DateTime.UtcNow - lastReport >= TimeSpan.FromMilliseconds(250))
                                {
                                    lastPercent = percent;
                                    lastReport = DateTime.UtcNow;
                                    progress?.Report(new BypassProgressState { AppId = appId, Status = BypassStatus.Downloading,
                                        Phase = "download", Percent = percent, BytesRead = read, TotalBytes = total,
                                        CurrentPart = index + 1, TotalParts = entry.Files.Count,
                                        Message = $"Mengunduh paket {index + 1}/{entry.Files.Count}..." });
                                }
                            }, deadline.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                        { throw new TimeoutException("Waktu unduhan Online-Fix habis. Silakan coba lagi."); }
                        paths.Add(path);
                    }
                    var staging = Path.Combine(work, "staging");
                    Report(BypassStatus.Extracting, "extract", 65);
                    var lastExtractReport = DateTime.UtcNow;
                    OnlineFixFileTransaction.Extract(paths, staging, appId, (_, _) =>
                    {
                        if (DateTime.UtcNow - lastExtractReport >= TimeSpan.FromMilliseconds(250))
                        {
                            Report(BypassStatus.Extracting, "extract", 70);
                            lastExtractReport = DateTime.UtcNow;
                        }
                    }, ct, entry.ArchivePassword);
                    Report(BypassStatus.Applying, "install", 75);
                    var lastInstallPercent = -1;
                    OnlineFixFileTransaction.Install(root, staging, appId, pct =>
                    {
                        var percent = 75 + pct * 24 / 100;
                        if (percent <= lastInstallPercent) return;
                        lastInstallPercent = percent;
                        Report(BypassStatus.Applying, "install", percent);
                    }, ct);
                    _log.Log("OnlineFix", $"Applied appId={appId}; durable transaction and backups retained");
                    return new OnlineFixResult(OnlineFixOutcome.Applied, "Online-Fix berhasil diterapkan.");
                }
                finally
                {
                    try
                    {
                        var absolute = Path.GetFullPath(work);
                        if (absolute.StartsWith(Path.GetFullPath(_downloadsRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                            StringComparison.OrdinalIgnoreCase)) Directory.Delete(absolute, recursive: true);
                    }
                    catch (Exception ex) { _log.Log("OnlineFix", $"Temporary cleanup failed: {ex.Message}"); }
                }
                void Report(BypassStatus status, string phase, int percent) => progress?.Report(
                    new BypassProgressState { AppId = appId, Status = status, Phase = phase, Percent = percent });
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _log.Log("OnlineFix", $"Operation cancelled appId={appId}");
            return new(OnlineFixOutcome.Cancelled, remove
                ? "Penghapusan dibatalkan. Data pemulihan disimpan; jalankan Remove Online-Fix lagi untuk melanjutkan."
                : "Proses Online-Fix dibatalkan. Perubahan yang belum selesai dipulihkan.");
        }
        catch (Exception ex) when (ex is SharpCompress.Common.CryptographicException or System.Security.Cryptography.CryptographicException)
        {
            _log.Log("OnlineFix", $"Archive decryption failed appId={appId}; verify mapping password or archive integrity");
            return new(OnlineFixOutcome.Failed,
                "Password arsip Online-Fix tidak cocok atau arsip terenkripsi rusak. Periksa field password pada mapping.");
        }
        catch (Exception ex)
        {
            _log.Log("OnlineFix", $"Operation failed appId={appId}: {ex}");
            return new(OnlineFixOutcome.Failed, ex.Message);
        }
        finally { if (entered) _operationGate.Release(); }
    }

    private static string LegacyLogPath(string root, int appId) => Path.Combine(root, $"{AppConstants.FixLogPrefix}{appId}.log");
}
