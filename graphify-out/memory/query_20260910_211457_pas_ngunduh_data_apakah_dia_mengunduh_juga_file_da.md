---
type: "query"
date: "2026-09-10T21:14:57.205121+00:00"
question: "pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?"
contributor: "graphify"
outcome: "useful"
source_nodes: ["MetadataService", "SettingsViewModel", "NewGamesCatalog"]
---

# Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?

## Answer

Expanded from original query via vocab: [new, games, load, refresh, dynamic, settings, startup, warmup, cache, materialize, metadata, sync]. new_games.json is not fetched by first-start warmup or normal background sync. Settings Load Games calls RefreshDynamicSourcesAsync, then BuildIndexAsync with materializeNewGames=true, which calls RefreshNewGamesListAsync and requests NewGamesUrl. Clear Metadata Cache retains source files. Clear All Data removes them, but startup still waits for Load Games before fetching new_games.json.

## Outcome

- Signal: useful

## Source Nodes

- MetadataService
- SettingsViewModel
- NewGamesCatalog