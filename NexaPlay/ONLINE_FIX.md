# Online-Fix — current implementation (2026-10-27)

Online-Fix has one mapping authority: `adii83/Nexaplay-Metadata-Override/main/online_fix.json`.
There is no LuaTools, generic Win64, alternate package, or alternate metadata endpoint fallback.

## Mapping contract

```json
[
  {
    "appid": 70,
    "files": [
      {
        "filename": "filename.rar",
        "gdrive_id": "REPLACE_WITH_ACTUAL_GOOGLE_DRIVE_ID",
        "gdrive_url": "https://drive.google.com/uc?export=download&id=REPLACE_WITH_ACTUAL_GOOGLE_DRIVE_ID"
      }
    ]
  }
]
```

AppIDs must be positive integers and unique. `files: []` means unavailable; `[]` removes all availability.
Filenames must be safe local basenames, unique within an AppID (maximum 64 packages).
Google Drive URLs must be HTTPS download URLs whose `id` matches `gdrive_id`.
An omitted URL is constructed from that same mapped ID; it does not select another source.
Malformed updates are rejected as a whole and never replace the last valid disk snapshot.
AppID membership and a nonempty file list establish catalog availability; they do not prove
that the mapped archive exists, that a custom ID is real, or that its binaries support the game.

## Freshness and request policy

- Startup schedules a background refresh after the initial content becomes usable. It does not block startup on this catalog.
- Loading Game Detail also schedules refresh; checking or applying Online-Fix awaits the same service.
- Only one refresh runs at a time. Successful checks suppress further checks for 10 minutes.
- Ordinary failures suppress retries for 1 minute. GitHub 403/429 responses honor Retry-After and rate-limit reset headers.
- No timer, per-card package HEAD requests, or refresh loop exists.
- The request uses GitHub Contents API `?ref=main`, raw-file media type, a unique query key,
  `Cache-Control: no-cache, max-age=0`, and the validated snapshot's ETag.
- 304 reuses the validated body. Each new app process revalidates; persisted cache age does not skip its first check.
- Body and ETag are stored together via atomic replacement in `%LocalAppData%/NexaPlay/online_fix_catalog.json`.
- A failed refresh retains the last good cache for later revalidation, but availability/apply returns an error
  until a refresh succeeds. A network failure cannot masquerade as an unavailable game or silently install stale mapping.
- Requests have a 15-second deadline and a 4 MiB catalog limit. GitHub API limits still apply; no anonymous key is embedded.

## Installation

1. Check only the mapping and ask for confirmation.
2. Resolve the actual installed Steam game folder; an Add Game Lua entry alone is not sufficient.
3. Acquire the service operation lock. Concurrent apply/remove requests receive a busy result.
4. Download every mapped package into a unique operation directory. Multiple files are required inputs, not fallback candidates.
5. Accept public Google Drive downloads and one validated public download-confirmation form for the same file ID.
   Login, permission, quota, HTTP, empty, truncated, and oversized responses fail explicitly.
6. Extract all packages into staging, never directly into the game. ZIP, RAR/RAR5, 7z, solid archives,
   and standard numbered multipart inputs use SharpCompress. No archive contents are executed.
7. Reject traversal, absolute paths, ADS, Windows device names, reparse points/symlinks, duplicate target paths,
   reserved recovery/log paths, empty archives, encrypted/incomplete archives, size inconsistencies, and ZIP/RAR CRC mismatch.
   Limits: 2 GiB per package, 4 GiB total expanded payload, 20,000 output files, 60-second idle reads, 20-minute download deadline.
8. Strip an AppID root folder only when all payload files share it. Patch `<appid>` in staged `unsteam.ini` before installation.
9. Copy existing originals to `.nexaplay-onlinefix/{appid}/backup/` inside the game folder and verify SHA-256.
10. Flush an Applying write-ahead journal before replacing any game files. Each replacement uses a same-directory
    temporary file and atomic rename. Save Applied only after all replacements complete.

Filesystem work, decompression, hashing, installation and rollback run on a worker thread.
Progress is throttled and carries overall operation progress; the returned `OnlineFixResult`, not a posted progress callback,
determines completion. Cancellation during commit restores originals and removes newly added files.
If rollback is blocked (for example by a locked file), the operation fails and keeps its journal/backups for retry.

## Remove, recovery, and UI lifecycle

The on-disk transaction or legacy fix log is authoritative; `applied_state.json` booleans are no longer used to claim installation.
Remove validates every journal path, original backup hash and current target hash before restoration.
It restores original bytes and deletes only newly installed files. Modified user/game files cause an explicit failure
instead of being overwritten. Locked files, invalid journals, missing/corrupt backups and cleanup failures cannot return success.
Applying/Removing records left after interruption are resumable through Remove Online-Fix; keep the backup folder intact.
Cancellation during Remove retains recovery data for the next Remove attempt.

Legacy `nexaplay-fix-log-{appid}.log` remains removable with complete path validation and error propagation.
Originals overwritten by older versions cannot be reconstructed because those versions did not create backups;
the completion message explicitly states that limitation.

The UI remains busy until cancellation/recovery actually completes. It ignores late progress, handles typed outcomes,
prevents simultaneous Add Game and Online-Fix actions in the same detail, and cancels/invalidates operations on navigation.
No automatic Steam restart or game launch is added.

## Verification

- Debug x64 MSBuild: successful, 0 errors.
- `dotnet run --project NexaPlay.SelfCheck`: full existing regression suite passed.
- Online-Fix: 111 assertions covering mapping validation, concurrent refresh, 304, cooldown/retry,
  offline/corrupt data, source changes/removal, rate-limit backoff, Drive confirmation/denial,
  unsafe archives, corrupted CRC, staged install, cancellation rollback, locked files,
  interrupted recovery, modified-file protection, legacy logs, busy guards, and explicit results.
- Official SharpCompress 0.48.1 sample archives tested from temporary files: RAR, RAR5,
  solid RAR, LZMA 7z, solid 7z, and a six-volume RAR archive. Each extracted successfully.
- Debug app startup successfully validated 597 mappings from the live GitHub Contents API.
- UI smoke: Home -> Game Detail -> unavailable Online-Fix dialog -> dismiss -> Home worked.
  Further UI automation was stopped with physical Escape and was not resumed.
- No actual mapped game package was installed. Mapping IDs are currently user-described placeholders;
  real end-to-end download/apply/remove QA remains necessary after the actual public links are supplied.

Targeted commands:

```powershell
dotnet run --project NexaPlay.SelfCheck -- --online-fix-only
dotnet run --project NexaPlay.SelfCheck -- --online-fix-archive C:\path\archive.rar
```

References: [mapping](https://github.com/adii83/Nexaplay-Metadata-Override/blob/main/online_fix.json),
[GitHub Contents API](https://docs.github.com/en/rest/repos/contents#get-repository-content).
