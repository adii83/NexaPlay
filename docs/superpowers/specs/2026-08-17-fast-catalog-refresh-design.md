# Fast Catalog Refresh Design

## Scope

Improve catalog behavior without changing the established cover behavior of Bypass Games, Bypass Detail, or Library.

The batch covers:

1. Games and Home Popular only: use R2 `library_capsule` lazily when no portrait capsule exists in higher-priority local sources, then fall back to header.
2. Load Games: newly downloaded catalog data becomes visible during the same app session without restarting.
3. Clear Cache: remove disposable derived/detail/image caches quickly while retaining essential source catalogs and user state.
4. Metadata tooling: make `collect_library_capsule.py` emit and verify deterministic GZIP output.

## Hard Constraints

- Performance is a product requirement: no catalog-wide R2 enrichment and no extra R2 work at startup.
- Do not modify Bypass Games, Bypass Detail, or Library cover selection.
- Games and Home Popular cover priority is exactly:
  1. NexaPlay catalog override capsule
  2. `library_capsule.json.gz` index
  3. runtime catalog capsule
  4. lazy R2 `library_capsule`
  5. header
  6. no-content placeholder
- R2 is queried only when the first three portrait sources are empty.
- The current page/batch is allowed to await the limited R2 fallback; background prefetch remains bounded and cancellable.
- Do not add a dependency.
- Clear Data remains the only factory reset and continues to remove all local application data after exit/restart.

## Cover Architecture

Add a focused `IListingCoverResolver` contract and Infrastructure implementation. The resolver receives AppID, runtime capsule, and header. It performs the shared priority decision for Games and Home Popular only.

The resolver is local-first. It loads the sparse override and lightweight cover index before considering R2. When a local portrait exists, it never calls `ISteamStoreService`. When no local portrait exists, it requests the existing cache-first `ISteamStoreService.GetDetailAsync` and selects `GameDetailEntry.LibraryCapsuleUrl`. Header is evaluated only after that request returns without a capsule.

A per-AppID in-flight task dictionary deduplicates simultaneous fallback requests from Home, Games, and prefetch. A small semaphore bounds R2 fallback concurrency. Completed results are retained in a small session cache because the resolved string is tiny. The existing detail service supplies the seven-day disk cache.

The resolver does not download image bytes. Existing `ICoverImageCacheService` remains responsible for local image files. Its write path receives per-destination single-flight synchronization and unique temporary filenames to remove deterministic `.tmp` collisions.

## Games and Home Integration

`GamesViewModel.GetOrBuildListingCardAsync` and `HomeViewModel.BuildPopularCardWithBestCoverAsync` call the resolver. Their local priority logic is removed so both use the same ordering.

Home's legacy `EnrichPopularCoversFromApiAsync` is retained only as a targeted collection-update mechanism, but its resolution delegates to the shared resolver. It does not query R2 if a local portrait exists.

Games continues to build only current-page cards and prefetch two pages. Home continues to build only the visible Popular batch and one next batch. No full-catalog iteration is added.

## Load Games Hot Reload

Add a lightweight singleton `ICatalogRefreshState` with a monotonically increasing `Generation`. It has no event bus and performs no I/O. The generation advances only after all source refresh/rebuild operations complete.

Correct refresh ordering:

1. Refresh NexaPlay sparse overrides.
2. Refresh the lightweight capsule index.
3. Download dynamic runtime sources and rebuild the metadata catalog using the already-refreshed override.
4. Refresh bypass data exactly as the existing Settings flow does.
5. Increment catalog generation.
6. Explicitly reload the singleton Home and Games ViewModels while the Settings operation is still active.

`HomeViewModel` and `GamesViewModel` store the generation they last loaded. On mismatch they invalidate only their in-memory derived state before loading again. Their next normal `OnNavigatedTo` is therefore also safe if explicit reload was interrupted.

Games filter cache receives a small envelope with the catalog generation/source revision. Existing legacy array cache is treated as stale and rebuilt. Load Games explicitly deletes/invalidate this derived cache before rebuilding, so a new AppID is searchable immediately.

The design deliberately does not add live subscriptions to every cached page. The user invokes Load Games from Settings, so explicitly refreshing Home/Games and generation-checking their next navigation is sufficient and cheaper.

## Clear Cache

Clear Cache preserves essential source files:

- `steam_data.json.gz`
- `steam_data.json`
- `override_data.json`
- `fix_games.json`
- `new_fix_games.json`
- `steam_games.json`
- `nexaplay_override.json` and ETag
- `library_capsule.json(.gz)`
- popular/new-fix source caches and ETags
- license, applied state, settings, update state, logs, and WebView2 data

It removes only disposable data:

- `runtime_catalog_sources/cover_files`
- `runtime_catalog_sources/games_filter_index_cache_v3.json`
- `metadata_detail`
- in-memory listing/card caches

Large cache directories are detached using an atomic same-volume directory rename to a unique tombstone. An empty active directory is created immediately, then the tombstone is deleted on a background worker. Startup/next clear removes stale tombstones. This keeps the Settings UI responsive. If rename fails, deletion runs via `Task.Run` rather than blocking the UI thread.

`MetadataService.ClearCacheAsync` no longer recursively deletes the whole source directory. It clears only its in-memory index state if invoked, while the Settings action targets disposable caches directly.

## Generator

`collect_library_capsule.py` writes `library_capsule.json` as before, then writes `library_capsule.json.gz` with `gzip.GzipFile(..., mtime=0)` for reproducible bytes. It reads the GZIP back, compares it to the in-memory result, and fails with a non-zero exception if they differ.

## Error Handling

- A local cover/index failure falls through to the next source.
- R2 errors are non-fatal; header remains the result.
- Cancellation propagates for navigation/search changes and is not converted into a fallback network request.
- Load Games publishes a new generation only after a successful rebuild.
- Existing usable source files remain available when a refresh download fails.
- Cache cleanup logs failures but does not delete source catalogs.

## Verification

Because the repository has no test project, add a small runnable assertion self-check for the pure cover priority decision and generation behavior, then use the required Debug x64 MSBuild gate.

Manual/runtime smoke checks:

- Games/Home item with override capsule: no R2 call.
- Item in capsule index: no R2 call.
- Item with runtime capsule: no R2 call.
- AppID 1293830 or another R2-only capsule: R2 capsule appears before header.
- Missing R2 capsule: header remains visible.
- Load Games then search a newly added AppID without restarting.
- Clear Cache completes promptly, preserves source catalogs/license, removes derived image/filter/detail caches.
- Navigate Home, Games, Library, and Bypass pages without crash; Bypass and Library appearance remains unchanged.
