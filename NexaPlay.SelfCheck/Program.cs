using NexaPlay.Core.Helpers;
using NexaPlay.Infrastructure.Services;
using System.Text.Json;

static void AssertEqual(string? expected, string? actual, string scenario)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
        throw new InvalidOperationException($"{scenario}: expected '{expected ?? "<null>"}', got '{actual ?? "<null>"}'.");
}

AssertEqual("override", ListingCoverPriority.Select("override", "index", "runtime", "r2", "header"), "override wins");
AssertEqual("index", ListingCoverPriority.Select(" ", "index", "runtime", "r2", "header"), "index wins");
AssertEqual("runtime", ListingCoverPriority.Select(null, null, "runtime", "r2", "header"), "runtime wins");
AssertEqual("r2", ListingCoverPriority.Select(null, null, null, "r2", "header"), "R2 precedes header");
AssertEqual("header", ListingCoverPriority.Select(null, null, null, null, "header"), "header is final image fallback");
AssertEqual(null, ListingCoverPriority.Select(null, "", " ", null, ""), "empty inputs yield no content");

var before = GC.GetAllocatedBytesForCurrentThread();
for (var i = 0; i < 1_000; i++)
    ListingCoverPriority.Select(null, "index", "runtime", "r2", "header");
var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
if (allocated != 0)
    throw new InvalidOperationException($"priority selection allocated {allocated} bytes");

var refreshState = new CatalogRefreshState();
if (refreshState.Generation != 0)
    throw new InvalidOperationException($"generation must start at zero, got {refreshState.Generation}");
if (refreshState.Advance() != 1 || refreshState.Advance() != 2 || refreshState.Generation != 2)
    throw new InvalidOperationException("generation must advance monotonically");

var sourceDirectory = Path.Combine(Path.GetTempPath(), $"nexaplay-selfcheck-{Guid.NewGuid():N}");
Directory.CreateDirectory(sourceDirectory);
try
{
    var sourcePath = Path.Combine(sourceDirectory, "override_data.json");
    File.WriteAllText(sourcePath, "{}");
    File.SetLastWriteTimeUtc(sourcePath, new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc));
    var beforeInfo = new FileInfo(sourcePath);
    var revisionBefore = CatalogCacheStamp.CreateRevision(
        new CatalogSourceStamp(beforeInfo.Name, beforeInfo.Length, beforeInfo.LastWriteTimeUtc.Ticks));
    File.SetLastWriteTimeUtc(sourcePath, new DateTime(2026, 8, 17, 0, 0, 2, DateTimeKind.Utc));
    var afterInfo = new FileInfo(sourcePath);
    afterInfo.Refresh();
    var revisionAfter = CatalogCacheStamp.CreateRevision(
        new CatalogSourceStamp(afterInfo.Name, afterInfo.Length, afterInfo.LastWriteTimeUtc.Ticks));
    if (revisionBefore == revisionAfter)
        throw new InvalidOperationException("source revision must change when relevant file metadata changes");

    const int schema = 1;
    var current = JsonSerializer.Deserialize<CatalogCacheEnvelope<int[]>>(
        $$"""{"schema":{{schema}},"sourceRevision":"{{revisionAfter}}","items":[]}""");
    var oldSchema = JsonSerializer.Deserialize<CatalogCacheEnvelope<int[]>>(
        $$"""{"schema":0,"sourceRevision":"{{revisionAfter}}","items":[]}""");
    var wrongRevision = JsonSerializer.Deserialize<CatalogCacheEnvelope<int[]>>(
        $$"""{"schema":{{schema}},"sourceRevision":"other","items":[]}""");
    CatalogCacheEnvelope<int[]>? legacyArray = null;
    try
    {
        legacyArray = JsonSerializer.Deserialize<CatalogCacheEnvelope<int[]>>("[]");
    }
    catch (JsonException)
    {
    }

    if (!CatalogCacheStamp.IsCurrent(current, schema, revisionAfter) ||
        CatalogCacheStamp.IsCurrent(oldSchema, schema, revisionAfter) ||
        CatalogCacheStamp.IsCurrent(wrongRevision, schema, revisionAfter) ||
        CatalogCacheStamp.IsCurrent(legacyArray, schema, revisionAfter))
    {
        throw new InvalidOperationException("cache envelope validation accepted stale or legacy data");
    }
}
finally
{
    Directory.Delete(sourceDirectory, recursive: true);
}

