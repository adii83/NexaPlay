---
type: "query"
date: "2026-10-27T13:02:02.392500+00:00"
question: "Untuk release aplikasi terbaru apakah catalog.sqlite harus terbaru atau boleh lama?"
contributor: "graphify"
outcome: "useful"
source_nodes: ["CatalogBootstrapService", "CatalogUpdateService"]
---

# Q: Untuk release aplikasi terbaru apakah catalog.sqlite harus terbaru atau boleh lama?

## Answer

Use a validated release-only catalog_seed.sqlite matching the published manifest latest_version for fresh installs. Current repository seed is catalog_version=2, built 2026-09-14, 163262 games. Existing user runtime Data/catalog.sqlite is retained by CatalogBootstrapService.EnsureReadyAsync if schema/integrity valid; installer seed is only copied if missing/invalid. CatalogUpdateService applies every consecutive patch, requires localVersion >= minimum_supported_version, and returns UnsupportedBaseline below that. Bundling a new seed alone does not repair a valid but unsupported old runtime database, so a future version-gated backed-up recovery/migration is needed for that edge case. The remote manifest could not be verified here (HTTP 403); do not claim local v2 is latest. Sources: release/README.md L100, CatalogBootstrapService L37, CatalogUpdateService L43.

## Outcome

- Signal: useful

## Source Nodes

- CatalogBootstrapService
- CatalogUpdateService