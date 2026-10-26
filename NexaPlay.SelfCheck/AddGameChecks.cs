using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Infrastructure.Services;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;

internal static class AddGameChecks
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexaplay-add-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            CheckConfiguration();
            CheckPayload(root);
            await CheckFallbackAsync(root);
            await CheckLegacyBytesAsync(root);
            await CheckCancellationAsync(root);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void CheckConfiguration()
    {
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine("NexaPlay", "data", "api.json")));
        var apis = config.RootElement.GetProperty("api_list").EnumerateArray().ToArray();
        var expected = new[] { "NexaPlay R2", "ProjectLightningManifests", "SPIN0ZAi", "sojogamesdatabase1", "sojogames2", "ManifestHub", "GameHub", "Ryuu", "Sadie", "TwentyTwo Cloud", "Sushi" };
        Check(apis.Select(a => a.GetProperty("name").GetString()).SequenceEqual(expected), "API priority changed");
        Check(apis.Skip(1).Take(5).All(a => a.GetProperty("lua_processing").GetString() == "auto"), "GitHub preparation missing");
        Check(!apis[0].TryGetProperty("lua_processing", out _), "R2 should be ready to install");
        using var codes = JsonDocument.Parse("[200,\"2xx\"]");
        Check(AddGameService.IsSuccessStatus(206, codes.RootElement[1]), "2xx rejects 206");
        Check(!AddGameService.IsSuccessStatus(206, codes.RootElement[0]), "numeric 200 accepts 206");
        Check(!AddGameService.IsSuccessStatus(404, codes.RootElement[1]), "2xx accepts 404");
        Check(AddGamePayloadBuilder.ResolveMode("auto", "https://github.com/SPIN0ZAi/repo") == "spin0zai", "SPIN0ZAi mode not resolved");
        Check(AddGamePayloadBuilder.ResolveMode("standard", "https://github.com/SPIN0ZAi/repo") == "standard", "explicit mode ignored");
    }

    private static void CheckPayload(string root)
    {
        const string lua = "-- original\r\naddappid(42)\r\n  adddlc(43)\r\nsetManifestid(44, \"111\")\r\nprint('discard only in spin mode')\r\n--setManifestid(45, \"222\")\r\n";
        var source = Path.Combine(root, "source.zip");
        File.WriteAllBytes(source, ZipBytes(
            ("repo-42/deep/42.lua", Encoding.UTF8.GetBytes(lua)),
            ("repo-42/manifest/44_111.manifest", new byte[] { 0, 1, 254, 255 }),
            ("repo-42/other/helper.lua", Encoding.UTF8.GetBytes("addappid(99)")),
            ("repo-42/README.md", Encoding.UTF8.GetBytes("ignore")),
            ("__MACOSX/hidden.lua", Encoding.UTF8.GetBytes("ignore"))));
        foreach (var mode in new[] { "standard", "spin0zai" })
        {
            var output = Path.Combine(root, mode + ".zip");
            AddGamePayloadBuilder.Build(source, output, mode, default);
            AddGamePayloadBuilder.ValidateInstallable(output, "42", default);
            using var archive = ZipFile.OpenRead(output);
            Check(archive.Entries.Select(e => e.FullName).Order().SequenceEqual(new[] { "42.lua", "44_111.manifest", "helper.lua" }.Order()), "final ZIP contains folders/unwanted files");
            var expected = mode == "standard" ? lua.Replace("\r\n", "\n")
                : "addappid(42)\n  adddlc(43)\nsetManifestid(44, \"111\")";
            expected += "\n\n-- Made with love by NexaPlay ID \U0001f5a4\U0001f90d";
            Check(ReadEntry(archive, "42.lua").SequenceEqual(Encoding.UTF8.GetBytes(expected)), "Lightning Lua rules differ for " + mode);
            Check(ReadEntry(archive, "44_111.manifest").SequenceEqual(new byte[] { 0, 1, 254, 255 }), "manifest bytes changed");
            var steam = Path.Combine(root, "steam-" + mode);
            var installed = AddGameService.InstallLuaFromZip("42", output, steam);
            Check(File.ReadAllBytes(installed).SequenceEqual(ReadEntry(archive, "42.lua")), "installer rewrites Lua");
            Check(File.ReadAllBytes(Path.Combine(steam, "depotcache", "44_111.manifest")).SequenceEqual(ReadEntry(archive, "44_111.manifest")), "installer changes manifest");
        }

        var collisionSource = Path.Combine(root, "collisions.zip");
        File.WriteAllBytes(collisionSource, ZipBytes(
            ("repo/a/42.lua", Encoding.UTF8.GetBytes("addappid(1)")),
            ("repo/b/42.lua", Encoding.UTF8.GetBytes("addappid(2)")),
            ("repo/b/42.lua", Encoding.UTF8.GetBytes("addappid(3)"))));
        var collisionFinal = Path.Combine(root, "collisions-final.zip");
        AddGamePayloadBuilder.Build(collisionSource, collisionFinal, "standard", default);
        using (var archive = ZipFile.OpenRead(collisionFinal))
            Check(archive.Entries.Count == 1 && Encoding.UTF8.GetString(ReadEntry(archive, "42.lua")).StartsWith("addappid(3)"), "native overwrite winner differs");

        foreach (var unsafeName in new[] { "../escape.lua", "repo/../../escape.lua", "repo/CON.lua", "repo/file.lua:stream" })
        {
            var unsafeZip = Path.Combine(root, "unsafe.zip");
            File.WriteAllBytes(unsafeZip, ZipBytes((unsafeName, Encoding.UTF8.GetBytes("bad"))));
            ExpectInvalid(() => AddGamePayloadBuilder.Build(unsafeZip, Path.Combine(root, "unsafe-final.zip"), "standard", default));
            Check(!File.Exists(Path.Combine(root, "escape.lua")), "ZIP escaped staging");
        }
        var corruptZip = Path.Combine(root, "bad-crc.zip");
        var corrupt = ZipBytes(("repo/42.lua", Encoding.UTF8.GetBytes("addappid(42)")));
        for (var i = 0; i < corrupt.Length - 46; i++)
            if (BitConverter.ToUInt32(corrupt, i) == 0x02014b50)
            {
                corrupt[i + 16] ^= 0xff; // Corrupt the recorded CRC without breaking decompression.
                break;
            }
        File.WriteAllBytes(corruptZip, corrupt);
        ExpectInvalid(() => AddGamePayloadBuilder.Build(corruptZip, Path.Combine(root, "bad-crc-final.zip"), "standard", default));
        ExpectInvalid(() => AddGamePayloadBuilder.ValidateInstallable(corruptZip, "42", default));
        var cancelled = new CancellationToken(true);
        try
        {
            AddGamePayloadBuilder.Build(source, Path.Combine(root, "cancelled.zip"), "standard", cancelled);
            throw new Exception("preparation ignored cancellation");
        }
        catch (OperationCanceledException) { }
        Check(!Directory.EnumerateDirectories(root, "payload-*").Any(), "preparation staging leaked");
    }

    private static async Task CheckFallbackAsync(string root)
    {
        var valid = ZipBytes(("branch-42/sub/42.lua", Encoding.UTF8.GetBytes("addappid(42)\nsetManifestid(44, \"111\")")),
            ("branch-42/sub/44_111.manifest", new byte[] { 1, 2, 3 }));
        var missingLua = ZipBytes(("branch/readme.txt", new byte[] { 1 }));
        var calls = new List<string>();
        using var http = new HttpClient(new RouteHandler(request =>
        {
            var route = request.RequestUri!.AbsolutePath.Split('/')[1];
            calls.Add(route);
            return new HttpResponseMessage(route == "missing" ? HttpStatusCode.NotFound : HttpStatusCode.PartialContent)
            {
                Content = new ByteArrayContent(route switch
                {
                    "broken" => Encoding.UTF8.GetBytes("not a ZIP"),
                    "no-lua" => missingLua,
                    _ => valid
                })
            };
        }));
        var config = WriteConfig(root, "fallback", new[] { "missing", "broken", "no-lua", "valid", "unused" }, true);
        var steam = new FakeSteam(Path.Combine(root, "fallback-steam"));
        var downloads = Path.Combine(root, "fallback-downloads");
        var service = new AddGameService(new NullLogService(), steam, http, config, downloads);
        var states = new CaptureProgress();
        await service.AddGameAsync("42", states);
        Check(calls.SequenceEqual(new[] { "missing", "broken", "no-lua", "valid" }), "fallback did not reject invalid source or stop at success");
        Check(states.Items.Last().Status == BypassStatus.Applied, "prepared payload not installed");
        var installed = File.ReadAllText(Path.Combine(steam.Root, "config", "stplug-in", "42.lua"));
        Check(installed.Contains("\nsetManifestid(44, \"111\")") && !installed.Contains("--setManifestid"), "active manifest binding was commented");
        Check(!Directory.EnumerateFiles(downloads, "*", SearchOption.AllDirectories).Any(), "accepted/rejected downloads leaked");
    }

    private static async Task CheckLegacyBytesAsync(string root)
    {
        var original = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("addappid(42)\r\nsetManifestid(44, \"111\")\r\n-- unchanged\n")).ToArray();
        var bytes = ZipBytes(("99.lua", new byte[] { 99 }), ("42.lua", original), ("44_111.manifest", new byte[] { 7, 8 }));
        using var http = new HttpClient(new RouteHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }));
        var config = WriteConfig(root, "legacy", new[] { "r2" }, false);
        var steam = new FakeSteam(Path.Combine(root, "legacy-steam"));
        var service = new AddGameService(new NullLogService(), steam, http, config, Path.Combine(root, "legacy-downloads"));
        var states = new CaptureProgress();
        await service.AddGameAsync("42", states);
        Check(states.Items.Last().Status == BypassStatus.Applied, "ready-made payload failed");
        Check(File.ReadAllBytes(Path.Combine(steam.Root, "config", "stplug-in", "42.lua")).SequenceEqual(original), "ready-made Lua bytes/BOM/line endings changed");

        var fallbackZip = Path.Combine(root, "numeric-fallback.zip");
        File.WriteAllBytes(fallbackZip, ZipBytes(("nested/99.lua", original)));
        var installed = AddGameService.InstallLuaFromZip("42", fallbackZip, Path.Combine(root, "numeric-steam"));
        Check(Path.GetFileName(installed) == "42.lua" && File.ReadAllBytes(installed).SequenceEqual(original), "numeric Lua fallback lost");
    }

    private static async Task CheckCancellationAsync(string root)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new BlockingHandler(started, cancelled));
        var config = WriteConfig(root, "cancel", new[] { "slow", "unused" }, true);
        var steam = new FakeSteam(Path.Combine(root, "cancel-steam"));
        var downloads = Path.Combine(root, "cancel-downloads");
        var service = new AddGameService(new NullLogService(), steam, http, config, downloads);
        var states = new CaptureProgress();
        var running = service.AddGameAsync("42", states);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        service.CancelAdd("42");
        await running.WaitAsync(TimeSpan.FromSeconds(5));
        Check(cancelled.Task.IsCompleted && states.Items.Last().Status == BypassStatus.Cancelled, "cancel was treated as provider failure");
        Check(!Directory.Exists(steam.Root), "cancel touched Steam");
        Check(!Directory.EnumerateFiles(downloads, "*", SearchOption.AllDirectories).Any(), "cancelled download leaked");
    }

    private static string WriteConfig(string root, string name, string[] sources, bool prepare)
    {
        var path = Path.Combine(root, name + "-api.json");
        var rows = sources.Select(source =>
        {
            var row = new Dictionary<string, object>
            {
                ["name"] = source, ["url"] = "https://fixture.test/" + source + "/<appid>",
                ["success_code"] = prepare ? "2xx" : (object)200, ["unavailable_code"] = 404, ["enabled"] = true
            };
            if (prepare) row["lua_processing"] = "auto";
            return row;
        });
        File.WriteAllText(path, JsonSerializer.Serialize(new { api_list = rows }));
        return path;
    }

    private static byte[] ZipBytes(params (string Name, byte[] Bytes)[] files)
    {
        using var result = new MemoryStream();
        using (var zip = new ZipArchive(result, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var file in files)
            {
                using var output = zip.CreateEntry(file.Name).Open();
                output.Write(file.Bytes);
            }
        return result.ToArray();
    }

    private static byte[] ReadEntry(ZipArchive zip, string name)
    {
        using var input = zip.GetEntry(name)!.Open();
        using var result = new MemoryStream();
        input.CopyTo(result);
        return result.ToArray();
    }

    private static void ExpectInvalid(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new Exception("invalid ZIP was accepted");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Add Game: " + message);
    }

    private sealed class CaptureProgress : IProgress<BypassProgressState>
    {
        public List<BypassProgressState> Items { get; } = [];
        public void Report(BypassProgressState value) => Items.Add(value);
    }

    private sealed class BlockingHandler(TaskCompletionSource started, TaskCompletionSource cancelled) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            started.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
            throw new Exception("blocking handler unexpectedly resumed");
        }
    }

    private sealed class FakeSteam(string root) : ISteamService
    {
        public string Root => root;
        public string? GetSteamBasePath() => root;
        public IReadOnlyList<string> GetLibraryPaths() => [];
        public IReadOnlyList<InstalledGame> ScanInstalledGames() => [];
        public string? ResolveGameInstallPath(int appId) => null;
        public string? GetGameName(int appId) => null;
        public Task<SteamFinalizeResult> FinalizeBypassAsync(int appId, string? launchOptions) => throw new NotSupportedException();
        public Task RestoreManagedManifestReadOnlyAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RestartAsync() => Task.CompletedTask;
    }
}
