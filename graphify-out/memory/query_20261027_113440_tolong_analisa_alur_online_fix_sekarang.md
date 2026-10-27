---
type: "query"
date: "2026-10-27T11:34:40.929381+00:00"
question: "tolong analisa alur online fix sekarang"
contributor: "graphify"
outcome: "useful"
source_nodes: ["OnlineFixService", "GameDetailViewModel", "AppliedStateStore", "BypassGamesViewModel", "SteamPlatformService"]
---

# Q: tolong analisa alur online fix sekarang

## Answer

Expanded from original query via graph vocab: online fix apply unfix extract patch. Verified graph source locations against current source. Active entry is GameDetailPage ApplyFixCommand -> GameDetailViewModel.ApplyFixAsync L332: cached applied flag triggers RemoveFixAsync, otherwise HEAD availability then confirmation. OnlineFixService.CheckAvailabilityAsync L36 probes app-specific primary three times then a shared Win64 fallback; reachable fallback can report availability for any appId without compatibility validation. ApplyAsync L75 resolves Steam physical install path, streams primary GET or shared fallback, synchronously extracts with overwrite (L211), strips appId prefix when applicable, patches first unsteam.ini placeholder, writes per-game fix log and applied_state.json. No automatic restart/launch. Findings: ExtractAsync path combination lacks containment checks (L225/L250), and UnfixAsync trusts log paths (L203); confirmed Path.Combine allows parent/absolute paths without modifying files. Existing files overwritten with no backup; cancel/failure can leave partial changes with no completed log. ExtractAsync has no await and synchronous decompression can block UI. Cancel action L745 unlocks IsApplyingFix before old operation completes, allowing overlapping operations sharing a per-app temp path. Unfix L184 swallows deletion errors and clears flag, potentially reporting success with residual files. AppliedStateStore L21 reads boolean only and L51 swallows persistence failures. Progress<T> is used as terminal result channel, so final progress callback timing can affect UI error reporting. Primary archive extraction/body errors do not retry fallback. BypassGamesViewModel ApplyFixAsync has no callers found in current Presentation; visible card navigation opens separate BypassGameDetailPage. Analysis only, no source edits and no game installation performed.

## Outcome

- Signal: useful

## Source Nodes

- OnlineFixService
- GameDetailViewModel
- AppliedStateStore
- BypassGamesViewModel
- SteamPlatformService