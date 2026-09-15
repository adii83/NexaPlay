---
type: "query"
date: "2026-09-15T13:45:07.573460+00:00"
question: "Fix new_fix_games missing after Clear Data and prefer LibraryHero for clicked detail hero"
contributor: "graphify"
outcome: "useful"
source_nodes: ["DynamicFeedRefreshService", "HomeViewModel", "GameDetailViewModel", "BypassGameDetailViewModel"]
---

# Q: Fix new_fix_games missing after Clear Data and prefer LibraryHero for clicked detail hero

## Answer

Expanded from original query via vocab: [dynamic, refresh, etag, missing, retry, atomic, home, recent, detail, hero, library, bypass]. DynamicFeedRefreshService accepted 304 without a local body, suppressed retries after failures, and rejected the live NewFix integer-array payload. HomeViewModel already reloads Recent on NewFixChanged. Both detail view models prioritized library_hero_2x before library_hero. Fix adds missing-body recovery, retry-safe session dedup, live payload validation, and shared detail hero order while preserving override and later fallbacks.

## Outcome

- Signal: useful

## Source Nodes

- DynamicFeedRefreshService
- HomeViewModel
- GameDetailViewModel
- BypassGameDetailViewModel