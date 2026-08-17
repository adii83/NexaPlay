# Fast Catalog Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a performance-safe last-resort R2 capsule fallback to Games and Home Popular, hot-reload catalog updates, and make Clear Cache disposable-only and responsive.

**Architecture:** A shared listing-cover resolver centralizes the local-first priority for Games/Home only and limits/deduplicates R2 work. A tiny generation singleton coordinates refresh invalidation without an event bus. Cache cleanup detaches disposable directories before background deletion and never removes source catalogs.

**Tech Stack:** C#/.NET 8, WinUI 3, CommunityToolkit.Mvvm, System.Text.Json, Python stdlib gzip.

**Spec:** `docs/superpowers/specs/2026-08-17-fast-catalog-refresh-design.md`

## Global Constraints

- Do not modify Bypass Games, Bypass Detail, or Library cover behavior.
- R2 is queried only after override, capsule index, and runtime capsule are all absent.
- Header is the final image fallback after R2.
- No catalog-wide R2 requests and no added startup R2 work.
- No new dependency.
- Required validation gate is Debug x64 MSBuild plus runtime smoke testing.

---

### Task 1: Shared performance-safe listing cover resolver

**Files:**
- Create: `NexaPlay/Contracts/Services/IListingCoverResolver.cs`
- Create: `NexaPlay/Infrastructure/Services/ListingCoverResolver.cs`
- Modify: `NexaPlay/App.xaml.cs`
- Modify: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs`
- Modify: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs`
- Modify: `NexaPlay/Infrastructure/Services/CoverImageCacheService.cs`

**Interfaces:**
- Produces: `Task<string?> ResolveAsync(int appId, string? runtimeCapsule, string? header, CancellationToken ct = default)`.
- Priority: override → index → runtime capsule → lazy R2 capsule → header.

- [ ] Add a pure priority helper and assertion self-check covering all source combinations.
- [ ] Run the self-check and verify it fails before the resolver exists.
- [ ] Implement local-first resolution, a small R2 concurrency gate, and per-AppID single-flight caching.
- [ ] Register the singleton resolver in DI and inject it only into Games/Home.
- [ ] Replace duplicated Games/Home selection code; leave Bypass/Library files untouched.
- [ ] Add keyed image-download single-flight and unique temporary files.
- [ ] Run the assertion self-check and Debug x64 build.

### Task 2: Catalog generation and hot reload

**Files:**
- Create: `NexaPlay/Contracts/Services/ICatalogRefreshState.cs`
- Create: `NexaPlay/Infrastructure/Services/CatalogRefreshState.cs`
- Modify: `NexaPlay/App.xaml.cs`
- Modify: `NexaPlay/Contracts/Services/IGameCoverIndexService.cs`
- Modify: `NexaPlay/Infrastructure/Services/GameCoverIndexService.cs`
- Modify: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs`
- Modify: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs`
- Modify: `NexaPlay/Presentation/ViewModels/SettingsViewModel.cs`

**Interfaces:**
- Produces: `long Generation { get; }` and `long Advance()`.
- Produces: `Task RefreshAsync(CancellationToken ct = default)` on the cover index.
- Produces: explicit `ReloadCatalogAsync()` methods on Home/Games.

- [ ] Add assertion coverage that generation is monotonic.
- [ ] Implement the lock-free generation singleton and register it.
- [ ] Add forced cover-index refresh without clearing source catalogs first.
- [ ] Make Home/Games invalidate only their in-memory card/snapshot/filter state when generation changes.
- [ ] Version the Games disk filter cache and reject the legacy raw-array cache.
- [ ] Reorder Load Games: override/index refresh → dynamic source rebuild → bypass refresh → generation advance → Home/Games reload.
- [ ] Build Debug x64 and verify AppID search sees a rebuilt index in-session.

### Task 3: Fast disposable-only Clear Cache

**Files:**
- Modify: `NexaPlay/Contracts/Services/ISteamStoreService.cs`
- Modify: `NexaPlay/Infrastructure/Services/SteamStoreService.cs`
- Modify: `NexaPlay/Infrastructure/Services/CoverImageCacheService.cs`
- Modify: `NexaPlay/Infrastructure/Services/MetadataService.cs`
- Modify: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs`
- Modify: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs`
- Modify: `NexaPlay/Presentation/ViewModels/SettingsViewModel.cs`

**Interfaces:**
- Produces: bulk detail-cache clear and in-memory ViewModel cache invalidation.
- Directory cleanup uses rename-to-tombstone followed by background deletion.

- [ ] Implement reusable private detach/delete behavior inside each owning service, without introducing another abstraction.
- [ ] Make cover and detail cache clears return after detach and delete tombstones asynchronously.
- [ ] Make `MetadataService.ClearCacheAsync` retain runtime source files.
- [ ] Delete only the Games derived filter-index file and reset Home/Games in-memory card caches.
- [ ] Update Settings success text to distinguish cache cleanup from Clear Data.
- [ ] Verify source/license files survive while cover/detail/filter caches disappear.
- [ ] Build Debug x64.

### Task 4: Deterministic capsule index generator

**Files:**
- Modify: `D:/My Project/___Metadata Nexaplay Cloudfare R2/collect_library_capsule.py`

**Interfaces:**
- Produces: `library_capsule.json` and byte-reproducible `library_capsule.json.gz` containing equivalent JSON.

- [ ] Add `gzip` output with `mtime=0` and explicit UTF-8 JSON bytes.
- [ ] Read the generated GZIP back and assert exact data equivalence.
- [ ] Run the generator against the local metadata directory.
- [ ] Compare JSON/GZIP entry counts and report missing/error counts.

### Task 5: Documentation and end-to-end verification

**Files:**
- Modify: `NexaPlay/MIGRATION_PARITY_MATRIX.md`
- Modify: `NexaPlay/AI_HANDOFF_PROMPT.md`

**Interfaces:**
- Consumes all prior tasks.

- [ ] Run the required Debug x64 MSBuild command; repair all introduced errors.
- [ ] Launch NexaPlay and smoke-test Home, Games, Settings Load Games/Clear Cache, Bypass, and Library navigation.
- [ ] Confirm no Bypass/Library source files changed via `git diff --name-only`.
- [ ] Record exact build/smoke results and the scoped cover priority in the handoff checkpoint.
- [ ] Review the final diff for accidental source-catalog deletion, eager R2 loops, or unrelated refactoring.
