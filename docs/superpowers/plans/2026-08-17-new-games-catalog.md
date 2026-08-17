# New Games Catalog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add games missing from NexaPlay's primary catalog by materializing AppIDs from GitHub `new_games.json` through per-AppID R2 metadata during **Load Games**, then hot-reload Home/Games without restart.

**Architecture:** `MetadataService` remains the catalog owner. It conditionally downloads and strictly validates the authoritative AppID list, builds a deterministic local `new_games_catalog.json` snapshot using at most four R2 requests at once, and merges that snapshot after the two primary Steam sources but before existing overrides. Pure parsing/selection logic and validated atomic publication are isolated for runnable self-check coverage; startup remains local-only.

**Tech Stack:** C#/.NET 8, WinUI 3, CommunityToolkit.Mvvm, `System.Text.Json`, `HttpClient`, stdlib filesystem primitives.

**Spec:** `docs/superpowers/specs/2026-08-17-new-games-catalog-design.md`

## Global Constraints

- Do not modify Bypass Games, Bypass Detail, Library, or their cover/data behavior.
- Preserve every uncommitted cover-resolver, hot-reload, and Clear Cache change already present in this worktree; do not reset, checkout, stash, or overwrite them.
- `new_games.json` is a root JSON array of positive integer AppIDs and is authoritative only after complete validation.
- An AppID already present in `steam_data.json.gz` or `steam_data.json` is never fetched from R2 and never replaced by the generated snapshot.
- R2 materialization happens only from **Load Games**, never during startup, background catalog refresh, or page navigation.
- Reuse valid materialized entries; fetch only missing entries; bound R2 concurrency to four.
- Merge order is primary Steam sources → additive new-games snapshot → `override_data.json` → existing protection/status processing → `nexaplay_override.json`.
- A failed/invalid remote list or interrupted write retains last-known-good files; one failed R2 AppID does not fail other AppIDs.
- **Clear Cache** retains `new_games.json`, `new_games.etag`, and `new_games_catalog.json`; **Clear Data** may remove them.
- Do not derive `PriceNormalized` from R2 `store_data.price_overview.final`: that value is currency cents, while NexaPlay's premium threshold uses normalized rupiah. Use explicit catalog-compatible `price_normalized` only, otherwise `0`; existing overrides may supply it.
- No Steam Store fallback and no placeholder card for unavailable R2 metadata.
- No new NuGet dependency.
- Do not commit: the user did not request commits and this worktree already contains an uncommitted approved batch. Review using working-tree diff packages.
- Required gates: `NexaPlay.SelfCheck`, Debug x64 MSBuild, whitespace/scope checks, and runtime smoke-test instructions.

---

## File Map

- Create `NexaPlay/Core/Models/NewGamesCatalogModels.cs`: lightweight snapshot entry and refresh result.
- Create `NexaPlay/Core/Helpers/NewGamesCatalog.cs`: strict AppID validation, R2-to-lightweight parsing, deterministic snapshot parsing/serialization, fetch selection, and authoritative snapshot composition.
- Create `NexaPlay/Infrastructure/Services/NewGamesCatalogFile.cs`: validated same-directory temp publication for the list and snapshot.
- Modify `NexaPlay/Core/Constants/AppConstants.cs`: exact remote URLs and local file names.
- Modify `NexaPlay/Contracts/Services/IMetadataService.cs`: return the new-games result from the existing dynamic refresh operation.
- Modify `NexaPlay/Infrastructure/Services/MetadataService.cs`: ETag list refresh, bounded R2 materialization, additive merge, and rich lightweight-field application.
- Modify `NexaPlay/Infrastructure/Services/SteamStoreService.cs`: consume the shared R2 base URL constant instead of keeping a second literal.
- Modify `NexaPlay/Presentation/ViewModels/SettingsViewModel.cs`: display materialization outcome while preserving the existing generation/reload sequence.
- Modify `NexaPlay/Presentation/ViewModels/GamesViewModel.cs`: include the generated snapshot in source-revision invalidation.
- Modify `NexaPlay.SelfCheck/NexaPlay.SelfCheck.csproj` and `NexaPlay.SelfCheck/Program.cs`: runnable logic and atomic-publication checks.
- Modify `NexaPlay/MIGRATION_PARITY_MATRIX.md` and `NexaPlay/AI_HANDOFF_PROMPT.md`: record the completed additive catalog source and verification.

