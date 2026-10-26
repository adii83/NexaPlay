---
type: "query"
date: "2026-10-26T14:24:54.306835+00:00"
question: "Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?"
contributor: "graphify"
outcome: "useful"
source_nodes: ["AddGameService", "InstallLuaFromZip"]
---

# Q: Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?

## Answer

Graph vocab: manifest lua. Local AI_HANDOFF_PROMPT.md L979 says comment out setManifestid to match GameHub; no rationale stated there. GameHub AddGameService.cs L532 has same transformation. External primary source github.com/OpenSteam001/OpenSteamTool/issues/101 confirms active setManifestid binds a depot manifest and commenting it resolved updates for reporter; actual behavior depends on loader. Likely original intent is allowing updates rather than pinning old manifests, but historical author intent remains unconfirmed. Sibling _script/no-internet-fix-nexaplay.ps1 L454-472 downloads manifests and writes active setManifestid again.

## Outcome

- Signal: useful

## Source Nodes

- AddGameService
- InstallLuaFromZip