var publishDirectory = Path.Combine(Path.GetTempPath(), $"nexaplay-cover-publish-{Guid.NewGuid():N}");
Directory.CreateDirectory(publishDirectory);
try
{
    var activePath = Path.Combine(publishDirectory, "library_capsule.json");
    var candidatePath = Path.Combine(publishDirectory, "candidate.tmp");
    const string validIndex = "{\"1\":{\"library_capsule\":\"https://example.test/valid.jpg\"}}";
    File.WriteAllText(activePath, validIndex);
    File.WriteAllText(candidatePath, "<html>upstream error</html>");

    var rejected = false;
    try
    {
        await GameCoverIndexFile.PublishValidatedAsync(candidatePath, activePath, isGzip: false);
    }
    catch (JsonException)
    {
        rejected = true;
    }

    if (!rejected || File.ReadAllText(activePath) != validIndex)
        throw new InvalidOperationException("corrupt cover-index candidate replaced the valid active source");
}
finally
{
    Directory.Delete(publishDirectory, recursive: true);
}

var missingCoverDirectory = Path.Combine(Path.GetTempPath(), $"nexaplay-cover-missing-{Guid.NewGuid():N}");
Directory.CreateDirectory(missingCoverDirectory);
try
{
    using var unavailableHttp = new HttpClient(new StatusCodeHandler(System.Net.HttpStatusCode.TooManyRequests));
    var optionalCoverIndex = new GameCoverIndexService(
        new NullLogService(),
        unavailableHttp,
        missingCoverDirectory);

    await optionalCoverIndex.WarmupAsync();
    if (await optionalCoverIndex.GetLibraryCapsuleAsync(3751950) is not null)
        throw new InvalidOperationException("missing cover index must fall through to later cover sources");

    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    var cancellationPropagated = false;
    try
    {
        await new GameCoverIndexService(
            new NullLogService(),
            unavailableHttp,
            missingCoverDirectory).WarmupAsync(canceled.Token);
    }
    catch (OperationCanceledException)
    {
        cancellationPropagated = true;
    }

    if (!cancellationPropagated)
        throw new InvalidOperationException("cover-index fallback swallowed caller cancellation");
}
finally
{
    Directory.Delete(missingCoverDirectory, recursive: true);
}

var coordinator = new CatalogLoadCoordinator();
var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var capturedGenerations = new List<long>();
var activeLoads = 0;
var maxActiveLoads = 0;
var invalidations = 0;

var firstLoad = coordinator.RunAsync(
    () => refreshState.Generation,
    forceReload: false,
    () => Interlocked.Increment(ref invalidations),
    async generation =>
    {
        capturedGenerations.Add(generation);
        maxActiveLoads = Math.Max(maxActiveLoads, Interlocked.Increment(ref activeLoads));
        firstEntered.SetResult();
        await releaseFirst.Task;
        Interlocked.Decrement(ref activeLoads);
    });
await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
refreshState.Advance();
var secondLoad = coordinator.RunAsync(
    () => refreshState.Generation,
    forceReload: true,
    () => Interlocked.Increment(ref invalidations),
    generation =>
    {
        capturedGenerations.Add(generation);
        maxActiveLoads = Math.Max(maxActiveLoads, Interlocked.Increment(ref activeLoads));
        secondEntered.SetResult();
        Interlocked.Decrement(ref activeLoads);
        return Task.CompletedTask;
    });

if (await Task.WhenAny(secondEntered.Task, Task.Delay(100)) == secondEntered.Task)
    throw new InvalidOperationException("forced reload overlapped the prior catalog load");