---

### Task 1: Pure new-games contract and parsing logic

**Files:**
- Create: `NexaPlay/Core/Models/NewGamesCatalogModels.cs`
- Create: `NexaPlay/Core/Helpers/NewGamesCatalog.cs`
- Modify: `NexaPlay.SelfCheck/NexaPlay.SelfCheck.csproj`
- Modify: `NexaPlay.SelfCheck/Program.cs`

**Interfaces:**
- Produces: `NewGamesCatalogEntry`, the disk-safe lightweight entry model.
- Produces: `readonly record struct NewGamesRefreshResult(int Added, int Unavailable)`.
- Produces: `NewGamesCatalog.ParseAppIds`, `ParseMetadata`, `ParseSnapshot`, `SerializeSnapshot`, `SelectFetchAppIds`, and `ComposeSnapshot`.
- Consumes only `System.Text.Json` and the new core model; no WinUI or infrastructure dependency.

- [ ] **Step 1: Add failing self-check cases and compile links**

Add these links to `NexaPlay.SelfCheck/NexaPlay.SelfCheck.csproj`:

```xml
<Compile Include="..\NexaPlay\Core\Models\NewGamesCatalogModels.cs" Link="NewGamesCatalogModels.cs" />
<Compile Include="..\NexaPlay\Core\Helpers\NewGamesCatalog.cs" Link="NewGamesCatalog.cs" />
```

Append self-checks to `NexaPlay.SelfCheck/Program.cs` using this representative R2 payload:

```csharp
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
```

Add pure precedence/removal/round-trip checks:

```csharp
var cached = new NewGamesCatalogEntry { AppId = 10, Name = "Cached" };
var fetched = new NewGamesCatalogEntry { AppId = 20, Name = "Fetched" };
var requested = new[] { 10, 20, 30 };
var primary = new HashSet<int> { 30 };
var cachedById = new Dictionary<int, NewGamesCatalogEntry> { [10] = cached };

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
```

- [ ] **Step 2: Run the self-check to prove the feature is absent**

Run:

```powershell
dotnet run --project .\NexaPlay.SelfCheck\NexaPlay.SelfCheck.csproj
```

Expected: compile failure because `NewGamesCatalogModels.cs` and `NewGamesCatalog.cs` do not exist yet.

- [ ] **Step 3: Create the lightweight models**

Create `NexaPlay/Core/Models/NewGamesCatalogModels.cs`:

```csharp
namespace NexaPlay.Core.Models;

public sealed class NewGamesCatalogEntry
{
    public int AppId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public string[] Developers { get; init; } = [];
    public string[] Publishers { get; init; } = [];
    public string? Genre { get; init; }
    public string? ShortDescription { get; init; }
    public string? ReleaseDate { get; init; }
    public int PriceNormalized { get; init; }
    public string? PriceDisplay { get; init; }
    public bool Protection { get; init; }
    public string? HeaderImageUrl { get; init; }
    public string? IconImageUrl { get; init; }
    public string? LibraryCapsuleUrl { get; init; }
    public string? LibraryHero2xUrl { get; init; }
    public string? BackgroundRawImageUrl { get; init; }
}

public readonly record struct NewGamesRefreshResult(int Added, int Unavailable);
```

- [ ] **Step 4: Implement strict parsing and deterministic composition**

Create `NexaPlay/Core/Helpers/NewGamesCatalog.cs` with these exact public signatures:

