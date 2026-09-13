---
type: "query"
date: "2026-09-10T19:46:57.696388+00:00"
question: "saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan"
contributor: "graphify"
outcome: "useful"
source_nodes: ["MetadataService", "SteamStoreService", "SteamPlatformService", "LicenseService", "BypassGamesDataService", "AppUpdateService", "TelegramAccountReportService", "DeviceIdHelper", "WindowsDefenderService", "AddGameService"]
---

# Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan

## Answer

Expanded from original query via graph vocab: metadata, data, sources, api, fetch, cache, environment, localconfig, user, games, steam, content. Direct runtime sources: GitHub steam-metadata-archive catalogs, GitHub Nexaplay-Metadata-Override data, NexaPlay R2 per-AppID metadata, bundled api.json provider list, Google Drive/OnlineFix downloads, GitHub update manifest, Supabase license RPC, Telegram report API, Gmail IMAP, Steam registry/VDF/ACF/process state, Windows WMI/PowerShell, user inputs, and LocalAppData caches. No analytics SDK or general telemetry found. Security risk: service credentials are hardcoded and HTTP providers are enabled.

## Outcome

- Signal: useful

## Source Nodes

- MetadataService
- SteamStoreService
- SteamPlatformService
- LicenseService
- BypassGamesDataService
- AppUpdateService
- TelegramAccountReportService
- DeviceIdHelper
- WindowsDefenderService
- AddGameService