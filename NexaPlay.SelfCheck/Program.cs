using Microsoft.Data.Sqlite;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Helpers;
using NexaPlay.Core.Models;
using NexaPlay.Infrastructure.Services;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

if (args.Length >= 2 && args[0] == "--online-fix-archive")
{
    OnlineFixChecks.CheckArchiveFixture(args[1..]);
    return;
}

if (args.Contains("--online-fix-only", StringComparer.Ordinal))
{
    await OnlineFixChecks.RunAsync();
    return;
}

if (args.Contains("--add-game-only", StringComparer.Ordinal))
{
    await AddGameChecks.RunAsync();
    Console.WriteLine("Add Game self-check passed.");
    return;
}

if (args.Contains("--dynamic-feed-only", StringComparer.Ordinal))
{
    await AssertDynamicFeedsAsync();
    Console.WriteLine("Dynamic feed self-check passed.");
    return;
}

AssertPhysicalAppDataPath();
await OnlineFixChecks.RunAsync();
await AddGameChecks.RunAsync();
AssertImageUris();
AssertDetailHeroSelection();
await AssertLoadCoordinatorAsync();
AssertProtectionParity();
await AssertCatalogAsync();
await AssertHomeRecentFlowAsync();
await AssertCachePoliciesAsync();
await AssertDetailCacheAsync();
await AssertCoverEpochAsync();
await AssertDynamicFeedsAsync();
AssertBundledSeed();
if (Environment.GetEnvironmentVariable("NEXAPLAY_BENCHMARK") == "1")
    await BenchmarkBundledCatalogAsync();

Console.WriteLine("Self-check passed: Add Game payloads/fallback/cancellation, SQLite ordering/search/filter/patch, Home Recent, detail hero selection, cache safety, detail dedup/fallback, and dynamic feeds.");

