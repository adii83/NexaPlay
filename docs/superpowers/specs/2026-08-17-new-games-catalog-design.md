# New Games Catalog Design

**Date:** 2026-08-17

## Goal

Allow NexaPlay to add games that are not yet present in its primary catalog by reading a thin, remotely managed AppID list from `new_games.json`, materializing only missing entries from per-AppID R2 metadata, and showing them immediately after **Load Games** without clearing data, clearing cache, or restarting the application.

## Scope

This change applies only to the primary Games catalog and downstream Home/Games refresh behavior.

It must not modify the behavior or files of:

- Bypass Games
- Bypass Detail
- Library

The existing `override_data.json` and `nexaplay_override.json` formats and semantics remain unchanged.

## Remote Contracts

### AppID list

URL:

```text
https://raw.githubusercontent.com/adii83/Nexaplay-Metadata-Override/main/new_games.json
```

Schema:

```json
[
  3751950,
  3768760
]
```

The root must be an array containing positive integer AppIDs only. Strings, non-integers, zero, and negative values are invalid. Duplicate AppIDs are deduplicated and the accepted list is normalized in ascending AppID order for deterministic output.

One invalid element rejects the entire downloaded candidate. NexaPlay then retains its last-known-good local list and catalog snapshot.

### Per-AppID metadata

URL template:

```text
https://meta.nexaplaymetadata.online/Metadata/{appid}.json
```

The AppID from `new_games.json` is authoritative for identity. NexaPlay does not use Steam Store as a fallback when materializing a catalog entry.

## Local Runtime Sources