releaseFirst.SetResult();
await Task.WhenAll(firstLoad, secondLoad).WaitAsync(TimeSpan.FromSeconds(2));
if (capturedGenerations.Count != 2 || capturedGenerations[0] != 2 || capturedGenerations[1] != 3 ||
    coordinator.LoadedGeneration != 3 || maxActiveLoads != 1 || invalidations != 2)
{
    throw new InvalidOperationException("forced reload did not serialize a distinct load with its captured generation");
}

var unchangedLoadRan = false;
await coordinator.RunAsync(
    () => refreshState.Generation,
    forceReload: false,
    () => Interlocked.Increment(ref invalidations),
    _ =>
    {
        unchangedLoadRan = true;
        return Task.CompletedTask;
    });
if (unchangedLoadRan || invalidations != 2)
    throw new InvalidOperationException("unchanged catalog generation must skip invalidation and load");

const string newGameMetadata = """
{
  "name": "New Test Game",
  "price_normalized": 140000,
  "assets": {
    "header": [{"url":"https://example.test/header.jpg"}],
    "library_capsule": [{"url":"https://example.test/library.jpg"}],
    "icon": [{"url":"https://example.test/icon.jpg"}],
    "library_hero_2x": [{"url":"https://example.test/hero.jpg"}],
    "background_raw": [{"url":"https://example.test/background.jpg"}]
  },
  "store_data": {
    "developers": ["Developer A", "Developer B"],
    "publishers": ["Publisher A"],
    "genres": [{"id":"1","description":"Action"},{"id":"25","description":"Adventure"}],
    "short_description": "Short description",
    "release_date": {"date":"Aug 17, 2026"},
    "price_overview": {"final":6999,"final_formatted":"$69.99"}
  }
}
""";

var normalizedIds = NewGamesCatalog.ParseAppIds("[3768760,3751950,3768760]");
if (normalizedIds is null || !normalizedIds.SequenceEqual([3751950, 3768760]))
    throw new InvalidOperationException("new-games AppID validation must deduplicate and sort");
if (NewGamesCatalog.ParseAppIds("[1,\"2\"]") is not null ||
    NewGamesCatalog.ParseAppIds("[1,0]") is not null ||
    NewGamesCatalog.ParseAppIds("[1,-2]") is not null ||
    NewGamesCatalog.ParseAppIds("{}") is not null)
{
    throw new InvalidOperationException("new-games AppID validation accepted an invalid candidate");
}

var parsedEntry = NewGamesCatalog.ParseMetadata(3751950, newGameMetadata);
if (parsedEntry is null || parsedEntry.AppId != 3751950 || parsedEntry.Name != "New Test Game" ||
    parsedEntry.Developer != "Developer A" || parsedEntry.Publisher != "Publisher A" ||
    parsedEntry.Genre != "Action, Adventure" || parsedEntry.PriceDisplay != "$69.99" ||
    parsedEntry.PriceNormalized != 140000 || parsedEntry.LibraryCapsuleUrl != "https://example.test/library.jpg")
{
    throw new InvalidOperationException("R2 lightweight catalog mapping is incomplete");
}
if (NewGamesCatalog.ParseMetadata(1, "{}") is not null ||
    NewGamesCatalog.ParseMetadata(0, newGameMetadata) is not null)
{
    throw new InvalidOperationException("invalid R2 metadata produced a catalog entry");
}

const string centsOnlyMetadata = """
{"name":"Cents Only","store_data":{"price_overview":{"final":6999,"final_formatted":"$69.99"}}}
""";
if (NewGamesCatalog.ParseMetadata(2, centsOnlyMetadata)?.PriceNormalized != 0)
    throw new InvalidOperationException("R2 currency cents must not become normalized rupiah");

const string blankFirstAssetMetadata = """
{"name":"Asset Fallback","assets":{"header":[{"url":" "},{"url":"https://example.test/fallback.jpg"}]}}
""";
if (NewGamesCatalog.ParseMetadata(3, blankFirstAssetMetadata)?.HeaderImageUrl != "https://example.test/fallback.jpg")
    throw new InvalidOperationException("asset mapping must select the first nonblank URL");

