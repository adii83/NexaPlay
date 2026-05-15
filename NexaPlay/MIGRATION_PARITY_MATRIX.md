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
| Popular games feed | `HomeDataService` raw json cache | `HomeViewModel` ada, feed perlu wiring penuh | Partial |
| New fix games feed | `HomeDataService` | Belum wiring penuh UI | Missing |
| Cache TTL logic | Ada | Belum dipastikan parity | Missing |

## D. Games Catalog

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Metadata source | steam metadata archive + override | `IMetadataService`/`MetadataService` | Partial |
| Search/filter | Web JS | `GamesViewModel` ada | Partial |
| Virtualized grid/list | Web rendering | Perlu finalisasi `GridView` virtualized behavior | Partial |
| Pagination/infinite strategy | Web-side logic | Belum final | Missing |

## E. Library

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Scan installed Steam games | `SteamService` | `ISteamService` + `SteamPlatformService` | Partial |
| Show applied fixes | Applied state + UI | `LibraryViewModel` ada | Partial |
| Add/Remove game flow | `AddGameService` | `IAddGameService` + implementation | Partial |

## F. Fix Games Core

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Fix catalog load | `FixGamesDataService` | `IFixGamesDataService` + impl | Done |
| Apply fix flow | `OnlineFixService` + progress | `IOnlineFixService` + `FixGamesViewModel` | Partial |
| Progress state UI | Web progress updates | Native progress panel | Partial |
| Unfix flow | Ada | Ada, perlu parity behavior test | Partial |
| Cancel flow | Ada | Ada, perlu full test | Partial |

## G. System / Security Helpers

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Windows Defender scan/exclusion | Terpisah | `IWindowsDefenderService` + impl | Partial |
| Steam path + library detect | Ada | Ada | Partial |
| Device ID helper | Ada | Ada | Done |

## H. Update System

| Area | GameHub (lama) | NexaPlay (baru) | Status |
|---|---|---|---|
| Check update metadata | `UpdateService` | Belum ada service update parity penuh | Missing |
| Download installer | `UpdateService` | Missing | Missing |
| Install/launch updater | `UpdateService` | Missing | Missing |

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
3. Lengkapi behavior parity `FixGames` (cancel/error edge cases).
4. Lengkapi `Library` parity (scan, add/remove, applied state sync).
5. Hardening warning:
   - `MVVMTK0045` (AOT-safe property pattern)
   - dependency advisory `SharpCompress`.

