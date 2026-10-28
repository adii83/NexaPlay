---
type: "query"
date: "2026-10-28T15:07:13.927067+00:00"
question: "Tambahkan password default online-fix.me dan override password pada mapping AppID untuk Online-Fix, beserta testing"
contributor: "graphify"
outcome: "useful"
source_nodes: ["OnlineFixEntry", "OnlineFixCatalogService", "OnlineFixFileTransaction", "OnlineFixService", "OnlineFixChecks"]
---

# Q: Tambahkan password default online-fix.me dan override password pada mapping AppID untuk Online-Fix, beserta testing

## Answer

OnlineFixEntry carries optional top-level Password; missing/null/empty resolves to online-fix.me, otherwise exact mapping value is used without trimming or fallback to the default. Plain archives ignore the supplied password. The resolved value is passed to ReaderOptions before archive opening, enabling encrypted RAR/7z headers as well as encrypted payloads. Wrong passwords return Failed and staging is removed before game modification. Password diagnostics are redacted. Checksum comparison is format-aware for keyed encrypted RAR5 checksums and zero AES ZIP AE-2 CRC. Six tiny inert real encrypted/unencrypted archive fixtures exercise service install/remove and wrong/default/explicit passwords, metadata cache/304 and cancellation. 213 Online-Fix assertions and full regression pass; Debug x64 build and startup smoke passed. Seed validation was corrected to compare game_count metadata plus SQLite quick_check instead of an obsolete hardcoded row count. New source requires Release publish/signing before release.

## Outcome

- Signal: useful

## Source Nodes

- OnlineFixEntry
- OnlineFixCatalogService
- OnlineFixFileTransaction
- OnlineFixService
- OnlineFixChecks