```csharp
public static int[]? ParseAppIds(string json);
public static NewGamesCatalogEntry? ParseMetadata(int appId, string json);
public static NewGamesCatalogEntry? ParseMetadata(int appId, JsonElement root);
public static NewGamesCatalogEntry[]? ParseSnapshot(string json);
public static string SerializeSnapshot(IEnumerable<NewGamesCatalogEntry> entries);
public static int[] SelectFetchAppIds(
    IEnumerable<int> requestedAppIds,
    ISet<int> primaryAppIds,
    IReadOnlyDictionary<int, NewGamesCatalogEntry> cachedEntries);
public static NewGamesCatalogEntry[] ComposeSnapshot(
    IEnumerable<int> requestedAppIds,
    ISet<int> primaryAppIds,
    IEnumerable<NewGamesCatalogEntry> materializedEntries);
```

Implementation rules:

- `ParseAppIds`: catch `JsonException`; require a root array; require every element to be an `Int32` JSON number greater than zero; return distinct ascending IDs; return an empty array for valid `[]`; return `null` for every invalid candidate.
- `ParseMetadata`: require `appId > 0`, root object, and nonblank root `name`, falling back to root `title`; use the authoritative method argument as `AppId`; copy developer/publisher string arrays from `store_data`; join `store_data.genres[].description` with `", "`; read short description, release date, `final_formatted`, explicit root `price_normalized`, explicit root `protection`, and first nonblank asset URL for `header`, `icon`, `library_capsule` (then `library_capsule_2x`), `library_hero_2x` (then `library_hero`), and `background_raw` (then `background`). Do not map `price_overview.final` into `PriceNormalized`.
- `ParseSnapshot`: deserialize with `JsonSerializerDefaults.Web`; reject a non-array, null item, nonpositive/duplicate AppID, or blank name; return entries sorted by AppID. A valid empty array is accepted.
- `SerializeSnapshot`: filter no entries silently—throw `JsonException` if any entry is invalid or duplicated; sort by AppID; serialize with `JsonSerializerDefaults.Web` and `WriteIndented = true`.
- `SelectFetchAppIds`: return requested IDs that are absent from both `primaryAppIds` and `cachedEntries`, distinct and ascending.
- `ComposeSnapshot`: retain only materialized entries whose AppIDs are in the current requested list and absent from primary; reject duplicate/invalid materialized entries via deterministic one-entry-per-ID composition; return ascending entries. Entries removed from the requested list therefore disappear.

- [ ] **Step 5: Run the pure self-check**

Run:

```powershell
dotnet run --project .\NexaPlay.SelfCheck\NexaPlay.SelfCheck.csproj
```

Expected: exit code `0`, with all old checks and the new parser/precedence/round-trip checks passing.

---

### Task 2: Validated atomic source publication

**Files:**
- Create: `NexaPlay/Infrastructure/Services/NewGamesCatalogFile.cs`
- Modify: `NexaPlay.SelfCheck/NexaPlay.SelfCheck.csproj`
- Modify: `NexaPlay.SelfCheck/Program.cs`

**Interfaces:**
- Consumes: `NewGamesCatalog.ParseAppIds` and `NewGamesCatalog.ParseSnapshot` from Task 1.
- Produces: `PublishAppIdListAsync(string json, string activePath, CancellationToken ct = default)`.
- Produces: `PublishSnapshotAsync(string json, string activePath, CancellationToken ct = default)`.

- [ ] **Step 1: Add a failing atomic-publication self-check**

Link the implementation in `NexaPlay.SelfCheck/NexaPlay.SelfCheck.csproj`:

```xml
<Compile Include="..\NexaPlay\Infrastructure\Services\NewGamesCatalogFile.cs" Link="NewGamesCatalogFile.cs" />
```

Append this self-check:

```csharp
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
```

- [ ] **Step 2: Run the self-check to prove publication support is absent**

Run:

```powershell
dotnet run --project .\NexaPlay.SelfCheck\NexaPlay.SelfCheck.csproj
```

Expected: compile failure because `NewGamesCatalogFile` does not exist.

