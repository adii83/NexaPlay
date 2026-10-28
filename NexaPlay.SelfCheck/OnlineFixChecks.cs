using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Infrastructure.Services;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

internal static partial class OnlineFixChecks
{
    private const string Id = "fixture_drive_id_42";
    private static int _checks;
    private static string Mapping(int appId = 42) => $$"""[{"appid":{{appId}},"files":[{"filename":"fix.rar","gdrive_id":"{{Id}}","gdrive_url":"https://drive.google.com/uc?export=download&id={{Id}}"}]}]""";

    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexaplay-onlinefix-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            CheckCatalogValidation();
            await CheckRefreshAsync(root);
            await CheckRefreshCancellationAsync(root);
            await CheckRateLimitAsync(root);
            await CheckDriveAsync(root);
            await CheckArchivePasswordsAsync(root);
            CheckTransactions(root);
            CheckUnsafeArchives(root);
            CheckRecoveryAndLocks(root);
            CheckLegacy(root);
            await CheckServiceAsync(root);
            Console.WriteLine($"Online-Fix self-check passed ({_checks} assertions): password defaults/overrides, encrypted archives, catalog freshness, Drive, staging, rollback, recovery, file locks, and operation results.");
        }
        finally
        {
            var absolute = Path.GetFullPath(root);
            if (absolute.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)) Directory.Delete(absolute, recursive: true);
        }
    }

    private static void CheckCatalogValidation()
    {
        var entry = OnlineFixCatalogService.Parse(Mapping())[42];
        Check(entry.Files.Single().DriveId == Id, "mapping lost Drive identity");
        Check(OnlineFixCatalogService.Parse("[]").Count == 0, "empty catalog must remove availability");
        foreach (var bad in new[]
        {
            "{}", "not-json", Mapping().Replace("\"appid\":42", "\"appid\":0"),
            Mapping().Replace("fix.rar", "../bad.rar"), Mapping().Replace("drive.google.com", "evil.test"),
            Mapping().Replace("export=download&id=", "export=download&wrong="),
            Mapping()[..^1] + "," + Mapping()[1..],
            Mapping().Replace("fix.rar", "fix.rar:stream")
        }) ExpectFailure(() => OnlineFixCatalogService.Parse(bad));
    }

    private static async Task CheckRefreshAsync(string root)
    {
        var clock = new FakeClock();
        var cache = Path.Combine(root, "catalog.json");
        var mode = "valid";
        var handler = new RouteHandler(request =>
        {
            Check(request.Method == HttpMethod.Get, "availability must not probe packages with HEAD");
            Check(request.RequestUri!.Host == "api.github.com" && request.RequestUri.AbsolutePath.EndsWith("/contents/online_fix.json") && request.RequestUri.Query.Contains("ref=main"), "old source used");
            Check(request.Headers.Accept.Any(a => a.MediaType == "application/vnd.github.raw+json"), "raw file API media type missing");
            Check(request.RequestUri.Query.Contains("nexaplay_check="), "CDN query missing");
            Check(request.Headers.CacheControl?.NoCache == true, "revalidation headers missing");
            if (mode == "304")
            {
                Check(request.Headers.IfNoneMatch.Any(e => e.Tag == "\"v1\""), "ETag lost");
                return new HttpResponseMessage(HttpStatusCode.NotModified);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(mode == "bad" ? "{" : mode == "empty" ? "[]" : mode == "changed" ? Mapping(70) : Mapping()) };
            response.Headers.ETag = new EntityTagHeaderValue("\"v1\"");
            return response;
        });
        using var http = new HttpClient(handler);
        var catalog = new OnlineFixCatalogService(new NullLogService(), http, cache, clock);
        await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => catalog.RefreshAsync()));
        Check(handler.Requests.Count == 1, "concurrent startup/detail requests were not coalesced");
        Check(await catalog.GetAsync(42) is not null && await catalog.GetAsync(999) is null, "availability must use AppID membership");
        Check(handler.Requests.Count == 1, "game clicks spam requests inside cooldown");
        mode = "304";
        clock.Advance(OnlineFixCatalogService.RefreshInterval);
        await catalog.RefreshAsync();
        Check(handler.Requests.Count == 2 && await catalog.GetAsync(42) is not null, "304 lost cached mapping");
        var disk = File.ReadAllText(cache);
        mode = "bad";
        clock.Advance(OnlineFixCatalogService.RefreshInterval);
        await catalog.RefreshAsync();
        await ExpectFailureAsync(async () => await catalog.GetAsync(42));
        Check(handler.Requests.Count == 3 && File.ReadAllText(cache) == disk, "invalid source poisoned cache or retried immediately");
        mode = "changed";
        clock.Advance(OnlineFixCatalogService.FailureInterval);
        Check(await catalog.GetAsync(42) is null && await catalog.GetAsync(70) is not null, "new mapping not published after retry");
        mode = "empty";
        clock.Advance(OnlineFixCatalogService.RefreshInterval);
        Check(await catalog.GetAsync(70) is null, "removed mapping remains available");

        mode = "valid";
        var restarted = new OnlineFixCatalogService(new NullLogService(), http, cache, clock);
        await restarted.RefreshAsync();
        Check(handler.Requests.Count == 6, "new app session did not revalidate disk cache");
        Check(await restarted.GetAsync(42) is not null, "session restart missing latest mapping");
        using var offline = new HttpClient(new RouteHandler(_ => throw new HttpRequestException("offline")));
        var offlineCatalog = new OnlineFixCatalogService(new NullLogService(), offline, cache, clock);
        await offlineCatalog.RefreshAsync();
        await ExpectFailureAsync(async () => await offlineCatalog.GetAsync(42));
        Check(File.ReadAllText(cache).Contains(Id), "offline startup destroyed valid cache");
    }

    private static async Task CheckRefreshCancellationAsync(string root)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new AsyncHandler(async (_, ct) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage();
        }));
        var catalog = new OnlineFixCatalogService(new NullLogService(), http, Path.Combine(root, "cancel-catalog.json"), new FakeClock());
        using var cts = new CancellationTokenSource();
        var running = catalog.RefreshAsync(cts.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var waiter = new CancellationTokenSource();
        var waiting = catalog.RefreshAsync(waiter.Token);
        waiter.Cancel();
        await ExpectCancelledAsync(() => waiting);
        Check(!running.IsCompleted, "cancelled waiter aborted another caller's refresh");
        cts.Cancel();
        await ExpectCancelledAsync(() => running);
    }

    private static async Task CheckRateLimitAsync(string root)
    {
        var clock = new FakeClock();
        var calls = 0;
        using var http = new HttpClient(new RouteHandler(_ =>
        {
            calls++;
            if (calls > 1) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Mapping()) };
            var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
            response.Headers.TryAddWithoutValidation("X-RateLimit-Reset", clock.GetUtcNow().AddMinutes(30).ToUnixTimeSeconds().ToString());
            return response;
        }));
        var catalog = new OnlineFixCatalogService(new NullLogService(), http, Path.Combine(root, "rate-catalog.json"), clock);
        await catalog.RefreshAsync();
        clock.Advance(TimeSpan.FromMinutes(2));
        await ExpectFailureAsync(async () => await catalog.GetAsync(42));
        Check(calls == 1, "GitHub rate-limit reset was ignored");
        clock.Advance(TimeSpan.FromMinutes(29));
        Check(await catalog.GetAsync(42) is not null && calls == 2, "catalog did not resume after GitHub reset");
    }

    private static async Task CheckDriveAsync(string root)
    {
        var bytes = Zip(("fix.dll", "payload"));
        var requests = 0;
        using var http = new HttpClient(new RouteHandler(request =>
        {
            requests++;
            if (requests == 1) return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent($"<form action=\"https://drive.usercontent.google.com/download\" method=\"get\"><input name=\"id\" value=\"{Id}\"><input name=\"confirm\" value=\"t\"><input name=\"uuid\" value=\"a&amp;b\"></form>", Encoding.UTF8, "text/html") };
            Check(request.RequestUri!.Host == "drive.usercontent.google.com" && request.RequestUri.Query.Contains("uuid=a%26b"), "Drive confirmation escaped identity");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        }));
        var path = Path.Combine(root, "drive.zip");
        await new OnlineFixArchiveDownloader(http).DownloadAsync(OnlineFixCatalogService.Parse(Mapping())[42].Files[0], path, (_, _) => { }, default);
        Check(requests == 2 && File.ReadAllBytes(path).SequenceEqual(bytes), "Drive confirmation changed archive bytes");
        foreach (var html in new[] { "<html>quota exceeded</html>",
            $"<form action='https://evil.test/download'><input name='id' value='{Id}'><input name='confirm' value='t'></form>",
            "<form action='https://drive.usercontent.google.com/download'><input name='id' value='another_file_id'><input name='confirm' value='t'></form>" })
            ExpectFailure(() => OnlineFixArchiveDownloader.ParseConfirmation(html, Id));
        using var denied = new HttpClient(new RouteHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)));
        await ExpectFailureAsync(() => new OnlineFixArchiveDownloader(denied).DownloadAsync(
            OnlineFixCatalogService.Parse(Mapping())[42].Files[0], Path.Combine(root, "denied.rar"), (_, _) => { }, default));
        Check(!File.Exists(Path.Combine(root, "denied.rar")), "denied Drive wrote package");
    }

    private static void CheckTransactions(string root)
    {
        var game = MakeDir(root, "game");
        File.WriteAllText(Path.Combine(game, "fix.dll"), "original-bytes");
        var zip = Path.Combine(root, "good.zip");
        File.WriteAllBytes(zip, Zip(("42/fix.dll", "patched"), ("42/bin/unsteam.ini", "[Game]\r\nAppId=<appid>\r\n")));
        var stage = Path.Combine(root, "stage");
        OnlineFixFileTransaction.Extract([zip], stage, 42, (_, _) => { }, default);
        Check(File.ReadAllText(Path.Combine(stage, "bin", "unsteam.ini")).Contains("AppId=42"), "INI placeholder not patched");
        OnlineFixFileTransaction.Install(game, stage, 42, _ => { }, default);
        Check(File.ReadAllText(Path.Combine(game, "fix.dll")) == "patched", "install missing");
        Check(OnlineFixFileTransaction.HasTransaction(game, 42), "durable backup/journal missing");
        OnlineFixFileTransaction.Remove(game, 42, default);
        Check(File.ReadAllText(Path.Combine(game, "fix.dll")) == "original-bytes", "Remove did not restore original bytes");
        Check(!File.Exists(Path.Combine(game, "bin", "unsteam.ini")) && !OnlineFixFileTransaction.HasTransaction(game, 42), "Remove left installed files/journal");

        using var cancel = new CancellationTokenSource();
        ExpectCancelled(() => OnlineFixFileTransaction.Install(game, stage, 42, _ => cancel.Cancel(), cancel.Token));
        Check(File.ReadAllText(Path.Combine(game, "fix.dll")) == "original-bytes" && !File.Exists(Path.Combine(game, "bin", "unsteam.ini")), "cancel did not restore partially installed files");
        Check(!OnlineFixFileTransaction.HasTransaction(game, 42), "successful rollback retained active transaction");
        OnlineFixFileTransaction.Install(game, stage, 42, _ => { }, default);
        File.WriteAllText(Path.Combine(game, "fix.dll"), "user-newer-version");
        ExpectFailure(() => OnlineFixFileTransaction.Remove(game, 42, default));
        Check(File.ReadAllText(Path.Combine(game, "fix.dll")) == "user-newer-version" && OnlineFixFileTransaction.HasTransaction(game, 42), "Remove destroyed modified file or recovery record");
        File.WriteAllText(Path.Combine(game, "fix.dll"), "patched");
        OnlineFixFileTransaction.Remove(game, 42, default);
    }

    private static void CheckUnsafeArchives(string root)
    {
        foreach (var name in new[] { "../outside.dll", "/absolute.dll", "C:/outside.dll", "bin/../../outside.dll", "CON.dll", "bin/file.dll:ads", ".nexaplay-onlinefix/42/transaction.json", "file.dll.", "file.dll " })
        {
            var path = Path.Combine(root, "unsafe.zip");
            File.WriteAllBytes(path, Zip((name, "bad")));
            var staging = Path.Combine(root, "unsafe-" + Guid.NewGuid().ToString("N"));
            ExpectFailure(() => OnlineFixFileTransaction.Extract([path], staging, 42, (_, _) => { }, default), name);
        }
        Check(!File.Exists(Path.Combine(root, "outside.dll")), "archive escaped staging");
        var corrupt = Zip(("a.dll", "payload"));
        for (var i = 0; i < corrupt.Length - 46; i++)
            if (BitConverter.ToUInt32(corrupt, i) == 0x02014b50) { corrupt[i + 16] ^= 0xff; break; }
        var bad = Path.Combine(root, "bad-crc.zip");
        File.WriteAllBytes(bad, corrupt);
        ExpectFailure(() => OnlineFixFileTransaction.Extract([bad], Path.Combine(root, "bad-crc-stage"), 42, (_, _) => { }, default));
        var duplicate = Path.Combine(root, "duplicate.zip");
        File.WriteAllBytes(duplicate, Zip(("a.dll", "one"), ("A.dll", "two")));
        ExpectFailure(() => OnlineFixFileTransaction.Extract([duplicate], Path.Combine(root, "duplicate-stage"), 42, (_, _) => { }, default));
        var empty = Path.Combine(root, "empty.zip");
        File.WriteAllBytes(empty, Zip());
        ExpectFailure(() => OnlineFixFileTransaction.Extract([empty], Path.Combine(root, "empty-stage"), 42, (_, _) => { }, default));
    }

    private static void CheckRecoveryAndLocks(string root)
    {
        var game = MakeDir(root, "recovery-game");
        var stage = MakeDir(root, "recovery-stage");
        File.WriteAllText(Path.Combine(game, "a.dll"), "a-original");
        File.WriteAllText(Path.Combine(game, "b.dll"), "b-original");
        File.WriteAllText(Path.Combine(stage, "a.dll"), "a-fixed");
        File.WriteAllText(Path.Combine(stage, "b.dll"), "b-fixed");
        OnlineFixFileTransaction.Install(game, stage, 42, _ => { }, default);
        using (var locked = new FileStream(Path.Combine(game, "b.dll"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            ExpectFailure(() => OnlineFixFileTransaction.Remove(game, 42, default));
            Check(OnlineFixFileTransaction.HasTransaction(game, 42), "failed Remove cleared state");
            Check(File.ReadAllText(Path.Combine(game, "a.dll")) == "a-fixed", "preflight allowed partial Remove before detecting lock");
        }
        // Simulate a terminated application with a complete write-ahead record in Applying state.
        var manifest = OnlineFixFileTransaction.ManifestPath(game, 42);
        var journal = JsonSerializer.Deserialize<OnlineFixFileTransaction.Journal>(File.ReadAllText(manifest))!;
        journal.State = "Applying";
        File.WriteAllText(manifest, JsonSerializer.Serialize(journal));
        OnlineFixFileTransaction.Remove(game, 42, default);
        Check(File.ReadAllText(Path.Combine(game, "a.dll")) == "a-original" && File.ReadAllText(Path.Combine(game, "b.dll")) == "b-original", "interrupted transaction recovery failed");

        FileStream? lockDuringCommit = null;
        try
        {
            ExpectFailure(() => OnlineFixFileTransaction.Install(game, stage, 42, _ =>
            {
                lockDuringCommit ??= new FileStream(Path.Combine(game, "b.dll"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }, default));
            Check(OnlineFixFileTransaction.HasTransaction(game, 42), "blocked rollback discarded backups");
        }
        finally { lockDuringCommit?.Dispose(); }
        OnlineFixFileTransaction.Remove(game, 42, default);
        Check(File.ReadAllText(Path.Combine(game, "a.dll")) == "a-original", "retry after lock did not recover original");
        var orphan = Path.Combine(game, ".nexaplay-onlinefix", "42", "backup");
        Directory.CreateDirectory(orphan);
        File.WriteAllText(Path.Combine(orphan, "original.dll"), "backup-before-journal");
        OnlineFixFileTransaction.Remove(game, 42, default);
        Check(!OnlineFixFileTransaction.HasTransaction(game, 42), "pre-commit orphan could not be cleaned");
    }

    private static void CheckLegacy(string root)
    {
        var game = MakeDir(root, "legacy-game");
        var log = Path.Combine(game, "nexaplay-fix-log-42.log");
        var file = Path.Combine(game, "fix.dll");
        File.WriteAllText(file, "fix");
        File.WriteAllText(log, "[FIX]\nDate: test\nAppId: 42\nFiles:\nfix.dll\n../outside.dll\n[/FIX]");
        ExpectFailure(() => OnlineFixFileTransaction.RemoveLegacy(game, log, default));
        Check(File.Exists(file) && File.Exists(log), "unsafe legacy log partially deleted files");
        File.WriteAllText(log, "[FIX]\nDate: test\nAppId: 42\nFiles:\nfix.dll\n[/FIX]");
        using (var locked = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            ExpectFailure(() => OnlineFixFileTransaction.RemoveLegacy(game, log, default));
            Check(File.Exists(log), "locked legacy removal discarded retry log");
        }
        OnlineFixFileTransaction.RemoveLegacy(game, log, default);
        Check(!File.Exists(file) && !File.Exists(log), "valid legacy removal failed");
    }

    private static async Task CheckServiceAsync(string root)
    {
        var game = MakeDir(root, "service-game");
        var bytes = Zip(("fix.dll", "fixed"));
        var catalog = new FakeCatalog(OnlineFixCatalogService.Parse(Mapping())[42]);
        var requests = 0;
        using var http = new HttpClient(new RouteHandler(_ => { requests++; return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }; }));
        var service = new OnlineFixService(new NullLogService(), new FakeSteam(game), catalog, http, Path.Combine(root, "downloads"));
        Check(await service.CheckAvailabilityAsync(42) && !await service.CheckAvailabilityAsync(70) && requests == 0, "availability requested package or accepted unmapped app");
        Check((await service.ApplyAsync(70, new Capture())).Outcome == OnlineFixOutcome.NotAvailable && requests == 0, "unmapped game used fallback");
        // Progress intentionally has no delivered callbacks: completion must still be correct.
        var result = await service.ApplyAsync(42, new Capture());
        Check(result.Outcome == OnlineFixOutcome.Applied && service.IsApplied(42), "explicit success depends on progress callback");
        Check((await service.UnfixAsync(42)).Outcome == OnlineFixOutcome.Removed && !service.IsApplied(42), "explicit remove result incorrect");
        var missing = new OnlineFixService(new NullLogService(), new FakeSteam(null), catalog, http, Path.Combine(root, "missing-downloads"));
        Check((await missing.ApplyAsync(42, new Capture())).Outcome == OnlineFixOutcome.NotInstalled, "missing install not typed");

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var slow = new HttpClient(new AsyncHandler(async (_, ct) => { started.TrySetResult(); await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(); }));
        var blocking = new OnlineFixService(new NullLogService(), new FakeSteam(game), catalog, slow, Path.Combine(root, "slow-downloads"));
        using var cancel = new CancellationTokenSource();
        var running = blocking.ApplyAsync(42, new Capture(), cancel.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Check((await blocking.ApplyAsync(42, new Capture())).Outcome == OnlineFixOutcome.Failed, "concurrent apply accepted");
        Check((await blocking.UnfixAsync(42)).Outcome == OnlineFixOutcome.Failed, "Remove raced an active installation");
        cancel.Cancel();
        Check((await running.WaitAsync(TimeSpan.FromSeconds(5))).Outcome == OnlineFixOutcome.Cancelled, "download cancellation failed");
        Check(!File.Exists(Path.Combine(game, "fix.dll")), "cancelled download touched game");
    }

    private static string MakeDir(string root, string name) { var path = Path.Combine(root, name); Directory.CreateDirectory(path); return path; }
    internal static void CheckArchiveFixture(params string[] paths)
    {
        var staging = Path.Combine(Path.GetTempPath(), "nexaplay-archive-fixture-" + Guid.NewGuid().ToString("N"));
        try
        {
            OnlineFixFileTransaction.Extract(paths.Select(Path.GetFullPath).ToArray(), staging, 42, (_, _) => { }, default);
            var count = Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories).Count();
            Check(count > 0, "archive fixture has no extracted files");
            Console.WriteLine($"Archive fixture passed: {Path.GetFileName(paths[0])}, {paths.Length} volume(s), {count} files");
        }
        finally
        {
            var full = Path.GetFullPath(staging);
            if (full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) && Directory.Exists(full)) Directory.Delete(full, recursive: true);
        }
    }
    private static byte[] Zip(params (string Name, string Content)[] files)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
            foreach (var file in files) { using var writer = new StreamWriter(archive.CreateEntry(file.Name).Open(), new UTF8Encoding(false)); writer.Write(file.Content); }
        return output.ToArray();
    }
    private static void Check(bool value, string message) { _checks++; if (!value) throw new Exception("Online-Fix: " + message); }
    private static void ExpectFailure(Action action, string? context = null) { try { action(); } catch (Exception ex) when (ex is not OperationCanceledException) { _checks++; return; } throw new Exception("Online-Fix accepted invalid input: " + context); }
    private static async Task ExpectFailureAsync(Func<Task> action) { try { await action(); } catch (Exception ex) when (ex is not OperationCanceledException) { _checks++; return; } throw new Exception("Online-Fix accepted invalid request"); }
    private static void ExpectCancelled(Action action) { try { action(); } catch (OperationCanceledException) { _checks++; return; } throw new Exception("Online-Fix ignored cancellation"); }
    private static async Task ExpectCancelledAsync(Func<Task> action) { try { await action(); } catch (OperationCanceledException) { _checks++; return; } throw new Exception("Online-Fix ignored cancellation"); }
    private sealed class FakeClock : TimeProvider { private DateTimeOffset _now = new(2026, 10, 27, 0, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => _now; public void Advance(TimeSpan span) => _now += span; }
    private sealed class FakeCatalog(OnlineFixEntry entry) : IOnlineFixCatalogService { public Task RefreshAsync(CancellationToken ct = default) => Task.CompletedTask; public Task<OnlineFixEntry?> GetAsync(int appId, CancellationToken ct = default) => Task.FromResult(appId == entry.AppId ? entry : null); }
    private sealed class Capture : IProgress<BypassProgressState> { public void Report(BypassProgressState value) { } }
    private sealed class AsyncHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> route) : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => route(request, ct); }
    private sealed class FakeSteam(string? root) : ISteamService
    {
        public string? GetSteamBasePath() => root;
        public IReadOnlyList<string> GetLibraryPaths() => [];
        public IReadOnlyList<InstalledGame> ScanInstalledGames() => [];
        public string? ResolveGameInstallPath(int appId) => root;
        public string? GetGameName(int appId) => null;
        public Task<SteamFinalizeResult> FinalizeBypassAsync(int appId, string? launchOptions) => throw new NotSupportedException();
        public Task RestoreManagedManifestReadOnlyAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RestartAsync() => Task.CompletedTask;
    }
}
