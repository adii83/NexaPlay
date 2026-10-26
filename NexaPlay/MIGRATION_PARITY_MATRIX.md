# Migration Parity Matrix - GameHub -> NexaPlay

Gunakan dokumen ini sebagai tracker resmi migrasi fitur.
Update status setiap selesai implementasi/validasi.

## Legend

- `Done` = sudah setara behavior.
- `Partial` = sudah ada, tapi belum penuh.
- `Missing` = belum ada.

## A. Platform & Shell

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| App shell | WPF host + WebView2 | WinUI 3 native sidebar + Frame | Partial |
| Navigation | JS route via postMessage action | Native page navigation | Partial |
| Theme resources | CSS + web theme | `App.xaml` resource dictionary | Partial |

## B. License & Activation

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Load local license | `LicenseService` | `ILicenseService` + `LicenseService` | Done |
| Online validation on startup | Ada | Dipanggil dari `MainWindow` | Partial |
| Activation dialog | Halaman web aktivasi | `LicenseActivationDialog` | Partial |
| Ban/reset/error state handling | Ada di flow lama | Perlu parity detail | Missing |

## C. Home Data

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Popular games feed | `HomeDataService` raw json cache | `appid_populer.json` order + SQLite batch hydration | **Done** |
| New fix games feed | `HomeDataService` | Ordered-distinct max 12, SQLite local render, bounded background full-detail enrichment | **Done** |
| Cache TTL logic | Ada | SQLite runtime canonical + generation-based invalidation; side-feed ETag tetap aktif | **Done** |

## D. Games Catalog

> Current 2026-09-14: general listing runtime migrated to local SQLite. Rows below describe current authority and supersede older JSON-catalog checkpoints.

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Current metadata authority | steam metadata archive + override | Bundled `catalog_seed.sqlite` -> `%LocalAppData%\NexaPlay\Data\catalog.sqlite` | **Done** |
| Current catalog update | N/A | R2 manifest + sequential JSON patches; size/SHA/version validation and one transaction per patch | **Done** |
| Current search/filter | Web JS | Parameterized SQLite count/page/search/tier/protection/genre queries | **Done** |
| Current Denuvo final value | fix/new-fix/steam membership + override | SQLite base + same membership overlay + final NexaPlay override | **Done** |
| Current premium rule | harga katalog | Stored SQLite `tier`; fallback threshold `135000` | **Done** |
| Legacy full JSON listing | Runtime parser/index | `steam_data*`, `override_data`, `new_games*`, and filter-index generation retired from client general listing | **Done** |
| Metadata source | steam metadata archive + override | SQLite lokal untuk listing; R2 `Metadata/{appid}.json` tetap untuk full detail | **Done** |
| Catalog synchronization | N/A | R2 `catalog_manifest.json` + patch JSON berurutan; validasi size/SHA/schema/from/to dan transaction per patch | **Done** |
| Additive new-games source | N/A | [RETIRED] general listing; penambahan game masuk melalui patch SQLite | **Done** |
| Override data pipeline | `OverrideDataService` global + user | `ApplyOverrideDataAsync` (global + user, semua field) | **Done** |
| Denuvo auto-flag | fix_games + steam_games list | `ApplyAutoDenuvoFromListsAsync` + override dapat reset | **Done** |
| PREMIUM threshold | `price_normalized >= 130000` | Stored SQLite `tier`; fallback `GameEntry.IsPremium` memakai `>= 135000` | **Done** |
| Search/filter | Web JS | `GamesViewModel` search + `IsEmpty` state | **Done** |
| Virtualized grid/list | Web rendering | `GridView` + `ItemsWrapGrid` virtualized | **Done** |
| Game detail page | Modal popup web | `GameDetailPage` native — hero, screenshot strip, sidebar, aksi | **Done** |
| Rich detail on-demand | Steam API via JS | `ISteamStoreService` + `SteamStoreService` — cache 7 hari per-appid | **Done** |
| Cover priority pipeline | Steam CDN images | SQLite effective capsule/header -> byte cache -> placeholder; `library_capsule.json(.gz)`, cover index, dan full-detail listing fallback retired | **Done** |
| Load Games hot-reload | Page reload | Conditional override/feed/catalog check; overlay/generation/ViewModel hanya berubah saat source relevan berubah | **Done** |
| Clear Cache vs Clear Data | N/A | Clear Cache: epoch + detach/delete cover/detail/transient state, SQLite/source retained dan no generation bump. Clear Data tetap factory reset | **Done** |
| Pagination/infinite strategy | Web-side logic | Load more via `LoadMorePopularGamesCommand` | Partial |