- [ ] **Step 3: Implement validated same-directory publication**

Create `NexaPlay/Infrastructure/Services/NewGamesCatalogFile.cs`:

```csharp
using NexaPlay.Core.Helpers;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

public static class NewGamesCatalogFile
{
    public static Task PublishAppIdListAsync(string json, string activePath, CancellationToken ct = default) =>
        PublishAsync(json, activePath, text => NewGamesCatalog.ParseAppIds(text) is not null, ct);

    public static Task PublishSnapshotAsync(string json, string activePath, CancellationToken ct = default) =>
        PublishAsync(json, activePath, text => NewGamesCatalog.ParseSnapshot(text) is not null, ct);

    private static async Task PublishAsync(
        string json,
        string activePath,
        Func<string, bool> validate,
        CancellationToken ct)
    {
        if (!validate(json))
            throw new JsonException($"Invalid candidate for {Path.GetFileName(activePath)}.");

        var directory = Path.GetDirectoryName(activePath)
            ?? throw new ArgumentException("Active path must have a directory.", nameof(activePath));
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, $".{Path.GetFileName(activePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(tempPath, json, ct);
            var readback = await File.ReadAllTextAsync(tempPath, ct);
            if (!validate(readback))
                throw new JsonException($"Candidate readback failed for {Path.GetFileName(activePath)}.");
            File.Move(tempPath, activePath, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }
    }
}
```

- [ ] **Step 4: Run publication and regression self-checks**

Run:

```powershell
dotnet run --project .\NexaPlay.SelfCheck\NexaPlay.SelfCheck.csproj
```

Expected: exit code `0`; corrupt candidates preserve the active files.

---

### Task 3: Integrate authoritative list and R2 snapshot into MetadataService

**Files:**
- Modify: `NexaPlay/Core/Constants/AppConstants.cs`
- Modify: `NexaPlay/Contracts/Services/IMetadataService.cs`
- Modify: `NexaPlay/Infrastructure/Services/MetadataService.cs`
- Modify: `NexaPlay/Infrastructure/Services/SteamStoreService.cs`

**Interfaces:**
- Consumes: Task 1 models/helper and Task 2 file publisher.
- Changes: `RefreshDynamicSourcesAsync` returns `Task<NewGamesRefreshResult>` instead of `Task`; it remains the only Load Games dynamic-refresh entry point.
- Produces local sources: `new_games.json`, `new_games.etag`, and `new_games_catalog.json`.
- Preserves: all existing callers except the single Settings caller updated in Task 4.

- [ ] **Step 1: Add exact constants and interface result**

Add to the NexaPlay override URL block in `AppConstants.cs`:

```csharp
public const string NewGamesUrl = "https://raw.githubusercontent.com/adii83/Nexaplay-Metadata-Override/main/new_games.json";
public const string R2MetadataBaseUrl = "https://meta.nexaplaymetadata.online/Metadata";
```

Add to the file-name block:

```csharp
public const string NewGamesCacheFileName = "new_games.json";
public const string NewGamesEtagFileName = "new_games.etag";
public const string NewGamesCatalogFileName = "new_games_catalog.json";
```

Add `using NexaPlay.Core.Models;` if not already present and change `IMetadataService`:

```csharp
Task<NewGamesRefreshResult> RefreshDynamicSourcesAsync(
    IProgress<double>? progress = null,
    CancellationToken ct = default);
```

After adding `AppConstants.R2MetadataBaseUrl`, remove the private `R2MetadataBaseUrl` literal from `SteamStoreService` and build its detail URL with `AppConstants.R2MetadataBaseUrl` so both consumers share one endpoint constant.

- [ ] **Step 2: Add MetadataService paths and initialization**

Add fields beside existing runtime-source paths:

```csharp
private readonly string _newGamesFile;
private readonly string _newGamesEtagFile;
private readonly string _newGamesCatalogFile;
private static readonly TimeSpan NewGamesR2Timeout = TimeSpan.FromSeconds(30);
```

