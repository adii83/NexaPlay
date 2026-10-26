---
type: "query"
date: "2026-10-26T14:53:43.249525+00:00"
question: "Apakah perilaku lima API terutama SPIN0ZAi identik dengan lampiran Lightning terbaru?"
contributor: "graphify"
outcome: "useful"
source_nodes: ["AddGamePayloadBuilder", "ResolveMode", "ProcessLua", "ValidateInstallable"]
---

# Q: Apakah perilaku lima API terutama SPIN0ZAi identik dengan lampiran Lightning terbaru?

## Answer

Latest attachment f8609585 is byte-identical to prior reference 0a229f75. Re-ran differential parity: all 16 payload fixtures match filenames and every extracted output file byte. ResolveMode auto uses case-sensitive SPIN0ZAi URL detection. With include_manifests=True, SPIN0ZAi retains only lines whose JS-trimStart view starts with addappid(, adddlc(, setManifestid(; writes original retained lines and LF plus blank line and original Lightning footer. Other four providers use standard mode preserving all lines plus footer. This does not establish universal equivalence: C# integration requires a numeric Lua before accepting source, unlike Python accepting manifest-only payload; networking/configuration/publication lifecycle differ; ZIP container metadata/compressed bytes need not match.

## Outcome

- Signal: useful

## Source Nodes

- AddGamePayloadBuilder
- ResolveMode
- ProcessLua
- ValidateInstallable