static void AssertPhysicalAppDataPath()
{
    if (!OperatingSystem.IsWindows())
        return;
    var expected = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA")!, AppConstants.AppDataFolder);
    var actual = AppConstants.AppDataPath.StartsWith(@"\\?\", StringComparison.Ordinal)
        ? AppConstants.AppDataPath[4..]
        : AppConstants.AppDataPath;
    Assert(Path.GetFullPath(expected) == actual, "physical NexaPlay AppData root mismatch");
}

static void AssertImageUris()
{
    var cases = new (string? Raw, bool Valid)[]
    {
        (null, false), ("", false), ("https://example.test/a.jpg", true),
        (@"C:\Temp\a.jpg", true), (@"\\server\share\a.jpg", true), ("not a URI", false), ("NO CONTENT", false)
    };
    foreach (var item in cases)
        Assert(ImageUriResolver.TryCreate(item.Raw, out _) == item.Valid, $"image URI case failed: {item.Raw}");
}

static void AssertDetailHeroSelection()
{
    var game = new GameEntry
    {
        AppId = 42,
        LibraryHero2xUrl = "catalog-hero-2x",
        BackgroundRawImageUrl = "catalog-background",
        HeaderImageUrl = "catalog-header"
    };

    static GameDetailEntry Detail(string assets, string? background = "detail-background") => new()
    {
        AppId = 42,
        RawMetadataJson = $$"""{"assets":{{assets}}}""",
        BackgroundImageUrl = background
    };

    var both = Detail("""{"library_hero":[{"url":"hero"}],"library_hero_2x":[{"url":"hero-2x"}]}""");
    var only2x = Detail("""{"library_hero_2x":[{"url":"hero-2x"}]}""");
    var onlyNormal = Detail("""{"library_hero":[{"url":"hero"}]}""");
    var neither = Detail("""{"sgdb_hero":[{"url":"sgdb"}]}""");
    foreach (var page in new[] { "General Detail", "Bypass Detail" })
    {
        Assert(DetailHeroSelector.Select(null, both, game) == "hero", $"{page} did not prefer library_hero");
        Assert(DetailHeroSelector.Select(null, only2x, game) == "hero-2x", $"{page} lost library_hero_2x fallback");
        Assert(DetailHeroSelector.Select(null, onlyNormal, game) == "hero", $"{page} did not select library_hero alone");
        Assert(DetailHeroSelector.Select(null, neither, game) == "sgdb", $"{page} changed later hero fallback");
        Assert(DetailHeroSelector.Select(new NexaPlayCatalogOverride { LibraryHero2x = "override" }, both, game) == "override",
            $"{page} changed NexaPlay override precedence");
    }

    var generalSource = File.ReadAllText(Path.Combine("NexaPlay", "Presentation", "ViewModels", "GameDetailViewModel.cs"));
    var bypassSource = File.ReadAllText(Path.Combine("NexaPlay", "Presentation", "ViewModels", "BypassGameDetailViewModel.cs"));
    Assert(generalSource.Contains("DetailHeroSelector.Select(heroOverride, Detail, Game)", StringComparison.Ordinal),
        "General Detail does not use shared hero selector");
    Assert(bypassSource.Contains("DetailHeroSelector.Select(heroOverride, Detail, Game)", StringComparison.Ordinal),
        "Bypass Detail does not use shared hero selector");
}

static async Task AssertLoadCoordinatorAsync()
{
    var generation = 0L;
    var loads = 0;
    var coordinator = new CatalogLoadCoordinator();
    await coordinator.RunAsync(() => generation, false, () => { }, _ => { loads++; return Task.CompletedTask; });
    await coordinator.RunAsync(() => generation, false, () => { }, _ => { loads++; return Task.CompletedTask; });
    Assert(loads == 1, "unchanged generation did not skip reload");
    coordinator.Invalidate();
    await coordinator.RunAsync(() => generation, false, () => { }, _ => { loads++; return Task.CompletedTask; });
    Assert(loads == 2, "explicit transient-cache invalidation did not force lazy reload");
}

static void AssertProtectionParity()
{
    (bool Base, bool Feed, bool? Override, bool Expected)[] cases =
    {
        (Base: true, Feed: false, Override: (bool?)null, Expected: true),
        (false, false, (bool?)null, false),
        (false, true, (bool?)null, true),
        (true, false, (bool?)false, false),
        (false, false, (bool?)true, true),
        (true, true, (bool?)false, false),
        (false, true, (bool?)true, true)
    };
    foreach (var item in cases)
        Assert(CatalogProtectionResolver.Resolve(item.Base, item.Feed, item.Override) == item.Expected,
            "catalog protection parity case failed");
}

static async Task AssertCatalogAsync()
{
    var root = TempDirectory("catalog");
    try
    {
        var seed = Path.Combine(root, "seed.sqlite");
        var runtime = Path.Combine(root, "runtime", "catalog.sqlite");
        await CreateCatalogFixtureAsync(seed, 2);
        var bootstrap = new CatalogBootstrapService(new NullLogService(), seed, runtime);
        var repository = new SqliteCatalogRepository(bootstrap);
        var eligibilityIds = new[] { 810001, 810002, 810003, 810004, 810005, 810006, 810007 };

        var sameDay = await ReadAllPagesAsync(repository, 20260914, 5);
        var sameDayAgain = await ReadAllPagesAsync(repository, 20260914, 5);
        var nextDay = await ReadAllPagesAsync(repository, 20260915, 5);
        Assert(sameDay.SequenceEqual(sameDayAgain), "same local date ordering changed");
        Assert(!sameDay.SequenceEqual(nextDay), "next local date did not rotate ordering");
        Assert(sameDay.Count == sameDay.Distinct().Count(), "sequential pages contain duplicate AppIDs");

        var games = await repository.GetByAppIdsAsync(sameDay);
        var bands = games.Select(game => PriceBand(game.PriceNormalized)).ToArray();
        Assert(bands.SequenceEqual(bands.Order()), "price bands are not exhausted in descending priority");
        Assert(games.All(game => game.PriceNormalized >= 100_000), "default browse included sub-100K row");
        Assert(games.All(HasBrowseArtwork), "default browse included row without effective library capsule");
        var browseCount = await repository.QueryAsync(new CatalogQuery("", null, null, [], true, 0, 5, 20260914));
        Assert(sameDay.Count == browseCount.TotalCount, "browse count/page eligibility mismatch");
        var firstThreeIds = new List<int>();
        for (var offset = 0; offset < 15; offset += 5)
        {
            var page = await repository.QueryAsync(new CatalogQuery("", null, null, [], true, offset, 5, 20260914));
            Assert(page.Items.Count == 5, $"browse page {offset / 5 + 1} is unexpectedly short");
            firstThreeIds.AddRange(page.Items.Select(game => game.AppId));
        }
        Assert(firstThreeIds.Count == firstThreeIds.Distinct().Count(), "first three browse pages contain duplicate AppIDs");

        var premium = await repository.QueryAsync(new CatalogQuery("", true, null, [], true, 0, 500, 20260914));
        var standard = await repository.QueryAsync(new CatalogQuery("", false, null, [], true, 0, 500, 20260914));
        var denuvo = await repository.QueryAsync(new CatalogQuery("", null, true, [], true, 0, 500, 20260914));
        var nonDenuvo = await repository.QueryAsync(new CatalogQuery("", null, false, [], true, 0, 500, 20260914));
        var action = await repository.QueryAsync(new CatalogQuery("", null, null, ["Action"], true, 0, 500, 20260914));
        var combined = await repository.QueryAsync(new CatalogQuery("", true, true, ["Action"], true, 0, 500, 20260914));
        var standardCombined = await repository.QueryAsync(new CatalogQuery("", false, false, ["RPG"], true, 0, 500, 20260914));
        foreach (var (name, page) in new[]
        {
            ("Premium", premium), ("Standard", standard), ("Denuvo", denuvo), ("Non-Denuvo", nonDenuvo),
            ("Genre Action", action), ("Premium+Denuvo+Action", combined), ("Standard+Non-Denuvo+RPG", standardCombined)
        })
            Assert(page.Items.All(game => game.PriceNormalized >= 100_000 && HasBrowseArtwork(game)),
                $"{name} browse eligibility failed");
        Assert(premium.Items.All(game => game.IsPremium), "Premium filter leaked Standard row");
        Assert(standard.Items.All(game => !game.IsPremium && game.PriceNormalized >= 100_000), "Standard filter/floor failed");
        Assert(denuvo.Items.All(game => game.HasDenuvo), "Denuvo filter failed");
        Assert(nonDenuvo.Items.All(game => !game.HasDenuvo), "Non-Denuvo filter failed");
        Assert(action.Items.All(game => game.Genre?.Contains("Action", StringComparison.Ordinal) == true), "genre filter failed");
        Assert(combined.Items.All(game => game.IsPremium && game.HasDenuvo && game.Genre?.Contains("Action", StringComparison.Ordinal) == true),
            "combined filter failed");
        Assert(standardCombined.Items.All(game => !game.IsPremium && !game.HasDenuvo && game.Genre?.Contains("RPG", StringComparison.Ordinal) == true),
            "Standard+Non-Denuvo+RPG filter failed");

        var browseEligibility = await repository.QueryAsync(new CatalogQuery("", null, null, [], true, 0, 500, 20260914));
        Assert(new[] { 810001, 810002 }.All(appId => browseEligibility.Items.Any(game => game.AppId == appId)),
            "eligible artwork matrix row missing from browse");
        Assert(new[] { 810003, 810004, 810005, 810006, 810007 }.All(appId => browseEligibility.Items.All(game => game.AppId != appId)),
            "ineligible artwork matrix row leaked into browse");

        var exactCheap = await repository.QueryAsync(new CatalogQuery("999", null, null, [], false, 0, 10, 1));
        var normalized = await repository.QueryAsync(new CatalogQuery("special-game", null, null, [], false, 0, 10, 1));
        var filteredSearch = await repository.QueryAsync(new CatalogQuery("special", true, true, ["Action"], false, 0, 10, 1));
        Assert((exactCheap.Items.SingleOrDefault() ?? throw new InvalidOperationException("exact AppID search returned no row")).PriceNormalized < 100_000,
            "exact AppID search incorrectly applied default floor");
        Assert((normalized.Items.SingleOrDefault() ?? throw new InvalidOperationException("normalized search returned no row")).AppId == 350001,
            "normalized name search failed");
        Assert((filteredSearch.Items.SingleOrDefault() ?? throw new InvalidOperationException("filtered search returned no row")).AppId == 350001,
            "search plus filters failed");
        var matrixSearch = await repository.QueryAsync(new CatalogQuery("eligibility", null, null, [], false, 0, 20, 1));
        Assert(matrixSearch.Items.Select(game => game.AppId).ToHashSet().SetEquals(eligibilityIds),
            "name search did not return full browse-eligibility matrix");
        foreach (var appId in eligibilityIds)
        {
            var exact = await repository.QueryAsync(new CatalogQuery(appId.ToString(), null, null, [], false, 0, 10, 1));
            Assert(exact.Items.SingleOrDefault()?.AppId == appId, $"exact AppID search failed for {appId}");
        }
        var freeDenuvoSearch = await repository.QueryAsync(new CatalogQuery("810004", null, true, [], false, 0, 10, 1));
        var noCapsulePremiumSearch = await repository.QueryAsync(new CatalogQuery("810005", true, null, [], false, 0, 10, 1));
        var cheapStandardSearch = await repository.QueryAsync(new CatalogQuery("810003", false, null, [], false, 0, 10, 1));
        Assert(freeDenuvoSearch.Items.SingleOrDefault()?.AppId == 810004, "Search + Denuvo excluded Free row");
        Assert(noCapsulePremiumSearch.Items.SingleOrDefault()?.AppId == 810005, "Search + Premium excluded no-capsule row");
        Assert(cheapStandardSearch.Items.SingleOrDefault()?.AppId == 810003, "Search + Standard incorrectly applied browse floor");
        Assert(standard.Items.All(game => game.AppId != 810003), "Browse + Standard included sub-100K row");
        Assert(premium.Items.All(game => game.AppId != 810005), "Browse + Premium included no-capsule row");

        await repository.ReplaceRuntimeOverridesAsync(new Dictionary<int, CatalogRuntimeOverride>
        {
            [350001] = new(Protection: false),
            [810001] = new(LibraryCapsule: "   "),
            [810005] = new(LibraryCapsule: "https://example.test/override-810005.jpg")
        });
        Assert((await repository.GetByAppIdAsync(350001))?.HasDenuvo == false, "runtime protection override failed");
        Assert((await repository.GetByAppIdAsync(810005))?.LibraryCapsuleUrl == "https://example.test/override-810005.jpg",
            "effective artwork does not match rendered card authority");
        var overrideBrowse = await repository.QueryAsync(new CatalogQuery("", null, null, [], true, 0, 500, 20260914));
        Assert(overrideBrowse.Items.Any(game => game.AppId == 810005), "effective artwork override did not make row browse-eligible");
        Assert(overrideBrowse.Items.All(game => game.AppId != 810001), "empty effective artwork override remained browse-eligible");
        var overridePages = await ReadAllPagesAsync(repository, 20260914, 5);
        Assert(overridePages.Count == overrideBrowse.TotalCount && overridePages.Count == overridePages.Distinct().Count(),
            "override browse count/page parity or uniqueness failed");

        await repository.ApplyPatchAsync(new CatalogPatch(1, 2, 3,
            [new CatalogPatchGame(777, "Patched", "patched", null, "[\"RPG\"]", 250000, "Rp 250.000", true, "premium", "{}")],
            [999]));
        Assert(await repository.GetVersionAsync() == 3 && await repository.GetByAppIdAsync(999) is null &&
            (await repository.GetByAppIdAsync(777))?.Name == "Patched", "catalog patch transaction failed");

        await AssertCatalogUpdaterAsync(root, seed);
    }
    finally
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, true);
    }
}