Initialize them from `AppConstants` in the constructor and do not load/fetch them there:

```csharp
_newGamesFile = Path.Combine(_catalogDir, AppConstants.NewGamesCacheFileName);
_newGamesEtagFile = Path.Combine(_catalogDir, AppConstants.NewGamesEtagFileName);
_newGamesCatalogFile = Path.Combine(_catalogDir, AppConstants.NewGamesCatalogFileName);
```

- [ ] **Step 3: Strictly refresh the AppID list with ETag and last-known-good fallback**

Add a private method with this signature:

```csharp
private async Task<int[]?> RefreshNewGamesListAsync(CancellationToken ct);
```

Implement it as follows:

1. Read `_newGamesEtagFile` with `TryReadText` and send `If-None-Match` when nonblank.
2. Use a linked CTS capped by `AppConstants.HttpDefaultTimeout`; user cancellation still propagates.
3. On `304`, parse `_newGamesFile` through `NewGamesCatalog.ParseAppIds`; return `null` if no valid local file.
4. On `200`, read the JSON, require `ParseAppIds(json) is not null`, serialize that normalized ascending array with `JsonSerializer.Serialize`, then call `NewGamesCatalogFile.PublishAppIdListAsync` with the normalized JSON before writing the response ETag.
5. Write a new ETag only after normalized list publication. If a successful response has no ETag, delete the stale ETag file.
6. Catch non-user-cancellation network/HTTP/JSON/IO errors, log them, and return the valid last-known-good local list. Return `null` if no valid local list exists.
7. Valid `[]` is authoritative and must be returned as an empty array, not treated as failure.

Do not reuse `FetchAppIdsWithEtagAsync`: it writes remote content before validation and accepts nested/string AppIDs, which violates this feature's strict contract.

- [ ] **Step 4: Add bounded R2 materialization**

Add:

```csharp
private async Task<NewGamesRefreshResult> MaterializeNewGamesAsync(
    IReadOnlyCollection<int> requestedAppIds,
    ISet<int> primaryAppIds,
    IProgress<double>? progress,
    CancellationToken ct);

private async Task<NewGamesCatalogEntry?> FetchNewGameEntryAsync(int appId, CancellationToken ct);
```

`MaterializeNewGamesAsync` must:

1. Parse the active snapshot; treat a missing/invalid snapshot as an empty cache and log invalid data.
2. Build `cachedById` only from valid snapshot entries.
3. Call `SelectFetchAppIds`; initialize progress at `80` and finish at `100`.
4. Fetch the selected IDs through `Task.WhenAll`, with one local `SemaphoreSlim(4, 4)` guarding calls. Update progress after each completed ID with `80 + completed * 20 / fetchCount`.
5. Catch per-AppID failures inside `FetchNewGameEntryAsync`; propagate only caller cancellation. A linked 30-second timeout, 404, non-success response, invalid JSON, or blank name returns `null` and is logged as unavailable.
6. Parse the response stream using `JsonDocument.ParseAsync` and `NewGamesCatalog.ParseMetadata(appId, root)`; do not allocate/store the full rich payload.
7. Compose from cached successes plus newly fetched successes using the current requested list and primary set.
8. Serialize and atomically publish the complete candidate snapshot. If publication fails without caller cancellation, retain the previous active snapshot, log the failure, report `Added = 0`, and count every requested non-primary ID absent from the previous snapshot as `Unavailable`.
9. On successful publication return `Added = newly fetched valid count` and `Unavailable = requested non-primary count minus composed snapshot count`.

No negative cache is persisted; unavailable AppIDs remain retryable on the next **Load Games**.

- [ ] **Step 5: Merge snapshot at the exact precedence point**

Change `BuildIndexAsync` to:

```csharp
private async Task<NewGamesRefreshResult> BuildIndexAsync(
    CancellationToken ct,
    bool materializeNewGames = false,
    IProgress<double>? progress = null)
```

