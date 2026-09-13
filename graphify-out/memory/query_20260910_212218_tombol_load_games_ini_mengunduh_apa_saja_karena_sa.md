---
type: "query"
date: "2026-09-10T21:22:18.685633+00:00"
question: "tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut"
contributor: "graphify"
outcome: "useful"
source_nodes: ["MetadataService", "SettingsViewModel", "GameCoverIndexService", "NexaPlayOverrideService", "BypassGamesDataService"]
---

# Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut

## Answer

Expanded from original query via vocab: [load, games, refresh, download, dynamic, metadata, settings, cover, override, catalog, cache, sync]. Load Games performs at least nine network checks/downloads: nexaplay_override, forced library_capsule gzip, four forced dynamic sources, conditional new_games, popular appids, and new-fix appids; it may also fetch missing per-AppID R2 metadata, up to three bypass sources in a separate cache, and lazy Home/Games detail data. Main observed bottleneck is forced library_capsule.json.gz refresh without ETag/progress; local log indicates roughly 101 seconds between override completion and cover-index completion. It also reparses the large local Steam catalog and reloads Home/Games. README has no explanation; design specs and AI_HANDOFF document the intended flow and local-first rationale.

## Outcome

- Signal: useful

## Source Nodes

- MetadataService
- SettingsViewModel
- GameCoverIndexService
- NexaPlayOverrideService
- BypassGamesDataService