Under `%LOCALAPPDATA%\NexaPlay\runtime_catalog_sources\` NexaPlay maintains:

```text
new_games.json
new_games.etag
new_games_catalog.json
```

- `new_games.json` is the last-known-good AppID list downloaded from GitHub.
- `new_games.etag` supports conditional downloads.
- `new_games_catalog.json` is an application-generated, lightweight catalog snapshot for AppIDs successfully materialized from R2.

`new_games_catalog.json` is not published to GitHub and requires no user maintenance. It is a runtime catalog source, not a disposable image/detail cache, so **Clear Cache** must retain all three files. **Clear Data** may remove them as part of a factory reset.

## Catalog Precedence

Index construction follows this order:

1. `steam_data.json.gz`
2. `steam_data.json`
3. `new_games_catalog.json`, additive only for AppIDs absent from steps 1–2
4. `override_data.json`
5. existing protection/status derivation
6. `nexaplay_override.json` as the final sparse catalog override

Rules:

- An AppID already present in the primary catalog is not fetched from R2 and is not replaced by the new-games snapshot.
- New-games entries are inserted before existing override layers, so current overrides can correct or enrich them naturally.
- Removing an AppID from a successfully validated remote `new_games.json` removes its materialized entry on the next **Load Games**, unless that AppID is now supplied by the primary catalog.
- A failed or invalid remote list must not be interpreted as an empty authoritative list; the last-known-good state remains active.

## Load Games Flow

When **Load Games** runs:

1. Conditionally download `new_games.json` using its ETag.
2. Validate the complete candidate and normalize its AppIDs.
3. Determine primary-catalog membership without applying new-games entries.
4. For each listed AppID absent from the primary catalog:
   - reuse its valid materialized entry from the existing local snapshot when available;
   - otherwise request `Metadata/{appid}.json` from R2.
5. Parse only the lightweight fields needed by `GameEntry`.
6. Skip individual AppIDs whose R2 request fails or whose metadata is invalid; do not create placeholder cards.
7. Build the complete candidate snapshot for the current accepted list.
8. Validate and publish the snapshot atomically.
9. Rebuild catalog state using the defined precedence.
10. Advance the catalog generation and force the existing Home/Games hot reload.
11. Report a concise result such as newly added and temporarily unavailable counts.

No R2 materialization occurs at application startup or merely by opening Home/Games. R2 requests occur only during **Load Games**, only for missing entries, and with bounded concurrency of at most four requests.

Previously materialized entries are reused from disk and are not downloaded again. An AppID skipped because of 404, timeout, malformed JSON, or missing required identity remains in `new_games.json` and is retried on a later **Load Games**.

## R2 Metadata Mapping

The catalog parser reads only lightweight list fields and does not materialize screenshots, movies, requirements, or other rich detail data.

Expected mappings include:

| `GameEntry` field | R2 source |
|---|---|
| `AppId` | AppID from `new_games.json` |
| `Name` | root name/title field |
| `Developer` | R2 developer data when present |
| `Publisher` | R2 publisher data when present |
| `Genre` | R2/store genre data when present |
| `ShortDescription` | R2/store short description when present |
| `ReleaseDate` | R2/store release date when present |
| `PriceDisplay` | `store_data.price_overview.final_formatted` when present |
| `PriceNormalized` | explicit catalog-compatible `price_normalized` when present; otherwise `0` until an existing override supplies it |
| `HeaderImageUrl` | first valid `assets.header[].url` |
| `LibraryCapsuleUrl` | first valid `assets.library_capsule[].url` |
| Other existing lightweight assets | matching R2 asset fields when present |

Minimum acceptance criteria for a materialized entry are:

- requested AppID is a positive integer;
- metadata is valid JSON with the expected object shape;
- game name/title is non-empty.

Image fields are optional. Missing images use the existing listing-cover pipeline for Games and Home Popular:

```text
override
→ capsule index
→ runtime capsule
→ lazy R2 capsule
→ header
→ No Content
```

Rich detail remains lazy through `SteamStoreService` when the detail page is opened.

## Atomicity and Failure Handling

- GitHub `304 Not Modified`: reuse the local list and materialized snapshot.
- GitHub timeout/offline/server error: retain last-known-good list and snapshot.
- Invalid downloaded list: reject it before publication and retain last-known-good state.
- One R2 404/timeout/invalid response: skip only that AppID and continue processing the rest.
- Application interruption during materialization: keep the previously published snapshot intact.
- Snapshot validation failure: do not replace the active snapshot.
- A valid accepted list that removes AppIDs: publish a snapshot without those entries.

Both downloaded list publication and generated snapshot publication use unique temporary files followed by same-volume atomic replacement only after full validation.

## Performance Constraints

- Local-first startup and page navigation.
- No catalog-wide or eager R2 work outside **Load Games**.
- Skip R2 entirely for AppIDs already in the primary catalog.
- Reuse valid entries from `new_games_catalog.json`.
- Deduplicate AppIDs before work begins.
- Bound R2 request concurrency to four.
- Keep rich detail parsing and assets out of the lightweight catalog snapshot.
- Preserve the existing serialized refresh/generation mechanism so stale loads cannot overwrite newer catalog state.

## User Experience

A successful **Load Games** makes newly materialized entries visible in Games and relevant Home surfaces immediately. Users do not need **Clear Cache**, **Clear Data**, page restart, or application restart.

A concise completion result should distinguish successful additions from unavailable metadata, for example:

```text
Load Games selesai: 8 game baru ditambahkan, 2 belum tersedia.
```

An unavailable AppID does not produce a broken or placeholder card. If every cover source is empty, the existing card rendering ends at **No Content**.

## Verification

Automated/self-check coverage must verify:

1. An AppID already in the primary catalog causes no R2 request and is not replaced.
2. A new AppID with valid R2 metadata enters the generated snapshot.
3. Duplicate AppIDs are processed once.
4. Invalid list elements reject the full candidate and preserve last-known-good state.
5. R2 404, timeout, and invalid metadata skip the individual AppID and remain retryable.
6. Removing an AppID from a valid list removes its additional snapshot entry.
7. Existing `override_data.json` and `nexaplay_override.json` still apply after insertion.
8. A corrupt candidate never replaces the active snapshot.
9. **Load Games** immediately refreshes Home/Games through the catalog generation path.
10. **Clear Cache** retains `new_games.json`, its ETag, and `new_games_catalog.json`.
11. Debug x64 build succeeds.
12. A scope guard confirms no changes to Bypass Games, Bypass Detail, or Library files.

Runtime smoke test:

```text
Add an AppID to GitHub new_games.json
→ press Load Games
→ confirm the game appears without restart
→ open its detail page
→ remove the AppID from GitHub
→ press Load Games
→ confirm it disappears if absent from the primary catalog
```