static async Task<IReadOnlyList<int>> ReadAllPagesAsync(SqliteCatalogRepository repository, int seed, int pageSize)
{
    var first = await repository.QueryAsync(new CatalogQuery("", null, null, [], true, 0, pageSize, seed));
    var ids = new List<int>(first.TotalCount);
    for (var offset = 0; offset < first.TotalCount; offset += pageSize)
    {
        var page = await repository.QueryAsync(new CatalogQuery("", null, null, [], true, offset, pageSize, seed));
        ids.AddRange(page.Items.Select(game => game.AppId));
    }
    return ids;
}

static int PriceBand(int price) => price switch
{
    >= 350_000 => 0,
    >= 250_000 => 1,
    >= 180_000 => 2,
    >= 135_000 => 3,
    _ => 4
};

static bool HasBrowseArtwork(GameEntry game) => !string.IsNullOrWhiteSpace(game.LibraryCapsuleUrl);

static async Task AssertHomeRecentFlowAsync()
{
    var parseMethod = typeof(MetadataService).GetMethod(
        "ParseAppIds",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
        ?? throw new InvalidOperationException("MetadataService.ParseAppIds not found");
    var sourceIds = new[]
    {
        350001, 350002, 350001, 350003, 350004, 350005, 350006, 350007,
        350008, 350009, 350010, 350011, 350012, 250001, 250002
    };
    var parsed = (IReadOnlyList<int>)parseMethod.Invoke(null, [JsonSerializer.Serialize(sourceIds)])!;
    var expected = sourceIds.Distinct().ToArray();
    Assert(parsed.SequenceEqual(expected), "new_fix AppIDs are not ordered-distinct");

    var recentIds = parsed.Take(12).ToArray();
    Assert(recentIds.Length == 12 && recentIds.SequenceEqual(expected.Take(12)), "Home Recent max 12 changed");

    var directory = TempDirectory("home-recent");
    try
    {
        var seed = Path.Combine(directory, "seed.sqlite");
        var runtime = Path.Combine(directory, "runtime", "catalog.sqlite");
        await CreateCatalogFixtureAsync(seed, 1);
        var repository = new SqliteCatalogRepository(new CatalogBootstrapService(new NullLogService(), seed, runtime));
        var rows = await repository.GetByAppIdsAsync(recentIds);
        Assert(rows.Count == 12 && rows.Select(game => game.AppId).ToHashSet().SetEquals(recentIds),
            "Home Recent SQLite base lookup failed");
    }
    finally
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(directory, true);
    }

    var homeSource = await File.ReadAllTextAsync(Path.Combine("NexaPlay", "Presentation", "ViewModels", "HomeViewModel.cs"));
    var mainSource = await File.ReadAllTextAsync(Path.Combine("NexaPlay", "MainWindow.xaml.cs"));
    Assert(homeSource.Contains("appIds.Take(12).ToArray()", StringComparison.Ordinal) &&
           homeSource.Contains("appIds.Take(12).Select", StringComparison.Ordinal),
        "Home Recent bounded batch path changed");
    Assert(CountOccurrences(homeSource, "GetDetailAsync(") == 1,
        "HomeViewModel detail calls are no longer limited to Recent enrichment");
    Assert(mainSource.Contains("DynamicFeedChanges.NewFix", StringComparison.Ordinal) &&
           mainSource.Contains("await _homeViewModel.ReloadRecentAsync();", StringComparison.Ordinal),
        "NewFixChanged does not trigger Home Recent reload");

    foreach (var file in new[] { "GamesViewModel.cs", "LibraryViewModel.cs", "BypassGamesViewModel.cs" })
    {
        var source = await File.ReadAllTextAsync(Path.Combine("NexaPlay", "Presentation", "ViewModels", file));
        Assert(!source.Contains("GetDetailAsync(", StringComparison.Ordinal), $"{file} added a listing detail call");
    }
}

