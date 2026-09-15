---
type: "query"
date: "2026-09-15T12:35:39.530813+00:00"
question: "NEXAPLAY Games Browse artwork eligibility filter"
contributor: "graphify"
outcome: "useful"
source_nodes: ["SqliteCatalogRepository", "BuildWhere", "QueryDailyBrowseAsync", "GamesViewModel", "FixEntry"]
---

# Q: NEXAPLAY Games Browse artwork eligibility filter

## Answer

Expanded from original query via graph vocab: [browse, catalog, capsule, count, filter, games, library, override, paging, search, sqlite, visibility]. SqliteCatalogRepository.BuildWhere now adds the browse-only effective library_capsule predicate beside the price floor; QueryDailyBrowseAsync candidate and fallback count use the same predicate. Search skips both browse predicates and keeps explicit filters. FixEntry treats NO CONTENT as missing artwork so search placeholder renders.

## Outcome

- Signal: useful

## Source Nodes

- SqliteCatalogRepository
- BuildWhere
- QueryDailyBrowseAsync
- GamesViewModel
- FixEntry