Inside it:

1. Merge only `_steamDataGzFile` and `_steamDataFile` first.
2. Apply the existing primary-source validity guard before any R2 work.
3. Capture `var primaryAppIds = catalog.Keys.ToHashSet();` before adding any snapshot or override.
4. If `materializeNewGames` is true, call `RefreshNewGamesListAsync`. When it returns a non-null list, call `MaterializeNewGamesAsync`; when it returns `null`, retain the current snapshot and return a zero result.
5. Parse `_newGamesCatalogFile`; for each valid entry ordered by AppID, call `catalog.TryAdd` with only `RuntimeCatalogEntry` fields (`Title`, display/normalized price, protection, library capsule, header, genre). Record the AppIDs successfully added from the snapshot.
6. Merge `_overrideDataFile` after the snapshot so current override behavior wins.
7. Convert `RuntimeCatalogEntry` values to `_index` exactly as before.
8. For only the recorded snapshot-added AppIDs, copy `Developer`, `Publisher`, developer/publisher arrays, `ShortDescription`, `ReleaseDate`, `IconImageUrl`, `LibraryHero2xUrl`, and `BackgroundRawImageUrl` from the snapshot into the corresponding `GameEntry`. Do not overwrite name, price, protection, header, library capsule, or genre here because those already passed through `override_data.json` precedence.
9. Call `ApplyNexaPlayCatalogOverridesAsync` last, unchanged.
10. Return the materialization result.

Update existing `BuildIndexAsync(ct)` call sites to rely on optional defaults. Only `RefreshDynamicSourcesAsync` passes `materializeNewGames: true`, ensuring startup/background builds remain disk-only.

- [ ] **Step 6: Integrate materialization into dynamic refresh without duplicate primary parsing**

Change `RefreshDynamicSourcesAsync` to return `NewGamesRefreshResult`.

- Keep the four existing dynamic downloads.
- Adjust their progress slices to occupy `0..80` (20 points each).
- Reset the existing popular/new-fix ETags exactly as before.
- Acquire `_loadLock`, call `BuildIndexAsync(ct, materializeNewGames: true, progress)`, and release the lock in `finally`.
- Return that result.
- Do not add the new-games URL to `SyncSourcesCoreAsync` or `WarmupEssentialSourcesAsync`; those paths run at startup/background and must remain free of R2 materialization.

- [ ] **Step 7: Preserve Clear Cache and Clear Data semantics**

Verify `ClearCacheAsync` does not include `_newGamesFile`, `_newGamesEtagFile`, or `_newGamesCatalogFile` in its `disposable` array. Add no deletion for them. Existing full LocalAppData removal in `ClearAllDataAndRestartAsync` remains the factory-reset behavior.

- [ ] **Step 8: Compile the service integration**

Run:

```powershell
dotnet run --project .\NexaPlay.SelfCheck\NexaPlay.SelfCheck.csproj
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  '.\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64 `
  /p:OutDir="$PWD\NexaPlay\bin\x64\Debug-new-games-task3\"
```

Expected: self-check passes; build has 0 errors. Fix only errors introduced by Tasks 1–3.

---

### Task 4: Load Games result, cache revision, documentation, and final verification

**Files:**
- Modify: `NexaPlay/Presentation/ViewModels/SettingsViewModel.cs`
- Modify: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs`
- Modify: `NexaPlay/MIGRATION_PARITY_MATRIX.md`
- Modify: `NexaPlay/AI_HANDOFF_PROMPT.md`

**Interfaces:**
- Consumes: `NewGamesRefreshResult` returned from Task 3.
- Preserves: existing `_catalogRefreshState.Advance()`, resolver invalidation, and serialized Home/Games reload.

- [ ] **Step 1: Show the materialization result from Load Games**

In `SettingsViewModel.LoadGamesAsync`, capture:

```csharp
var newGamesResult = await _metadata.RefreshDynamicSourcesAsync(progress);
```