static int CountOccurrences(string source, string value)
{
    var count = 0;
    for (var index = 0; (index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0; index += value.Length)
        count++;
    return count;
}

static async Task CreateCatalogFixtureAsync(string path, int version)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await using var connection = new SqliteConnection($"Data Source={path}");
    await connection.OpenAsync();
    await using (var schema = connection.CreateCommand())
    {
        schema.CommandText = $$"""
            CREATE TABLE catalog_meta(key TEXT PRIMARY KEY, value TEXT NOT NULL);
            INSERT INTO catalog_meta VALUES('catalog_version', '{{version}}'), ('schema_version', '1');
            CREATE TABLE games(
                appid INTEGER PRIMARY KEY, name TEXT NOT NULL, normalized_name TEXT NOT NULL,
                library_capsule TEXT NULL, genre TEXT NOT NULL, price_normalized INTEGER NOT NULL,
                price_display TEXT NOT NULL, protection INTEGER NOT NULL CHECK(protection IN (0,1)),
                tier TEXT NOT NULL CHECK(tier IN ('standard','premium')), pc_requirements TEXT NOT NULL);
            CREATE INDEX idx_games_price ON games(price_normalized);
            """;
        await schema.ExecuteNonQueryAsync();
    }

    var minimums = new[] { 350_000, 250_000, 180_000, 135_000, 100_000 };
    for (var band = 0; band < minimums.Length; band++)
    for (var index = 0; index < 12; index++)
    {
        var appId = minimums[band] + index + 1;
        var name = appId == 350001 ? "Special Game" : $"Band {band} Game {index}";
        var tier = index % 2 == 0 ? "premium" : "standard";
        var protection = index % 3 == 0 ? 1 : 0;
        var genre = index % 2 == 0 ? "[\"Action\"]" : "[\"RPG\"]";
        await InsertGameAsync(connection, appId, name, minimums[band] + index * 100, genre, protection, tier,
            $"https://example.test/{appId}.jpg");
    }
    await InsertGameAsync(connection, 999, "Cheap Searchable", 90_000, "[\"Action\"]", 0, "standard", "https://example.test/999.jpg");
    await InsertGameAsync(connection, 810001, "Eligibility A", 500_000, "[\"Action\"]", 1, "premium", "https://example.test/810001.jpg");
    await InsertGameAsync(connection, 810002, "Eligibility B", 120_000, "[\"RPG\"]", 0, "standard", "https://example.test/810002.jpg");
    await InsertGameAsync(connection, 810003, "Eligibility C", 75_000, "[\"RPG\"]", 0, "standard", "https://example.test/810003.jpg");
    await InsertGameAsync(connection, 810004, "Eligibility D", 0, "[\"Action\"]", 1, "standard", "https://example.test/810004.jpg");
    await InsertGameAsync(connection, 810005, "Eligibility E", 500_000, "[\"Action\"]", 0, "premium", null);
    await InsertGameAsync(connection, 810006, "Eligibility F", 120_000, "[\"RPG\"]", 0, "standard", "   ");
    await InsertGameAsync(connection, 810007, "Eligibility G", 0, "[\"RPG\"]", 0, "standard", null);
}

