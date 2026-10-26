---
type: "query"
date: "2026-10-26T14:14:17.355837+00:00"
question: "Analisa alur masing-masing API di api.json mendapatkan appid.zip dan pemrosesan manifest serta Lua sebelum dipindahkan"
contributor: "graphify"
outcome: "useful"
source_nodes: ["AddGameService", "AddGameAsync", "InstallLuaFromZip"]
---

# Q: Analisa alur masing-masing API di api.json mendapatkan appid.zip dan pemrosesan manifest serta Lua sebelum dipindahkan

## Answer

Expanded from original query via vocab: api download zip manifest lua extract provider. Graph points to AddGameService AddGameAsync L93 and InstallLuaFromZip L439; verified source and bundled api.json. Enabled providers are attempted sequentially GameHub, Ryuu, Sadie, TwentyTwo Cloud, Sushi. Substitute appid, HTTP GET, require configured success code, save body temporarily, stop download fallback on first completed download, validate PK signature. InstallLuaFromZip extracts manifest files unchanged to Steam depotcache first, then selects appid.lua or first numeric Lua, comments active setManifestid lines, normalizes CRLF and writes UTF-8 without BOM to Steam config/stplug-in/appid.lua. Finally removes downloaded archive. Invalid ZIP or Lua processing failure does not retry next provider.

## Outcome

- Signal: useful

## Source Nodes

- AddGameService
- AddGameAsync
- InstallLuaFromZip