---
type: "query"
date: "2026-09-10T21:29:10.717565+00:00"
question: "file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama"
contributor: "graphify"
outcome: "useful"
source_nodes: ["MetadataService", "SettingsViewModel", "GameCoverIndexService", "BypassGamesDataService", "CatalogRefreshState"]
---

# Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama

## Answer

Expanded from original query via vocab: [etag, manifest, download, refresh, source, sync, metadata, cache, catalog, update, background, startup, generation]. Recommended architecture: one ETag-controlled catalog_manifest.json checked by startup, background, Clear All Data restart, and Load Games. A 304 performs no downloads or rebuild. A changed manifest lists SHA-256, size, URL, and revision for each source; download only changed files in bounded parallel batches, validate to temp, atomically publish the coherent set, rebuild once, advance generation once, reload UI once. GitHub Raw sources tested expose ETag but not useful Last-Modified, so replace HEAD-based comparison and forced GETs with conditional GET. R2 Metadata app objects also expose ETag; cache one ETag per AppID and request only missing or revision-changed entries. Unify duplicate source caches and never force library_capsule gzip when unchanged.

## Outcome

- Signal: useful

## Source Nodes

- MetadataService
- SettingsViewModel
- GameCoverIndexService
- BypassGamesDataService
- CatalogRefreshState