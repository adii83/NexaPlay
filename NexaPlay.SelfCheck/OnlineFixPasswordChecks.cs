using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Infrastructure.Services;
using SharpCompress.Archives;
using SharpCompress.Readers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

internal static partial class OnlineFixChecks
{
    private const string CustomArchivePassword = "adi83geel";
    private const string WrongArchivePassword = "wrong-explicit-password-841";
    private static string PasswordMapping(string? password, bool includePassword, string filename = "fix.rar") =>
        (includePassword ? Mapping().Replace("\"appid\":42,", "\"appid\":42,\"password\":" + JsonSerializer.Serialize(password) + ",") : Mapping())
        .Replace("fix.rar", filename);

    private static async Task CheckArchivePasswordsAsync(string root)
    {
        foreach (var entry in new[]
        {
            OnlineFixCatalogService.Parse(PasswordMapping(null, false))[42],
            OnlineFixCatalogService.Parse(PasswordMapping(null, true))[42],
            OnlineFixCatalogService.Parse(PasswordMapping("", true))[42]
        }) Check(entry.ArchivePassword == "online-fix.me", "missing/null/empty password did not select default");
        var explicitEntry = OnlineFixCatalogService.Parse(PasswordMapping(CustomArchivePassword, true))[42];
        Check(explicitEntry.Password == CustomArchivePassword && explicitEntry.ArchivePassword == CustomArchivePassword, "mapping password not preserved");
        Check(!explicitEntry.ToString().Contains(CustomArchivePassword, StringComparison.Ordinal), "entry diagnostics expose password");
        Check(OnlineFixCatalogService.Parse(PasswordMapping(" password with spaces ", true))[42].ArchivePassword == " password with spaces ", "password whitespace was trimmed");
        foreach (var value in new[] { "123", "true", "[]", "{}", JsonSerializer.Serialize(new string('x', 1025)), "\"bad\\u0000password\"" })
            ExpectFailure(() => OnlineFixCatalogService.Parse(Mapping().Replace("\"appid\":42,", "\"appid\":42,\"password\":" + value + ",")));

        await CheckPasswordCacheAsync(root);
        var fixtures = Path.Combine("NexaPlay.SelfCheck", "Fixtures", "OnlineFix");
        using (var plain = ArchiveFactory.OpenArchive(Path.Combine(fixtures, "plain.rar"), new ReaderOptions()))
            Check(plain.Entries.Where(e => !e.IsDirectory).All(e => !e.IsEncrypted), "plain fixture unexpectedly encrypted");
        using (var encrypted = ArchiveFactory.OpenArchive(Path.Combine(fixtures, "default.rar"),
            new ReaderOptions { Password = OnlineFixEntry.DefaultArchivePassword }))
            Check(encrypted.Entries.Any(e => !e.IsDirectory && e.IsEncrypted), "encrypted fixture is not really encrypted");

        foreach (var sample in new[]
        {
            ("plain.rar", (string?)null, false),
            ("plain.rar", WrongArchivePassword, true),
            ("default.rar", (string?)null, false),
            ("default-headers.rar", (string?)null, false),
            ("default-aes.zip", (string?)null, false),
            ("custom-headers.rar", CustomArchivePassword, true),
            ("custom-headers.7z", CustomArchivePassword, true),
            ("default.rar", (string?)null, true),
            ("default.rar", "", true)
        }) await CheckPasswordInstallAsync(root, fixtures, sample.Item1, sample.Item2, sample.Item3, success: true);

        foreach (var sample in new[]
        {
            ("custom-headers.rar", (string?)null, false),
            ("custom-headers.rar", WrongArchivePassword, true),
            ("default-headers.rar", WrongArchivePassword, true),
            ("default.rar", WrongArchivePassword, true),
            ("default-aes.zip", WrongArchivePassword, true),
            ("custom-headers.7z", WrongArchivePassword, true)
        }) await CheckPasswordInstallAsync(root, fixtures, sample.Item1, sample.Item2, sample.Item3, success: false);

        using var cts = new CancellationTokenSource();
        var cancelledRoot = MakeDir(root, "encrypted-cancel");
        var game = MakeDir(cancelledRoot, "game");
        File.WriteAllText(Path.Combine(game, "payload.txt"), "original");
        using var http = new HttpClient(new RouteHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new ByteArrayContent(File.ReadAllBytes(Path.Combine(fixtures, "default-headers.rar"))) }));
        var service = new OnlineFixService(new NullLogService(), new FakeSteam(game),
            new FakeCatalog(OnlineFixCatalogService.Parse(Mapping())[42]), http, Path.Combine(cancelledRoot, "downloads"));
        var result = await service.ApplyAsync(42, new CancelAtExtraction(cts), cts.Token);
        Check(result.Outcome == OnlineFixOutcome.Cancelled && File.ReadAllText(Path.Combine(game, "payload.txt")) == "original",
            "cancel before encrypted extraction touched game");
        Check(!service.IsApplied(42), "cancelled encrypted extraction marked applied");
    }

    private static async Task CheckPasswordInstallAsync(string root, string fixtures, string filename,
        string? password, bool includePassword, bool success)
    {
        var work = MakeDir(root, "password-" + Guid.NewGuid().ToString("N"));
        var game = MakeDir(work, "game");
        var target = Path.Combine(game, "payload.txt");
        File.WriteAllText(target, "original-game-file");
        var logs = new PasswordCheckLog();
        var entry = OnlineFixCatalogService.Parse(PasswordMapping(password, includePassword, filename))[42];
        using var http = new HttpClient(new RouteHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new ByteArrayContent(File.ReadAllBytes(Path.Combine(fixtures, filename))) }));
        var downloads = Path.Combine(work, "downloads");
        var service = new OnlineFixService(logs, new FakeSteam(game), new FakeCatalog(entry), http, downloads);
        var result = await service.ApplyAsync(42, new Capture());
        if (success)
        {
            Check(result.Outcome == OnlineFixOutcome.Applied, $"password install failed for {filename}: {result.Message}");
            Check(File.ReadAllText(target) == "Online-Fix password fixture.\n", $"decrypted bytes incorrect for {filename}");
            Check(service.IsApplied(42), "encrypted install did not retain recovery record");
            Check((await service.UnfixAsync(42)).Succeeded && File.ReadAllText(target) == "original-game-file", "encrypted Remove did not restore original");
        }
        else
        {
            Check(result.Outcome == OnlineFixOutcome.Failed, $"wrong password accepted for {filename}");
            Check(File.ReadAllText(target) == "original-game-file" && !service.IsApplied(42), "wrong password changed game files/state");
            Check(!Directory.EnumerateFiles(game, "*", SearchOption.AllDirectories).Any(p => p != target), "failed decryption left files in game");
        }
        Check(!Directory.Exists(downloads) || !Directory.EnumerateFileSystemEntries(downloads).Any(), "password operation leaked staging files");
        Check(!result.Message.Contains(WrongArchivePassword, StringComparison.Ordinal) &&
            !(await logs.GetFullLogAsync()).Contains(WrongArchivePassword, StringComparison.Ordinal), "failure text/log leaks password");
    }

    private static async Task CheckPasswordCacheAsync(string root)
    {
        var path = Path.Combine(root, "password-catalog.json");
        var clock = new FakeClock();
        var unchanged = false;
        using var http = new HttpClient(new RouteHandler(_ =>
        {
            var response = new HttpResponseMessage(unchanged ? HttpStatusCode.NotModified : HttpStatusCode.OK);
            response.Headers.ETag = new EntityTagHeaderValue("\"password-fixture\"");
            response.Content = new StringContent(PasswordMapping(CustomArchivePassword, true));
            return response;
        }));
        var first = new OnlineFixCatalogService(new NullLogService(), http, path, clock);
        Check((await first.GetAsync(42))?.ArchivePassword == CustomArchivePassword, "network mapping lost password");
        unchanged = true;
        var restarted = new OnlineFixCatalogService(new NullLogService(), http, path, clock);
        Check((await restarted.GetAsync(42))?.ArchivePassword == CustomArchivePassword, "cached/304 mapping lost password after restart");
    }

    private sealed class CancelAtExtraction(CancellationTokenSource cts) : IProgress<BypassProgressState>
    {
        public void Report(BypassProgressState state) { if (state.Status == BypassStatus.Extracting) cts.Cancel(); }
    }

    private sealed class PasswordCheckLog : IAppLogService
    {
        private readonly ConcurrentQueue<string> _messages = new();
        public event EventHandler<string>? LogAppended { add { } remove { } }
        public void Log(string message) => _messages.Enqueue(message);
        public void Log(string category, string message) => Log(category + ": " + message);
        public IReadOnlyList<string> GetRecentLogs(int count = 200) => _messages.TakeLast(count).ToArray();
        public Task<string> GetFullLogAsync() => Task.FromResult(string.Join("\n", _messages));
        public void Clear() => _messages.Clear();
    }
}