## E. Library

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Scan installed Steam games | `SteamService` | `ISteamService` + `SteamPlatformService` | Partial |
| Show applied fixes | Applied state + UI | `.lua` membership + SQLite card hydration tanpa detail R2 | **Done** |
| Add/Remove game flow | `AddGameService` | `IAddGameService` + implementation | Partial |

Update Add Game 2026-10-26: enam sumber baru diprioritaskan sebelum GameHub.
Lima arsip GitHub disiapkan menjadi ZIP datar mengikuti aturan Lightning;
pemasangan Lua mempertahankan byte ZIP final tanpa mengomentari `setManifestid`.
Fallback juga berlaku untuk payload rusak/tanpa Lua. Detail dan validasi ada di
[ADD_GAME_PAYLOAD.md](ADD_GAME_PAYLOAD.md).

## F. Bypass Games Core (Formerly Fix Games)

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Fix catalog load | `FixGamesDataService` | `IBypassGamesDataService` + impl | Done |
| Apply fix flow | `OnlineFixService` + progress | `IOnlineFixService` + `BypassGamesViewModel` + `GameDetailViewModel` | Done |
| Progress state UI | Web progress updates | Native progress bar di `GameDetailPage` | **Done** |
| Unfix flow | Ada | Ada di `GameDetailViewModel.RemoveFixCommand` | Partial |
| Cancel flow | Ada | Ada di `GameDetailViewModel.CancelFixCommand` | Partial |
| Add-Game action | `AddGameService` | `GameDetailViewModel.AddGameCommand` | **Done** |
| Online-Fix action | `OnlineFixService` | `GameDetailViewModel.ApplyFixCommand` | Done |
| Restart Steam action | `SteamService.RestartSteam` | `GameDetailViewModel.RestartSteamCommand` | **Done** |
| Listing metadata requests | Web listing payload | Local JSON + one SQLite batch; zero `GetDetailAsync`, detail baru saat clicked row | **Done** |

Catatan nomenklatur UI Bypass:
- Untuk tampilan UI, istilah `Steam Sharing` sudah diganti menjadi `Akun Steam`.
- Label badge/card/detail memakai teks `AKUN STEAM`.
- Mapping data backend tetap memakai category/tag asli `steam-sharing` (display-only rename, tanpa perubahan source field).

## G. System / Security Helpers

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Windows Defender scan/exclusion | Terpisah | `IWindowsDefenderService` + impl | Partial |
| Steam path + library detect | Ada | Ada | Partial |
| Device ID helper | Ada | Ada | Done |

## H. Update System

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Check update metadata | `UpdateService` | `IAppUpdateService` + manifest GitHub + startup/settings check | Partial |
| Download installer | `UpdateService` | Download `setup.exe` + verifikasi SHA-256 | Partial |
| Install/launch updater | `UpdateService` | Helper PowerShell updater untuk silent install + relaunch | Partial |

## I. Logging & Diagnostics

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| App log service | `AppLogService` | `IAppLogService` + impl | Partial |
| JS console forwarding | Ada (WebView) | N/A di native | Done |
| UI log surface | Ada via web debug | Perlu panel native jika dibutuhkan | Missing |

## J. Deprecated/Not Carried

| Area | Keputusan |
|---|---|
| WebView2 host UI | Tidak dibawa ke NexaPlay |
| JS action bridge sebagai pusat aplikasi | Diganti command/event native |
| Web HTML/CSS rendering pipeline | Diganti XAML native |

## K. Prioritas Implementasi Berikutnya

1. Finalkan update system parity (`UpdateService` port ke `NexaPlay`).
2. Finalkan home data parity (`popular` + `new fix` + cache policy).
3. Lengkapi behavior parity `Bypass Games` (cancel/error edge cases).
4. Lengkapi `Library` parity (scan, add/remove, applied state sync).
5. Hardening warning:
   - `MVVMTK0045` (AOT-safe property pattern) — **Done** (semua ViewModel sudah migrasi ke partial property)
   - dependency advisory `SharpCompress`.