static async Task InsertGameAsync(
    SqliteConnection connection, int appId, string name, int price, string genre, int protection, string tier, string? libraryCapsule)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        INSERT INTO games VALUES(@appid,@name,@normalized,@capsule,@genre,@price,@display,@protection,@tier,'{}');
        """;
    command.Parameters.AddWithValue("@appid", appId);
    command.Parameters.AddWithValue("@name", name);
    command.Parameters.AddWithValue("@normalized", name.NormalizeForSearch());
    command.Parameters.AddWithValue("@capsule", (object?)libraryCapsule ?? DBNull.Value);
    command.Parameters.AddWithValue("@genre", genre);
    command.Parameters.AddWithValue("@price", price);
    command.Parameters.AddWithValue("@display", $"Rp {price:N0}");
    command.Parameters.AddWithValue("@protection", protection);
    command.Parameters.AddWithValue("@tier", tier);
    await command.ExecuteNonQueryAsync();
}

static async Task AssertCatalogUpdaterAsync(string root, string seed)
{
    var patch = Encoding.UTF8.GetBytes("""
        {"schema_version":1,"from_version":2,"to_version":3,"upsert":[],"delete":[]}
        """);
    var hash = Convert.ToHexString(SHA256.HashData(patch)).ToLowerInvariant();
    var manifest = JsonSerializer.Serialize(new
    {
        schema_version = 1,
        latest_version = 3,
        minimum_supported_version = 2,
        patches = new[] { new { version = 3, file = "patches/3.json", size = patch.LongLength, sha256 = hash } }
    });
    var handler = new RouteHandler(request => request.RequestUri!.AbsolutePath.EndsWith("catalog_manifest.json")
        ? JsonResponse(manifest)
        : BytesResponse(patch));
    using var http = new HttpClient(handler);
    var bootstrap = new CatalogBootstrapService(new NullLogService(), seed, Path.Combine(root, "update", "catalog.sqlite"));
    var repository = new SqliteCatalogRepository(bootstrap);
    var updater = new CatalogUpdateService(bootstrap, repository, new NullLogService(), http,
        new Uri("https://example.test/catalog/catalog_manifest.json"));
    var result = await updater.CheckAndApplyPatchesAsync();
    Assert(result.Status == CatalogUpdateStatus.Applied && await repository.GetVersionAsync() == 3,
        "catalog updater did not apply sequential patch");
    Assert(handler.Requests.All(url => !url.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase)),
        "catalog updater requested remote full SQLite");
}

static async Task AssertCachePoliciesAsync()
{
    var cover = TempDirectory("cover-cap");
    var detail = TempDirectory("detail-cap");
    try
    {
        WriteSizedFiles(cover, 6, 10);
        CoverImageCacheService.TrimDirectory(cover, 50, 30);
        Assert(Directory.EnumerateFiles(cover).Sum(path => new FileInfo(path).Length) <= 30,
            "cover cache did not trim to lower target");

        WriteSizedFiles(detail, 7, 10, ".json");
        var expired = Directory.EnumerateFiles(detail).First();
        File.SetLastWriteTimeUtc(expired, DateTime.UtcNow.AddDays(-8));
        SteamStoreService.MaintainDirectory(detail, DateTime.UtcNow, TimeSpan.FromDays(7), 50, 30);
        Assert(!File.Exists(expired), "expired detail cache survived maintenance");
        Assert(Directory.EnumerateFiles(detail).Sum(path => new FileInfo(path).Length) <= 30,
            "detail cache did not trim below cap");
    }
    finally
    {
        Directory.Delete(cover, true);
        Directory.Delete(detail, true);
    }
    await Task.CompletedTask;
}

static async Task AssertDetailCacheAsync()
{
    var directory = TempDirectory("detail");
    try
    {
        var handler = new DetailHandler();
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var service = new SteamStoreService(new NullLogService(), new NullOverrideService(), http, directory);

        var first = service.GetDetailAsync(42);
        await handler.WaitStartedAsync(42);
        var second = service.GetDetailAsync(42);
        handler.Release(42);
        await Task.WhenAll(first, second);
        Assert(handler.Count(42) == 1, "same-AppID detail requests were not deduplicated");

        var racing = service.GetDetailAsync(43);
        await handler.WaitStartedAsync(43);
        await service.ClearCacheAsync();
        handler.Release(43);
        await racing;
        Assert(!File.Exists(Path.Combine(directory, "43.json")), "pre-clear detail request repopulated cache");

        await File.WriteAllTextAsync(Path.Combine(directory, "44.json"), "{");
        var repaired = await service.GetDetailAsync(44);
        Assert(repaired is not null && JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "44.json"))) is not null,
            "fresh corrupt detail cache was not repaired");

        var stalePath = Path.Combine(directory, "45.json");
        await File.WriteAllTextAsync(stalePath, DetailHandler.ValidJson);
        File.SetLastWriteTimeUtc(stalePath, DateTime.UtcNow.AddDays(-8));
        var stale = await service.GetDetailAsync(45);
        Assert(stale is { IsFromCache: true }, "transient detail failure did not use stale known-good cache");
    }
    finally
    {
        Directory.Delete(directory, true);
    }
}

static async Task AssertCoverEpochAsync()
{
    var directory = TempDirectory("cover-epoch");
    try
    {
        var handler = new DelayedBytesHandler([1, 2, 3]);
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var service = new CoverImageCacheService(new NullLogService(), http, directory);
        var request = service.GetCachedOrFetchAsync(1, "https://example.test/cover.jpg");
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.ClearCacheAsync();
        handler.Release.SetResult();
        await request;
        Assert(!Directory.EnumerateFiles(directory).Any(), "pre-clear cover request repopulated cache");
    }
    finally
    {
        Directory.Delete(directory, true);
    }
}

static async Task AssertDynamicFeedsAsync()
{
    await AssertMissingFeedDownloadAsync();
    await AssertMissingFeedUnexpected304Async();
    await AssertExistingFeed304Async();
    await AssertMissingFeedFailureRetryAsync();
    await AssertInvalidFeedPreservesKnownGoodAsync();
}

static async Task AssertMissingFeedDownloadAsync()
{
    var directory = TempDirectory("feed-missing-200");
    try
    {
        await WriteFeedBodiesAsync(directory, includeNewFix: false);
        var handler = new RouteHandler(request => request.RequestUri!.AbsoluteUri == AppConstants.NewFixGamesUrl
            ? JsonResponse("[4]")
            : new HttpResponseMessage(HttpStatusCode.NotModified));
        using var http = new HttpClient(handler);
        var service = new DynamicFeedRefreshService(new NullLogService(), http, directory);
        var result = await service.RefreshAsync(force: false);
        var newFixPath = service.GetLocalPath(DynamicFeedKind.NewFix);
        Assert(result.Changes == DynamicFeedChanges.NewFix && result.Failed == 0, "missing new_fix + 200 result is wrong");
        Assert(File.Exists(newFixPath) && (await File.ReadAllTextAsync(newFixPath)).Contains("4", StringComparison.Ordinal),
            "missing new_fix body was not physically published");
        Assert(!handler.RequestDetails.Single(item => item.Url == AppConstants.NewFixGamesUrl).HasIfNoneMatch,
            "missing new_fix incorrectly sent validator");
        Assert(!Directory.EnumerateFiles(directory, "*.tmp", SearchOption.AllDirectories).Any(),
            "missing new_fix publish left temp files");
    }
    finally
    {
        Directory.Delete(directory, true);
    }
}

static async Task AssertMissingFeedUnexpected304Async()
{
    var directory = TempDirectory("feed-missing-304");
    try
    {
        await WriteFeedBodiesAsync(directory, includeNewFix: false);
        var etagDirectory = Path.Combine(directory, ".etags");
        Directory.CreateDirectory(etagDirectory);
        await File.WriteAllTextAsync(Path.Combine(etagDirectory, $"{AppConstants.NewFixGamesCacheFileName}.etag"), "\"stale\"");
        var attempts = 0;
        var handler = new RouteHandler(request => request.RequestUri!.AbsoluteUri == AppConstants.NewFixGamesUrl
            ? Interlocked.Increment(ref attempts) == 1
                ? new HttpResponseMessage(HttpStatusCode.NotModified)
                : JsonResponse("[5]")
            : new HttpResponseMessage(HttpStatusCode.NotModified));
        using var http = new HttpClient(handler);
        var service = new DynamicFeedRefreshService(new NullLogService(), http, directory);
        var result = await service.RefreshAsync(force: false);
        var requests = handler.RequestDetails.Where(item => item.Url == AppConstants.NewFixGamesUrl).ToArray();
        Assert(result.Changes == DynamicFeedChanges.NewFix && attempts == 2, "missing-body 304 was not retried unconditionally");
        Assert(requests.Length == 2 && requests.All(item => !item.HasIfNoneMatch), "missing-body retry used stale validator");
        Assert(File.Exists(service.GetLocalPath(DynamicFeedKind.NewFix)), "missing-body 304 recovery did not publish file");
    }
    finally
    {
        Directory.Delete(directory, true);
    }
}

static async Task AssertExistingFeed304Async()
{
    var directory = TempDirectory("feed-existing-304");
    try
    {
        await WriteFeedBodiesAsync(directory, includeNewFix: true);
        var etagDirectory = Path.Combine(directory, ".etags");
        Directory.CreateDirectory(etagDirectory);
        await File.WriteAllTextAsync(Path.Combine(etagDirectory, $"{AppConstants.NewFixGamesCacheFileName}.etag"), "\"current\"");
        var newFixPath = Path.Combine(directory, AppConstants.NewFixGamesCacheFileName);
        var before = File.GetLastWriteTimeUtc(newFixPath);
        var handler = new RouteHandler(_ => new HttpResponseMessage(HttpStatusCode.NotModified));
        using var http = new HttpClient(handler);
        var service = new DynamicFeedRefreshService(new NullLogService(), http, directory);
        var result = await service.RefreshAsync(force: false);
        Assert(result.Changes == DynamicFeedChanges.None && result.NotModified == 4 && result.Failed == 0,
            "existing feed 304 result is wrong");
        Assert(handler.RequestDetails.Single(item => item.Url == AppConstants.NewFixGamesUrl).HasIfNoneMatch,
            "existing new_fix omitted validator");
        Assert(File.GetLastWriteTimeUtc(newFixPath) == before, "existing 304 rewrote new_fix body");

        var requestCount = handler.Requests.Count;
        var suppressed = await service.RefreshAsync(force: false);
        Assert(suppressed.Suppressed && handler.Requests.Count == requestCount,
            "successful same-session feed check was not deduplicated");
    }
    finally
    {
        Directory.Delete(directory, true);
    }
}

static async Task AssertMissingFeedFailureRetryAsync()
{
    var directory = TempDirectory("feed-missing-retry");
    try
    {
        await WriteFeedBodiesAsync(directory, includeNewFix: false);
        var attempts = 0;
        var handler = new RouteHandler(request => request.RequestUri!.AbsoluteUri == AppConstants.NewFixGamesUrl
            ? Interlocked.Increment(ref attempts) == 1
                ? throw new HttpRequestException("fixture failure")
                : JsonResponse("[6]")
            : new HttpResponseMessage(HttpStatusCode.NotModified));
        using var http = new HttpClient(handler);
        var service = new DynamicFeedRefreshService(new NullLogService(), http, directory);
        var failed = await service.RefreshAsync(force: false);
        Assert(failed.Failed == 1 && !File.Exists(service.GetLocalPath(DynamicFeedKind.NewFix)),
            "missing feed failure result is wrong");
        var retried = await service.RefreshAsync(force: false);
        Assert(!retried.Suppressed && retried.Changes == DynamicFeedChanges.NewFix && attempts == 2,
            "failed missing feed was permanently session-suppressed");
    }
    finally
    {
        Directory.Delete(directory, true);
    }
}

static async Task AssertInvalidFeedPreservesKnownGoodAsync()
{
    var directory = TempDirectory("feed-invalid");
    try
    {
        await WriteFeedBodiesAsync(directory, includeNewFix: true);
        var newFixPath = Path.Combine(directory, AppConstants.NewFixGamesCacheFileName);
        var knownGood = await File.ReadAllTextAsync(newFixPath);
        var handler = new RouteHandler(request => request.RequestUri!.AbsoluteUri == AppConstants.NewFixGamesUrl
            ? JsonResponse("{\"unexpected\":123}")
            : new HttpResponseMessage(HttpStatusCode.NotModified));
        using var http = new HttpClient(handler);
        var service = new DynamicFeedRefreshService(new NullLogService(), http, directory);
        var result = await service.RefreshAsync(force: true);
        Assert(result.Failed == 1 && await File.ReadAllTextAsync(newFixPath) == knownGood,
            "invalid new_fix replaced known-good body");
    }
    finally
    {
        Directory.Delete(directory, true);
    }
}

static async Task WriteFeedBodiesAsync(string directory, bool includeNewFix)
{
    await File.WriteAllTextAsync(Path.Combine(directory, "appid_populer.json"), "[1,2]");
    await File.WriteAllTextAsync(Path.Combine(directory, AppConstants.BypassGamesCacheFileName), "{\"games\":[{\"appid\":1}]}");
    await File.WriteAllTextAsync(Path.Combine(directory, AppConstants.SteamGamesCacheFileName), "[{\"appid\":3}]");
    if (includeNewFix)
        await File.WriteAllTextAsync(Path.Combine(directory, AppConstants.NewFixGamesCacheFileName), "{\"games\":[{\"appid\":2}]}");
}

static void AssertBundledSeed()
{
    var seed = Path.GetFullPath(Path.Combine("NexaPlay", "data", "catalog", "catalog_seed.sqlite"));
    Assert(File.Exists(seed), "bundled catalog seed missing");
    using var connection = new SqliteConnection($"Data Source={seed};Mode=ReadOnly");
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM games;";
    Assert(Convert.ToInt32(command.ExecuteScalar()) == 163262, "bundled catalog seed row count mismatch");
}

static async Task BenchmarkBundledCatalogAsync()
{
    var root = TempDirectory("benchmark");
    try
    {
        var seed = Path.GetFullPath(Path.Combine("NexaPlay", "data", "catalog", "catalog_seed.sqlite"));
        var runtime = Path.Combine(root, "catalog.sqlite");
        File.Copy(seed, runtime);
        var repository = new SqliteCatalogRepository(new CatalogBootstrapService(new NullLogService(), seed, runtime));
        var scenarios = new (string Name, CatalogQuery Query)[]
        {
            ("default page 1", new("", null, null, [], true, 0, 60, 20260914)),
            ("default page 2", new("", null, null, [], true, 60, 60, 20260914)),
            ("Premium", new("", true, null, [], true, 0, 60, 20260914)),
            ("Standard", new("", false, null, [], true, 0, 60, 20260914)),
            ("Denuvo", new("", null, true, [], true, 0, 60, 20260914)),
            ("Non-Denuvo", new("", null, false, [], true, 0, 60, 20260914)),
            ("Genre Action", new("", null, null, ["Action"], true, 0, 60, 20260914)),
            ("Premium+Denuvo+Action", new("", true, true, ["Action"], true, 0, 60, 20260914)),
            ("exact AppID 570", new("570", null, null, [], false, 0, 60, 1)),
            ("normalized counter strike", new("counter strike", null, null, [], false, 0, 60, 1))
        };

        foreach (var scenario in scenarios)
        {
            await repository.QueryAsync(scenario.Query);
            var samples = new List<double>(5);
            CatalogPage? page = null;
            for (var run = 0; run < 5; run++)
            {
                var stopwatch = Stopwatch.StartNew();
                page = await repository.QueryAsync(scenario.Query);
                stopwatch.Stop();
                samples.Add(stopwatch.Elapsed.TotalMilliseconds);
            }
            samples.Sort();
            Console.WriteLine(
                $"BENCH {scenario.Name}: count={page!.TotalCount} rows={page.Items.Count} median={samples[2]:F3} ms min={samples[0]:F3} max={samples[^1]:F3}");
        }

        await PrintQueryPlanAsync(runtime, "daily price block", """
            WITH effective AS (
                SELECT g.appid, COALESCE(o.library_capsule, g.library_capsule) AS library_capsule,
                       COALESCE(o.price_normalized, g.price_normalized) AS price_normalized
                FROM games g INDEXED BY idx_games_price
                LEFT JOIN catalog_runtime_overrides o ON o.appid = g.appid)
            SELECT appid, price_normalized, COUNT(*) OVER() FROM effective
            WHERE price_normalized >= 100000 AND NULLIF(TRIM(library_capsule), '') IS NOT NULL
            ORDER BY price_normalized DESC, appid ASC LIMIT 180;
            """);
        await PrintQueryPlanAsync(runtime, "Premium browse", """
            WITH effective AS (
                SELECT g.appid, COALESCE(o.library_capsule, g.library_capsule) AS library_capsule,
                       COALESCE(o.price_normalized, g.price_normalized) AS price_normalized,
                       COALESCE(o.tier, g.tier) AS tier
                FROM games g INDEXED BY idx_games_price
                LEFT JOIN catalog_runtime_overrides o ON o.appid = g.appid)
            SELECT appid, price_normalized, COUNT(*) OVER() FROM effective
            WHERE price_normalized >= 100000 AND NULLIF(TRIM(library_capsule), '') IS NOT NULL AND tier = 'premium'
            ORDER BY price_normalized DESC, appid ASC LIMIT 180;
            """);
        await PrintQueryPlanAsync(runtime, "Genre browse", """
            WITH effective AS (
                SELECT g.appid, COALESCE(o.library_capsule, g.library_capsule) AS library_capsule,
                       COALESCE(o.genre, g.genre) AS genre,
                       COALESCE(o.price_normalized, g.price_normalized) AS price_normalized
                FROM games g INDEXED BY idx_games_price
                LEFT JOIN catalog_runtime_overrides o ON o.appid = g.appid)
            SELECT appid, price_normalized, COUNT(*) OVER() FROM effective
            WHERE price_normalized >= 100000 AND NULLIF(TRIM(library_capsule), '') IS NOT NULL
              AND genre LIKE '%"Action"%'
            ORDER BY price_normalized DESC, appid ASC LIMIT 180;
            """);
        await PrintQueryPlanAsync(runtime, "exact AppID", """
            WITH effective AS (
                SELECT g.appid FROM games g LEFT JOIN catalog_runtime_overrides o ON o.appid = g.appid)
            SELECT appid FROM effective WHERE appid = 570 LIMIT 60;
            """);
        await PrintQueryPlanAsync(runtime, "normalized substring", """
            WITH effective AS (
                SELECT g.appid, COALESCE(o.normalized_name, g.normalized_name) AS normalized_name
                FROM games g LEFT JOIN catalog_runtime_overrides o ON o.appid = g.appid)
            SELECT appid FROM effective WHERE normalized_name LIKE '%counter strike%'
            ORDER BY normalized_name, appid LIMIT 60;
            """);
    }
    finally
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, true);
    }
}

static async Task PrintQueryPlanAsync(string path, string name, string sql)
{
    await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = $"EXPLAIN QUERY PLAN {sql}";
    await using var reader = await command.ExecuteReaderAsync();
    var details = new List<string>();
    while (await reader.ReadAsync())
        details.Add(reader.GetString(3));
    Console.WriteLine($"PLAN {name}: {string.Join(" | ", details)}");
}

static void WriteSizedFiles(string directory, int count, int bytes, string extension = ".img")
{
    for (var index = 0; index < count; index++)
    {
        var path = Path.Combine(directory, $"{index}{extension}");
        File.WriteAllBytes(path, new byte[bytes]);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-count + index));
    }
}

static string TempDirectory(string suffix)
{
    var path = Path.Combine(Path.GetTempPath(), $"nexaplay-selfcheck-{suffix}-{Guid.NewGuid():N}");
    Directory.CreateDirectory(path);
    return path;
}

static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
{
    Content = new StringContent(json, Encoding.UTF8, "application/json")
};

static HttpResponseMessage BytesResponse(byte[] bytes) => new(HttpStatusCode.OK)
{
    Content = new ByteArrayContent(bytes)
};

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

sealed class RouteHandler(Func<HttpRequestMessage, HttpResponseMessage> route) : HttpMessageHandler
{
    public List<string> Requests { get; } = [];
    public List<RequestSnapshot> RequestDetails { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (Requests)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
            RequestDetails.Add(new RequestSnapshot(
                request.RequestUri.AbsoluteUri,
                request.Headers.Contains("If-None-Match")));
        }
        var response = route(request);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}

sealed record RequestSnapshot(string Url, bool HasIfNoneMatch);

sealed class DetailHandler : HttpMessageHandler
{
    public const string ValidJson = "{\"store_data\":{\"short_description\":\"ok\"},\"assets\":{}}";
    private readonly Dictionary<int, int> _counts = [];
    private readonly Dictionary<int, TaskCompletionSource> _started = [];
    private readonly Dictionary<int, TaskCompletionSource> _release = [];

    public int Count(int appId) => _counts.GetValueOrDefault(appId);
    public Task WaitStartedAsync(int appId) => Get(_started, appId).Task.WaitAsync(TimeSpan.FromSeconds(2));
    public void Release(int appId) => Get(_release, appId).TrySetResult();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var appId = int.Parse(Path.GetFileNameWithoutExtension(request.RequestUri!.AbsolutePath));
        _counts[appId] = Count(appId) + 1;
        if (appId is 42 or 43)
        {
            Get(_started, appId).TrySetResult();
            await Get(_release, appId).Task.WaitAsync(cancellationToken);
        }
        return appId == 45
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { RequestMessage = request }
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(ValidJson, Encoding.UTF8, "application/json")
            };
    }

    private static TaskCompletionSource Get(Dictionary<int, TaskCompletionSource> map, int appId)
    {
        if (!map.TryGetValue(appId, out var source))
            map[appId] = source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        return source;
    }
}

sealed class DelayedBytesHandler(byte[] bytes) : HttpMessageHandler
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Started.TrySetResult();
        await Release.Task.WaitAsync(cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new ByteArrayContent(bytes) };
    }
}

sealed class NullOverrideService : INexaPlayOverrideService
{
    public Task<NexaPlayCatalogOverride?> GetCatalogOverrideAsync(int appId, CancellationToken ct = default) => Task.FromResult<NexaPlayCatalogOverride?>(null);
    public Task<NexaPlayDetailOverride?> GetDetailOverrideAsync(int appId, CancellationToken ct = default) => Task.FromResult<NexaPlayDetailOverride?>(null);
    public Task<IReadOnlySet<int>> GetOverriddenAppIdsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());
    public Task<IReadOnlyDictionary<int, NexaPlayCatalogOverride>> GetCachedCatalogOverridesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<int, NexaPlayCatalogOverride>>(new Dictionary<int, NexaPlayCatalogOverride>());
    public Task RefreshAsync(CancellationToken ct = default) => Task.CompletedTask;
}

sealed class NullLogService : IAppLogService
{
    public event EventHandler<string>? LogAppended { add { } remove { } }
    public void Log(string message) { }
    public void Log(string category, string message) { }
    public IReadOnlyList<string> GetRecentLogs(int count = 200) => [];
    public Task<string> GetFullLogAsync() => Task.FromResult(string.Empty);
    public void Clear() { }
}