var cached = new NexaPlay.Core.Models.NewGamesCatalogEntry { AppId = 10, Name = "Cached" };
var fetched = new NexaPlay.Core.Models.NewGamesCatalogEntry { AppId = 20, Name = "Fetched" };
var requested = new[] { 10, 20, 30 };
var primary = new HashSet<int> { 30 };
var cachedById = new Dictionary<int, NexaPlay.Core.Models.NewGamesCatalogEntry> { [10] = cached };

var fetchIds = NewGamesCatalog.SelectFetchAppIds(requested, primary, cachedById);
if (!fetchIds.SequenceEqual([20]))
    throw new InvalidOperationException("primary/cached AppIDs must not be fetched from R2");

var composed = NewGamesCatalog.ComposeSnapshot(requested, primary, [cached, fetched]);
if (!composed.Select(x => x.AppId).SequenceEqual([10, 20]))
    throw new InvalidOperationException("snapshot composition violated additive precedence");

var removed = NewGamesCatalog.ComposeSnapshot([20], new HashSet<int>(), [cached, fetched]);
if (!removed.Select(x => x.AppId).SequenceEqual([20]))
    throw new InvalidOperationException("removed AppID survived authoritative snapshot composition");

var serialized = NewGamesCatalog.SerializeSnapshot(composed);
var roundTrip = NewGamesCatalog.ParseSnapshot(serialized);
if (roundTrip is null || !roundTrip.Select(x => x.AppId).SequenceEqual([10, 20]) ||
    NewGamesCatalog.ParseSnapshot("[{\"appId\":1,\"name\":\"\"}]") is not null)
{
    throw new InvalidOperationException("new-games snapshot validation/round-trip failed");
}

var newGamesPublishDirectory = Path.Combine(Path.GetTempPath(), $"nexaplay-new-games-{Guid.NewGuid():N}");
Directory.CreateDirectory(newGamesPublishDirectory);
try
{
    var activeList = Path.Combine(newGamesPublishDirectory, "new_games.json");
    await NewGamesCatalogFile.PublishAppIdListAsync("[1,2]", activeList);
    var originalList = await File.ReadAllTextAsync(activeList);
    var rejected = false;
    try
    {
        await NewGamesCatalogFile.PublishAppIdListAsync("[1,\"bad\"]", activeList);
    }
    catch (JsonException)
    {
        rejected = true;
    }

    if (!rejected || await File.ReadAllTextAsync(activeList) != originalList)
        throw new InvalidOperationException("invalid new-games list replaced the active source");

    var activeSnapshot = Path.Combine(newGamesPublishDirectory, "new_games_catalog.json");
    await NewGamesCatalogFile.PublishSnapshotAsync(serialized, activeSnapshot);
    var originalSnapshot = await File.ReadAllTextAsync(activeSnapshot);
    rejected = false;
    try
    {
        await NewGamesCatalogFile.PublishSnapshotAsync("[{\"appId\":1,\"name\":\"\"}]", activeSnapshot);
    }
    catch (JsonException)
    {
        rejected = true;
    }

    if (!rejected || await File.ReadAllTextAsync(activeSnapshot) != originalSnapshot)
        throw new InvalidOperationException("invalid new-games snapshot replaced the active source");
}
finally
{
    Directory.Delete(newGamesPublishDirectory, recursive: true);
}

Console.WriteLine("Self-check passed (cover priority, optional cover index, catalog generation, cache revision, safe cover publication, serialized reload, new-games catalog logic, and new-games atomic publication).");

sealed class StatusCodeHandler(System.Net.HttpStatusCode statusCode) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(statusCode) { RequestMessage = request });
}

sealed class NullLogService : NexaPlay.Contracts.Services.IAppLogService
{
    public event EventHandler<string>? LogAppended { add { } remove { } }

    public void Log(string message) { }
    public void Log(string category, string message) { }
    public IReadOnlyList<string> GetRecentLogs(int count = 200) => [];
    public Task<string> GetFullLogAsync() => Task.FromResult(string.Empty);
    public void Clear() { }
}