Do not add another refresh or another generation mechanism. Keep the existing order after this call:

```text
popular AppIDs → new-fix AppIDs → bypass refresh → generation advance → listing resolver clear → Home reload → Games reload
```

Build the success text without adding `StringBuilder`:

```csharp
var message = "Data game terbaru berhasil diunduh dan diperbarui.";
if (newGamesResult.Added > 0 || newGamesResult.Unavailable > 0)
{
    message += $" {newGamesResult.Added} game baru ditambahkan";
    if (newGamesResult.Unavailable > 0)
        message += $", {newGamesResult.Unavailable} belum tersedia";
    message += ".";
}
return (true, message);
```

Log the same counts. Do not turn unavailable individual AppIDs into an overall Load Games failure.

- [ ] **Step 2: Invalidate the Games derived filter index when the snapshot changes**

Add `"new_games_catalog.json"` to `GamesViewModel.GetCatalogSourceRevision()` beside the other runtime catalog sources. Do not add `new_games.json` or its ETag: only the materialized snapshot changes visible catalog content.

- [ ] **Step 3: Update parity and handoff documentation**

Add a concise Done row/checkpoint stating:

- `new_games.json` is an authoritative additive AppID list;
- missing primary AppIDs materialize from R2 only during **Load Games**;
- `new_games_catalog.json` is local/atomic and survives **Clear Cache**;
- existing overrides remain later in precedence;
- Home/Games hot-reload without restart;
- Bypass/Library remain untouched.

Place the handoff checkpoint immediately above `## 10. Update Log Ringkas` as required by project instructions.

- [ ] **Step 4: Run all static and build gates**

Run:

```powershell
dotnet run --project .\NexaPlay.SelfCheck\NexaPlay.SelfCheck.csproj
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  '.\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64 `
  /p:OutDir="$PWD\NexaPlay\bin\x64\Debug-new-games-final\"
git diff --check
git diff --name-only -- `
  NexaPlay/Presentation/ViewModels/BypassGamesViewModel.cs `
  NexaPlay/Presentation/ViewModels/BypassGameDetailViewModel.cs `
  NexaPlay/Presentation/ViewModels/LibraryViewModel.cs
```

Expected:

- self-check exits `0`;
- Debug x64 build has 0 errors;
- `git diff --check` is clean;
- scope-guard command prints nothing.

- [ ] **Step 5: Review the final diff for the feature's hard failures**

Search/review to confirm:

- `AppConstants.NewGamesUrl` is referenced only by the explicit Load Games path;
- no R2 loop was added to startup, background update, `EnsureIndexedAsync`, Home, or Games;
- primary membership is captured before snapshot and override merge;
- `override_data.json` merges after snapshot and `nexaplay_override.json` remains last;
- Clear Cache never deletes the three new-games source files;
- valid empty `[]` removes all additional entries;
- invalid/failed remote list cannot erase the last-known-good snapshot;
- R2 `final` cents never populate `PriceNormalized`;
- no Bypass/Library file changed.

- [ ] **Step 6: Prepare runtime smoke-test instructions**

Use the already-published remote list:

```text
https://raw.githubusercontent.com/adii83/Nexaplay-Metadata-Override/main/new_games.json
```

Smoke sequence:

```text
Launch NexaPlay
→ Settings → Load Games
→ verify completion message reports added/unavailable counts
→ search Games for AppID 3751950 and 3768760
→ verify available metadata appears without Clear Cache/Clear Data/restart
→ open a newly available detail page
→ restart NexaPlay and verify materialized entries load locally
→ temporarily test list removal in a controlled remote update
→ Load Games and verify removed additional entries disappear unless primary now supplies them
→ verify Bypass Games and Library navigation remain unchanged
```

If a runtime source file is inspected, confirm these exist under `%LOCALAPPDATA%\NexaPlay\runtime_catalog_sources\` after a successful Load Games:

```text
new_games.json
new_games.etag
new_games_catalog.json
```
