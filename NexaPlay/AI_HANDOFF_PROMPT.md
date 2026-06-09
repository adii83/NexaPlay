# AI Handoff Prompt - NexaPlay

Dokumen ini adalah checkpoint terbaru untuk AI/engineer yang melanjutkan NexaPlay.
Tujuannya: AI baru bisa langsung paham posisi terakhir tanpa user menjelaskan ulang dari awal.

Jika ada perubahan konteks besar, keputusan desain, fitur selesai, atau build result penting, update dokumen ini sebelum mengakhiri sesi.

## 1. Prompt Siap Pakai Untuk AI Baru

Copy prompt ini ke AI baru:

```text
Kamu adalah AI engineer utama untuk project NexaPlay.
Gunakan bahasa Indonesia.

Sebelum mengubah kode, WAJIB baca dokumen ini berurutan:
1. NexaPlay/README.md
2. NexaPlay/AGENTS.md
3. NexaPlay/ONBOARDING_ZERO_TO_PARITY.md
4. NexaPlay/MIGRATION_PARITY_MATRIX.md
5. NexaPlay/AI_HANDOFF_PROMPT.md
6. NexaPlay/AI_HANDOFF_HOME_HISTORY.md (riwayat detail perbaikan page Home)


Lokasi project utama:
- D:\My Project\NexaPlay\NexaPlay

Lokasi referensi GameHub:
- D:\My Project\NexaPlay\gamehub

Tugas awal sebelum edit:
1. Ringkas pemahaman posisi terakhir NexaPlay.
2. Sebutkan halaman/fokus aktif terbaru.
3. Jalankan baseline build.

Jangan redesign semua halaman sekaligus.
Jangan mengurangi feature parity GameHub.
Jangan mengubah behavior inti tanpa alasan kuat dan tanpa cek referensi GameHub.

Jangan lupa 
Tambahkan catatan baru di atas bagian ## 10. Update Log Ringkas setiap selesai batch penting.
Format:

```text
Tanggal:
- Fokus:
- Perubahan:
- Build:
- Next:
```

```

## 2. Status Project Saat Ini

NexaPlay adalah remake native WinUI 3 dari GameHub.
Target utamanya bukan membuat aplikasi baru dari nol, tetapi menjaga feature parity GameHub dengan UI native yang lebih ringan, cepat, dan profesional.

Stack aktif:

- WinUI 3
- C#
- .NET 8
- MVVM dengan service boundary

Boundary folder:

- `Contracts/`: interface service.
- `Core/`: model, enum, constant domain.
- `Infrastructure/`: implementasi teknis service.
- `Presentation/`: XAML page, ViewModel, converter.
- `App.xaml.cs`: dependency injection composition root.

Jangan kembalikan pola lama:

- WebView sebagai UI utama.
- HTML/CSS/JS sebagai rendering utama.
- JS bridge sebagai pusat behavior aplikasi.

## 3. Fokus Aktif Terbaru

Fokus aktif sekarang:

- Game Detail UI.
- Metadata untuk detail game.
- Label `STANDARD`, `PREMIUM`, dan `DENUVO`.
- Action penting di Game Detail: `Add Game`, `Online-Fix`, `Restart Steam`.
- Dark theme hitam-putih yang clean.

Game Detail sedang dipoles bertahap, bukan redesign semua halaman.

Yang sudah diarahkan:

- Hero memakai `library_hero_2x` dengan rasio native Steam library hero 3840 x 1240.
- Hero harus crop center bila gambar melebihi frame.
- Header/title sudah diperkecil agar lebih mirip referensi GameHub/teman user.
- Top overlay memakai gradient tipis ke bawah, bukan blok hitam transparan keras.
- Section title seperti `MEDIA`, `ABOUT THE GAME`, dll diperbesar, bold, dan diberi garis vertikal kiri.
- Metadata kanan sudah memuat `APP ID`, `RELEASE DATE`, `DEVELOPER`, `PUBLISHER`, `PRICE`.
- `View on Steam` di Game Detail sudah dihapus sesuai request terakhir.
- Price di Game Detail harus dari runtime catalog/source metadata repo, terutama `price_normalized`, bukan dari API detail Steam.
- Sticky action/status bar bawah sudah mulai diterapkan agar action penting tetap terlihat saat scroll.
- Terminologi UI terbaru untuk area bypass:
  - `Steam Sharing` diganti menjadi `Akun Steam` (display only).
  - Label di card dan detail menggunakan `AKUN STEAM`.
  - Tag/data backend tetap `steam-sharing` (source field asli tidak diubah).

Update UI Game Detail 2026-05-19:

- Label `PREMIUM`/`STANDARD`, `DENUVO`, dan aksi `Cek Bypass` dipindahkan sejajar ke strip metadata, di sisi kiri grup `APP ID`, `RELEASE DATE`, `DEVELOPER`, `PUBLISHER`, `PRICE`.
- `Cek Bypass` hanya UI placeholder untuk game berlabel Denuvo. Tampil sebagai tombol kecil berteks `CEK BYPASS` dengan ikon pencarian agar user paham ini action. Behavior/navigasi detail bypass belum dipasang.
- `Restart Steam` di sticky action bar memakai ikon refresh native, bukan emoji.
- Tab `Minimum` dan `Recommended` di `System Requirement` harus punya ukuran teks yang sama.
- Teks system requirement tidak boleh dipotong manual per karakter karena bisa memecah GPU seperti `Radeon RX 580`. Formatter cukup membersihkan HTML dan bullet, lalu biarkan WinUI wrap natural.
- Koreksi lanjutan: untuk `System Requirement`, tag `<br>` diperlakukan sebagai spasi agar hardware tidak terpecah aneh. Formatter tetap menambahkan hanging indent manual pada baris panjang supaya lanjutan bullet masuk ke dalam seperti list Word.
- Koreksi final untuk requirement: rendering sudah diganti dari satu `TextBlock` panjang menjadi `ItemsRepeater` item list. Bullet punya kolom sendiri dan teks punya kolom sendiri, sehingga wrapping lanjutan otomatis rata setelah bullet dan line height minimum/recommended memakai template yang sama.
- Section lama `SCREENSHOTS` sudah diganti menjadi `MEDIA`.
- Media section sekarang berupa carousel horizontal full-width di bawah strip label/metadata. Preview screenshot besar lama dihapus.
- Carousel media punya tombol panah kiri/kanan, hover scale ringan, dan klik gambar membuka overlay/lightbox.
- Section `TRAILERS` di Game Detail dihapus sementara dari UI sesuai arahan user. Trailer akan dibahas lagi sebagai ide terpisah nanti.
- Build sesudah batch ini berhasil dengan command MSBuild Debug x64 preview output. Sisa warning non-blocking saat build terakhir: 2 warning NU1902 `SharpCompress`.

Update engineering rule 2026-05-19:

- `AGENTS.md` sekarang memuat aturan SOLID. Untuk perubahan berikutnya, Presentation harus tetap bergantung ke `Contracts`, logic teknis masuk service, dan ViewModel tidak boleh menjadi tempat semua logic fetch/format/action.

## 4. Status Metadata Terbaru

Keputusan arsitektur metadata terbaru:

- Tidak memakai raw GitHub chunks besar sebagai source utama app runtime, karena repo bisa membesar.
- Detail game diambil on-demand dari API/service saat diperlukan.
- Catalog ringan tetap dipakai untuk field cepat dan label:
  - `appid`
  - `price_display`
  - `price_normalized`
  - `protection`

Source merge catalog ringan:

1. `steam_data.json.gz`
2. `steam_data.json`
3. `override_data.json`

Urutan override:

- `steam_data.json.gz` sebagai baseline.
- `steam_data.json` menimpa baseline jika AppID sama.
- `override_data.json` menimpa hasil akhir jika AppID sama.

Catatan protection:

- `true` berarti tampilkan Denuvo/protection.
- `false` dan `null` dari raw lama dianggap tidak perlu label Denuvo di UI.
- Selain field `protection`, AppID juga dianggap Denuvo jika muncul di salah satu sumber ini:
  - `fix_games.json`
  - `new_fix_games.json`
  - `steam_games/steam_games.json`

Contoh hasil cek sebelumnya untuk AppID `3321460`:

- `fix_games.json`: tidak ada.
- `new_fix_games.json`: ada.
- `steam_games.json`: ada.
- Maka Game Detail harus menampilkan label `DENUVO`.

Detail API/on-demand diharapkan bisa menyediakan struktur besar:

- `success`
- `source_priority`
- `fetch_status`
- `name`
- `steam_appid`
- `steamgriddb_game_id`
- `store_asset_mtime`
- `assets_count`
- `assets`
- `store_data`

Field penting yang diharapkan ada jika tersedia dari API:

- `assets.header`
- `assets.library_hero_2x`
- `assets.icon`
- `assets.background_raw`
- `assets.screenshots`
- `assets.movies`
- `assets.embedded_media`
- `store_data.about_the_game`
- `store_data.detailed_description`
- `store_data.short_description`
- `store_data.developers`
- `store_data.publishers`
- `store_data.release_date`
- `store_data.genres`
- `store_data.price_overview`

## 5. Aturan UI Wajib

Aturan utama:

1. Fitur NexaPlay harus tetap sama dengan GameHub secara behavior penting.
2. Ubah hanya tampilan atau struktur UI native, jangan ubah behavior inti tanpa alasan kuat.
3. Gaya visual harus profesional, clean, netral, dan konsisten.
4. UI tidak boleh terasa AI-generated.

Larangan UI:

- Jangan pakai emoji di UI, kecuali benar-benar lebih tepat dari ikon dan tetap satu warna.
- Jangan pakai warna campur/random.
- Jangan pakai gradient berlebihan.
- Jangan pakai style ramai, gimmick, atau dekorasi yang tidak perlu.
- Jangan pakai copywriting hiperbolik atau terlalu marketing.
- Jangan redesign semua halaman sekaligus.
- Jangan membuat komponen baru yang tidak punya fungsi jelas.

Design system sederhana:

- Dark theme fokus hitam dan putih.
- Netral dark surface yang konsisten.
- Satu warna aksen utama jika benar-benar perlu.
- Tombol utama cenderung putih.
- Tipografi rapi, hierarchy jelas, spacing stabil.
- Border dan surface depth boleh dipakai secukupnya.
- Hindari efek visual ramai.

Target desain:

- Modern tetapi enterprise-clean.
- Sidebar, topbar, dan content area rapi.
- Teks ringkas, formal, tidak alay, tidak dekoratif berlebihan.
- Visual depth secukupnya melalui border/surface level, bukan efek ramai.

## 6. Aturan Performa Wajib

Jaga performa sebagai prioritas:

- Startup harus ringan.
- Gunakan lazy loading untuk data/detail berat.
- Games list harus virtualized.
- Hindari operasi berat di UI thread.
- Detail metadata/media dipanggil saat dibutuhkan, bukan dimuat semua saat startup.
- Cache boleh dipakai, tetapi jangan membuat app menunggu download besar tanpa perlu.

Performance guardrail tambahan (wajib dipertahankan untuk fitur action seperti Add Game / Online-Fix):

- UI progress harus di-throttle (minimal perubahan persen atau interval waktu), hindari spam update per chunk kecil.
- Operasi file/network wajib jalan async + `CancellationToken` agar aksi user `Batal` responsif.
- Jangan lakukan logging verbose di loop panas; log cukup event penting: start, success, failed, cancelled.
- Jangan menaruh parse/download besar di constructor ViewModel atau startup window.
- Dialog proses harus state-driven dari ViewModel (bukan logic bisnis di code-behind) agar tidak memblok UI thread.
- Gunakan guard re-entrancy (`IsApplyingFix`, `IsAddingGame`) untuk mencegah proses ganda di AppID yang sama.

## 7. Workflow Wajib Per Batch

Kerjakan per halaman dan per batch kecil.

Urutan aman:

1. Pahami behavior dari GameHub jika fitur berkaitan parity.
2. Pilih satu area kecil.
3. Edit terbatas.
4. Build.
5. Jika error, perbaiki sampai hijau.
6. Baru lanjut batch berikutnya.
7. Update dokumen ini jika status/konteks berubah.

Build wajib:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64
```

Jika output normal terkunci karena app sedang berjalan, gunakan output preview:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64 /p:OutDir='D:\My Project\NexaPlay\NexaPlay\bin\x64\Debug-preview\'
```

Jangan lanjut jika build merah.

## 8. Catatan Build Terakhir

Build terakhir yang sudah dicek:

- Command: MSBuild `Debug x64` dengan `OutDir=Debug-preview`.
- Result: `Build succeeded`.
- Error: `0`.
- Warning terakhir yang diketahui: `2`, keduanya NU1902 `SharpCompress` advisory.

Jika AI baru masuk, tetap jalankan build ulang karena state lokal bisa berubah.

## 9. File Penting yang Perlu Dicek Untuk Fokus Aktif

Metadata/catalog:

- `NexaPlay/Core/Constants/AppConstants.cs`
- `NexaPlay/Core/Models/GameEntry.cs`
- `NexaPlay/Contracts/Services/IMetadataService.cs`
- `NexaPlay/Infrastructure/Services/MetadataService.cs`
- `NexaPlay/Contracts/Services/ISteamStoreService.cs`
- `NexaPlay/Infrastructure/Services/SteamStoreService.cs`

Game Detail:

- `NexaPlay/Presentation/ViewModels/GameDetailViewModel.cs`
- `NexaPlay/Presentation/Views/Pages/GameDetailPage.xaml`
- `NexaPlay/Presentation/Views/Pages/GameDetailPage.xaml.cs`

Games/Home navigation impact:

- `NexaPlay/Presentation/ViewModels/GamesViewModel.cs`
- `NexaPlay/Presentation/Views/Pages/GamesPage.xaml`
- `NexaPlay/Presentation/Views/Pages/GamesPage.xaml.cs`
- `NexaPlay/Presentation/Views/Pages/HomePage.xaml`
- `NexaPlay/Presentation/Views/Pages/HomePage.xaml.cs`
- `NexaPlay/MainWindow.xaml`
- `NexaPlay/MainWindow.xaml.cs`

Docs:

- `NexaPlay/README.md`
- `NexaPlay/AGENTS.md`
- `NexaPlay/ONBOARDING_ZERO_TO_PARITY.md`
- `NexaPlay/MIGRATION_PARITY_MATRIX.md`
- `NexaPlay/AI_HANDOFF_PROMPT.md`

  - `price_display`
  - `price_normalized`
  - `protection`

Source merge catalog ringan:

1. `steam_data.json.gz`
2. `steam_data.json`
3. `override_data.json`

Urutan override:

- `steam_data.json.gz` sebagai baseline.
- `steam_data.json` menimpa baseline jika AppID sama.
- `override_data.json` menimpa hasil akhir jika AppID sama.

Catatan protection:

- `true` berarti tampilkan Denuvo/protection.
- `false` dan `null` dari raw lama dianggap tidak perlu label Denuvo di UI.
- Selain field `protection`, AppID juga dianggap Denuvo jika muncul di salah satu sumber ini:
  - `fix_games.json`
  - `new_fix_games.json`
  - `steam_games/steam_games.json`

Contoh hasil cek sebelumnya untuk AppID `3321460`:

- `fix_games.json`: tidak ada.
- `new_fix_games.json`: ada.
- `steam_games.json`: ada.
- Maka Game Detail harus menampilkan label `DENUVO`.

Detail API/on-demand diharapkan bisa menyediakan struktur besar:

- `success`
- `source_priority`
- `fetch_status`
- `name`
- `steam_appid`
- `steamgriddb_game_id`
- `store_asset_mtime`
- `assets_count`
- `assets`
- `store_data`

Field penting yang diharapkan ada jika tersedia dari API:

- `assets.header`
- `assets.library_hero_2x`
- `assets.icon`
- `assets.background_raw`
- `assets.screenshots`
- `assets.movies`
- `assets.embedded_media`
- `store_data.about_the_game`
- `store_data.detailed_description`
- `store_data.short_description`
- `store_data.developers`
- `store_data.publishers`
- `store_data.release_date`
- `store_data.genres`
- `store_data.price_overview`

## 5. Aturan UI Wajib

Aturan utama:

1. Fitur NexaPlay harus tetap sama dengan GameHub secara behavior penting.
2. Ubah hanya tampilan atau struktur UI native, jangan ubah behavior inti tanpa alasan kuat.
3. Gaya visual harus profesional, clean, netral, dan konsisten.
4. UI tidak boleh terasa AI-generated.

Larangan UI:

- Jangan pakai emoji di UI, kecuali benar-benar lebih tepat dari ikon dan tetap satu warna.
- Jangan pakai warna campur/random.
- Jangan pakai gradient berlebihan.
- Jangan pakai style ramai, gimmick, atau dekorasi yang tidak perlu.
- Jangan pakai copywriting hiperbolik atau terlalu marketing.
- Jangan redesign semua halaman sekaligus.
- Jangan membuat komponen baru yang tidak punya fungsi jelas.

Design system sederhana:

- Dark theme fokus hitam dan putih.
- Netral dark surface yang konsisten.
- Satu warna aksen utama jika benar-benar perlu.
- Tombol utama cenderung putih.
- Tipografi rapi, hierarchy jelas, spacing stabil.
- Border dan surface depth boleh dipakai secukupnya.
- Hindari efek visual ramai.

Target desain:

- Modern tetapi enterprise-clean.
- Sidebar, topbar, dan content area rapi.
- Teks ringkas, formal, tidak alay, tidak dekoratif berlebihan.
- Visual depth secukupnya melalui border/surface level, bukan efek ramai.

## 6. Aturan Performa Wajib

Jaga performa sebagai prioritas:

- Startup harus ringan.
- Gunakan lazy loading untuk data/detail berat.
- Games list harus virtualized.
- Hindari operasi berat di UI thread.
- Detail metadata/media dipanggil saat dibutuhkan, bukan dimuat semua saat startup.
- Cache boleh dipakai, tetapi jangan membuat app menunggu download besar tanpa perlu.

Performance guardrail tambahan (wajib dipertahankan untuk fitur action seperti Add Game / Online-Fix):

- UI progress harus di-throttle (minimal perubahan persen atau interval waktu), hindari spam update per chunk kecil.
- Operasi file/network wajib jalan async + `CancellationToken` agar aksi user `Batal` responsif.
- Jangan lakukan logging verbose di loop panas; log cukup event penting: start, success, failed, cancelled.
- Jangan menaruh parse/download besar di constructor ViewModel atau startup window.
- Dialog proses harus state-driven dari ViewModel (bukan logic bisnis di code-behind) agar tidak memblok UI thread.
- Gunakan guard re-entrancy (`IsApplyingFix`, `IsAddingGame`) untuk mencegah proses ganda di AppID yang sama.

## 7. Workflow Wajib Per Batch

Kerjakan per halaman dan per batch kecil.

Urutan aman:

1. Pahami behavior dari GameHub jika fitur berkaitan parity.
2. Pilih satu area kecil.
3. Edit terbatas.
4. Build.
5. Jika error, perbaiki sampai hijau.
6. Baru lanjut batch berikutnya.
7. Update dokumen ini jika status/konteks berubah.

Build wajib:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64
```

Jika output normal terkunci karena app sedang berjalan, gunakan output preview:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64 /p:OutDir='D:\My Project\NexaPlay\NexaPlay\bin\x64\Debug-preview\'
```

Jangan lanjut jika build merah.

## 8. Catatan Build Terakhir

Build terakhir yang sudah dicek:

- Command: MSBuild `Debug x64` dengan `OutDir=Debug-preview`.
- Result: `Build succeeded`.
- Error: `0`.
- Warning terakhir yang diketahui: `2`, keduanya NU1902 `SharpCompress` advisory.

Jika AI baru masuk, tetap jalankan build ulang karena state lokal bisa berubah.

## 9. File Penting yang Perlu Dicek Untuk Fokus Aktif

Metadata/catalog:

- `NexaPlay/Core/Constants/AppConstants.cs`
- `NexaPlay/Core/Models/GameEntry.cs`
- `NexaPlay/Contracts/Services/IMetadataService.cs`
- `NexaPlay/Infrastructure/Services/MetadataService.cs`
- `NexaPlay/Contracts/Services/ISteamStoreService.cs`
- `NexaPlay/Infrastructure/Services/SteamStoreService.cs`

Game Detail:

- `NexaPlay/Presentation/ViewModels/GameDetailViewModel.cs`
- `NexaPlay/Presentation/Views/Pages/GameDetailPage.xaml`
- `NexaPlay/Presentation/Views/Pages/GameDetailPage.xaml.cs`

Games/Home navigation impact:

- `NexaPlay/Presentation/ViewModels/GamesViewModel.cs`
- `NexaPlay/Presentation/Views/Pages/GamesPage.xaml`
- `NexaPlay/Presentation/Views/Pages/GamesPage.xaml.cs`
- `NexaPlay/Presentation/Views/Pages/HomePage.xaml`
- `NexaPlay/Presentation/Views/Pages/HomePage.xaml.cs`
- `NexaPlay/MainWindow.xaml`
- `NexaPlay/MainWindow.xaml.cs`

Docs:

- `NexaPlay/README.md`
- `NexaPlay/AGENTS.md`
- `NexaPlay/ONBOARDING_ZERO_TO_PARITY.md`
- `NexaPlay/MIGRATION_PARITY_MATRIX.md`
- `NexaPlay/AI_HANDOFF_PROMPT.md`


Tanggal: 2026-06-07
- Fokus: Hardening startup license activation setelah repro crash first-run pasca banned.
- Perubahan: Jalur startup lisensi di `MainWindow` kini dibungkus fallback agar exception tidak langsung membunuh window; `LicenseService.LoadAsync()` dipindah ke background thread dan diberi catch+fallback result; `LicenseStore.Load()` tidak lagi memakai `JsonSerializer.Deserialize<StoredLicense>` untuk path baca startup, tetapi parse manual via `JsonDocument` sambil tetap menjaga AES parity dan trace log.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` pending setelah patch ini.
- Next: Rebuild lalu repro skenario aktivasi -> banned -> tutup app -> run pertama untuk memastikan crash berubah menjadi fallback/activation overlay dan log menulis exception lengkap bila masih ada.

Tanggal: 2026-06-07
- Fokus: Koreksi guard card premium pada tab Steam Sharing.
- Perubahan: `BypassGamesPage` guard klik card premium sekarang mengacu ke tab aktif `steam-sharing`, bukan enum item `Category` saja, karena data `_steamGames` di katalog Akun Steam bisa tetap lolos walau card sedang dibuka dari tab Steam Sharing.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Re-test klik card premium di tab Steam Sharing dengan license Standard untuk memastikan langsung muncul dialog dan tidak membuka `BypassGameDetailPage`.

Tanggal: 2026-06-07
- Fokus: Premium gating parity untuk action detail dan card Akun Steam premium.
- Perubahan: `GameDetail` kini memblok `Add Game` premium untuk license Standard; `BypassGameDetail` memblok `Mulai Proses Bypass` premium; `BypassGamesPage` memblok klik card `steam-sharing` premium langsung dari card. Semua blokir memakai copy parity GameHub (`Fitur Premium` / `Upgrade Ke Premium Dulu, Ya, Untuk Buka Fitur Ini 😁`) lewat dialog dark rounded reusable.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Smoke test 3 skenario: Standard klik `Add Game` game premium, Standard klik `Mulai Proses Bypass` premium, Standard klik card `steam-sharing` premium.

Tanggal: 2026-06-08
- Fokus: Sinkronisasi sumber label premium antara Home, Bypass card, dan Bypass detail.
- Perubahan: `BypassGameDetailViewModel` tidak lagi memprioritaskan `Game.IsPremium` untuk badge/action bypass jika `BypassEntry` tersedia; detail bypass sekarang mengikuti flag premium yang sama dengan card bypass/GameHub. `HomeViewModel` section `New Bypass Games` juga sekarang mencoba mewarisi `premium`, `category`, dan field bypass lain dari katalog bypass lebih dulu sebelum fallback ke metadata umum. Jalur `Home > Popular Games` disamakan lagi ke snapshot katalog yang sama dengan `Games` page, lalu setiap item di-clone dari snapshot itu agar label premium/standard mengikuti sumber yang sama dan tidak melenceng karena object metadata referensi yang berubah.
- Build: `MSBuild Debug x64 /p:OutDir=Debug-preview` sukses (`0 Error(s)`, `0 Warning(s)`). Build normal sempat tertahan lock `NexaPlay.exe` aktif.
- Next: build ulang lalu validasi dua skenario utama: card 3rd-party/steam-sharing standard tidak berubah premium saat masuk detail bypass, dan section `New Bypass Games` di Home tidak lagi berbeda label dengan sumber bypass yang sama.

Tanggal: 2026-06-08
- Fokus: Memperjelas CTA Home untuk navigasi cepat ke Bypass 3rd Party dan Games.
- Perubahan: CTA header `New Bypass Games` di `HomePage` diubah menjadi `Lihat Bypass` dengan panah dan underline, lalu diarahkan ke `BypassGamesPage` dengan parameter `all` agar masuk ke katalog 3rd Party. CTA header `Popular Games` dipoles dengan pola visual yang sama dan diarahkan ke `GamesPage`.
- Build: `MSBuild Debug x64 /p:OutDir=Debug-preview` sukses (`0 Error(s)`, `8 Warning(s)` nullability lama di `BypassGameDetailViewModel`).
- Next: Smoke test klik CTA `Lihat Bypass` dan `Jelajahi Games` langsung dari Home untuk verifikasi arah navigasi dan rasa visual tombol.

Tanggal: 2026-06-08
- Fokus: Wiring hero `View Bypass` di Home ke detail bypass yang sesuai + cleanup warning nullability.
- Perubahan: Tombol hero `View Bypass` pada `New Bypass Games` sekarang menerima `FixEntry` aktif dari carousel, hover text berubah putih tanpa warna cyan default, dan klik langsung menavigasi ke `BypassGameDetailPage` dengan payload `(AppId, FixEntry)` agar game bypass yang sesuai tetap terbuka baik kategori Steam maupun 3rd Party. `BypassGameDetailViewModel.StartBypassGameAsync()` juga dirapikan memakai local snapshot `bypassEntry/game` untuk menghapus warning nullability tanpa mengubah guard premium/standard yang sudah ada.
- Build: `MSBuild Debug x64 /p:OutDir=Debug-preview` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: Smoke test dari Home hero untuk dua skenario: item Steam Account membuka detail Akun Steam yang tepat, dan item 3rd Party membuka detail bypass 3rd Party yang tepat sambil mempertahankan gate action premium yang lama.

Tanggal: 2026-06-08
- Fokus: Final fix hover warna CTA hero `View Bypass` di Home.
- Perubahan: CTA hero `View Bypass` diganti dari `HyperlinkButton` ke `Border + TextBlock` custom karena visual state bawaan `HyperlinkButton` masih memaksa warna biru saat hover. Hover sekarang benar-benar mengubah teks menjadi putih dan tetap mempertahankan navigasi ke `BypassGameDetailPage` lewat payload `FixEntry` aktif.
- Build: `MSBuild Debug x64 /p:OutDir=Debug-preview` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: Validasi runtime hover CTA hero bahwa teks tidak lagi biru dan klik tetap membuka detail bypass item carousel aktif.

Tanggal: 2026-06-08
- Fokus: Menutup celah premium `steam-type` dari tombol hero `View Bypass` di Home.
- Perubahan: `HomePage` sekarang memakai guard lisensi yang sama seperti card `steam-sharing` di `BypassGamesPage` sebelum membuka `BypassGameDetailPage` dari hero `View Bypass`. Item `IsSteamType && IsPremium` kini diblok untuk license Standard/invalid dengan dialog parity yang sama, sementara item non-steam tetap boleh masuk ke detail bypass.
- Build: `MSBuild Debug x64 /p:OutDir=Debug-preview` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: Re-test dua skenario dari hero Home: game premium steam-type harus diblok untuk Standard, sedangkan game premium/non-premium 3rd Party tetap boleh membuka `BypassGameDetailPage`.

## 10. Update Log Ringkas

```text
Tanggal: 7 Juni 2026
- Fokus: Mempersempit titik crash startup banned ke jalur load license
- Perubahan:
  - Dari repro baru, `nexaplay.log` menunjukkan app mencapai `OnFirstActivated` dan `ContentFrame.Loaded`, lalu berhenti sebelum log lanjutan `ValidateLicenseAsync`, sehingga crash menyempit ke awal masuk validasi lisensi startup.
  - `MainWindow.xaml.cs` ditambah log paling awal `ValidateLicenseAsync entered`.
  - `LicenseService.LoadAsync()` ditambah breadcrumb sebelum baca cache, sebelum panggil store, dan sesudah hasil store kembali.
  - `LicenseStore.Load()` ditambah trace langsung ke `nexaplay.log`: file mana yang dipakai, panjang payload terenkripsi/terdekripsi, hasil parse, device match, migrasi GameHub -> NexaPlay, dan exception jika ada.
- Build: Pass (`Debug x64`, `0 Error(s)`, `0 Warning(s)`).
- Next: Repro ulang skenario banned dan baca urutan log `LicenseFlow` + `LicenseStore` di `%LOCALAPPDATA%\NexaPlay\nexaplay.log` untuk tahu apakah crash terjadi sebelum `LoadAsync`, saat decrypt/parse license file, atau sesudah hasil load kembali.
```

```text
Tanggal: 7 Juni 2026
- Fokus: Hardening diagnosis crash first-run setelah license dibanned
- Perubahan:
  - Ditemukan mismatch path logging: `App.xaml.cs` menulis crash ke `D:\My Project\NexaPlay\crash.txt`, tetapi `run_nexaplay.bat` sebelumnya membaca file lain di folder `bin`, sehingga output crash bisa stale/tidak relevan.
  - `run_nexaplay.bat` diperbaiki agar membaca `D:\My Project\NexaPlay\crash.txt` dan ikut menyalin tail `%LOCALAPPDATA%\NexaPlay\nexaplay.log` ke `nexaplay_crash_context.log`.
  - `MainWindow.xaml.cs` ditambah breadcrumb log `LicenseFlow` untuk alur startup: first activation, status offline license, hasil `ValidateExistingAsync`, buka/tutup overlay validasi, dan transisi ke activation overlay.
- Build: Pass (`Debug x64`, `0 Error(s)`, `0 Warning(s)`).
- Next: Repro ulang skenario aktivasi -> banned -> tutup app -> run pertama, lalu baca `nexaplay_crash_context.log` baru karena sekarang harus sudah memuat trace `LicenseFlow` yang relevan.
```

```text
Tanggal: 7 Juni 2026
- Fokus: Crash startup pertama setelah license dibanned
- Perubahan:
  - Root cause diidentifikasi pada `ValidatingLicenseOverlay` yang masih memakai `Image Source="/Assets/logo.svg"` langsung di `MainWindow.xaml`.
  - Saat license offline masih valid lalu hasil validasi online mengembalikan `Banned`, app menampilkan overlay validasi ini tepat sebelum cache license dibersihkan, sehingga WinUI 3 memicu crash fatal `0xc000027b` pada first-run.
  - Logo overlay validasi diubah ke `<SvgImageSource UriSource="ms-appx:///Assets/logo.svg"/>` agar konsisten dengan perbaikan crash SVG sebelumnya di `LicenseOverlay`.
- Build: Pass (`Debug x64`, `0 Error(s)`, `0 Warning(s)`).
- Next: Uji ulang skenario aktivasi -> banned -> tutup app -> run pertama untuk memastikan overlay validasi tidak crash lagi dan startup berikutnya tetap aman.
```

```text
Tanggal: 7 Juni 2026
- Fokus: Bug Fix Crash saat Lisensi Banned (0xc000027b)
- Perubahan: 
  - Membungkus operasi UI paska `_licenseService.ValidateExistingAsync()` dengan `DispatcherQueue.TryEnqueue` di `MainWindow.xaml.cs`.
  - Crash `NexaPlay exited with code: -1073741189` terjadi karena `HttpClient` callback kembali ke background thread dan XAML throw `RPC_E_WRONG_THREAD`.
  - Log crash `GamesPage` yang dilihat sebelumnya ternyata log lama dari cache `crash.txt` karena fast-fail WinUI 3 mem-bypass `UnhandledException`.
- Build: Pass.
- Next: Uji coba flow lisensi (ban, input baru) dan melanjutkan migration parity.
```

```text
Tanggal: 7 Juni 2026
- Fokus: Infrastruktur Lisensi Offline (WMI + AES) & UI MainWindow
- Perubahan: 
  - Membuat `DeviceIdHelper.cs` (WMI Win32_BaseBoard & Win32_BIOS, di-hash SHA-256).
  - Membuat `LicenseStore.cs` (AES-256 GCM) untuk menyimpan lisensi secara lokal yang terikat dengan device ID.
  - Memperbarui `LicenseService.cs` untuk mengintegrasikan validasi Supabase dengan penyimpanan offline.
  - Menambahkan `ValidatingLicenseOverlay` di `MainWindow.xaml` dan routing navigasi saat *banned*.
  - Menyambungkan *copy to clipboard* di `SettingsPage`.
- Build: Success (dengan beberapa warning WinRT yang diabaikan).
- Next: Menstabilkan flow startup dan error handling saat offline.
```

### 2026-06-07 (Batch : License Parity dengan GameHub)
- Fokus: Parity penuh alur aktivasi license NexaPlay dengan GameHub (WMI DeviceID, AES Encryption, Online Validation response parsing, dan ValidatingLicenseOverlay di startup).
- Perubahan:
  - `LicensePlan.cs`: Menambah status `NotFound` dan `Reset`.
  - `DeviceIdHelper.cs`: Re-write algoritma agar match dengan WMI WQL GameHub (ProcessorId, MotherboardSerial, UUID + SHA256 lowercase hex). Menambahkan dependensi `System.Management`.
  - `LicenseStore.cs`: Menambah AES encryption/decryption menggunakan secret dari GameHub. Menambah fallback path load dari directory GameHub lama untuk kemudahan migrasi.
  - `LicenseService.cs`: Memperbaiki parsing response dari Supabase RPC (`status: "success"`) dan parse `message` (banned, not_found, wrong_device, reset). Offline cleanup saat auto-validation gagal. Menambah `ValidateExistingAsync()`.
  - `MainWindow.xaml`: Menambahkan `ValidatingLicenseOverlay` dengan progress ring dan penanganan error timeout/koneksi saat load aplikasi awal.
  - `MainWindow.xaml.cs`: Mengubah `ValidateLicenseAsync` untuk selalu mencoba `ValidateExistingAsync` sesudah load offline, lalu redirect ke form aktivasi jika banned/tidak valid.
  - `SettingsPage.xaml`: Menyambungkan _License Information_ (Plan, Key, Device ID) dengan DataBinding ke `SettingsViewModel.CurrentLicense`.
  - `SettingsPage.xaml.cs`: Menambahkan fungsi copy ke Windows Clipboard untuk License Key dan Device ID.
- Build: Pass (0 Error, 0 Warning) menggunakan MSBuild.
- Next: Lanjut merapikan UI / menyambungkan fitur selanjutnya di Settings Page.

### 2026-06-07 (Batch : Load Games JSONs on Settings Page)
- Fokus: Menambahkan logika dan trigger pada halaman Settings untuk mengunduh semua data JSON terbaru, disertai UI Progress Overlay dan opsi Clear Cache/Data.
- Perubahan:
  - Menyambungkan tombol "Load Games" pada `SettingsPage.xaml` dengan event click `OnLoadGamesClicked`.
  - Menambahkan metode `RefreshDynamicSourcesAsync` pada `IMetadataService` agar HANYA mengambil file json yang spesifik tanpa file katalog raksasa (`steam_data.json`).
  - Menambahkan dukungan laporan progres `IProgress<double>` ke `RefreshDynamicSourcesAsync` yang membagi persentase unduhan untuk tiap file.
  - Menambahkan `LoadGamesAsync` pada `SettingsViewModel` untuk mengambil file json terbaru dari repository GitHub, HANYA meliputi `appid_populer.json`, `fix_games.json`, `new_fix_games.json`, `override_data.json`, `steam_games.json`, dan `nexaplay_override.json` sesuai permintaan pengguna.
  - Mengimplementasikan `LoadingOverlay` bergaya dialog monokrom di `SettingsPage.xaml` yang ter-binding ke `DownloadProgress` (berupa persentase 0-100%).
  - Memperbarui `LoadGamesAsync` dan `ClearMetadataCacheAsync` pada ViewModel agar mengembalikan Tuple `(bool Success, string Message)` untuk mengakomodasi penanganan *status* (success, network error, exception) secara komprehensif.
  - Memunculkan *Result Dialog* setelah proses Load Games selesai untuk memberitahu pengguna akan semua *case* yang terjadi (berhasil/gagal beserta alasannya).
  - Menghapus penggunaan `ClearCacheAsync` pada proses unduhan spesifik agar katalog dasar (`steam_data.json.gz`) tidak terhapus.
  - Memfungsikan tombol "Clear Cache" yang memanggil fungsi pembersihan cache aman, kemudian menampilkan *Result Dialog* konfirmasi bahwa penghapusan berhasil.
  - Mengimplementasikan fitur "Clear Data" (`ClearAllDataAndRestartAsync`) yang menampilkan dialog konfirmasi lalu menghapus `runtime_catalog_sources` serta file esensial secara menyeluruh (reset pabrik), dilanjutkan *restart* aplikasi.
  - Merombak *styling* tombol *Primary* (Aksi Utama) pada dialog konfirmasi Clear Data agar selaras dengan tema (menjadi putih terang, dengan *hover state* yang semi-transparan).
  - Merombak total arsitektur Aktivasi Lisensi. Menghapus `LicenseActivationDialog.xaml` (karena sifat bawaan *ContentDialog* yang selalu menutupi `TitleBar`) dan memindahkannya langsung ke dalam `MainWindow.xaml` sebagai `LicenseOverlay` (Grid).
  - Melakukan integrasi `LicenseOverlay` dengan menyembunyikan *sidebar* sementara saat overlay aktif, sehingga memberikan pengalaman visual *Full Page* yang memenuhi *ContentFrame* secara elegan namun tetap membiarkan `AppTitleBar` (Logo NexaPlay dan tombol Window) tetap terang dan tidak tertutup.
- Build: Build succeeded (0 Error(s), 0 Warning(s)) dengan MSBuild.
- Next: Menambahkan data binding untuk *fields dummy* di halaman Settings, atau merealisasikan instruksi UI page lainnya.

### 2026-06-02 (Batch : AOT Pre-Fetching Metadata R2)
- Fokus: Mengimplementasikan Background Cache Pre-warming (AOT) untuk metadata Game.
- Perubahan:
  - Membuat `PreFetchNextPopularGamesBackgroundAsync` di `HomeViewModel.cs` untuk mengunduh 2 batch ke depan secara senyap.
  - Membuat `PreFetchNextPagesBackgroundAsync` di `GamesViewModel.cs` untuk mengunduh halaman ke-2 dan ke-3 saat pengguna berada di halaman ke-1.
  - Memanfaatkan *SemaphoreSlim(4, 4)* agar unduhan background tidak mencekik *bandwidth* pengguna.
- Build: Build succeeded (0 Error(s), 0 Warning(s)) dengan MSBuild.
- Next: Menunggu arahan pengguna untuk fitur atau optimalisasi selanjutnya.

### 2026-06-02 (Batch : Migrasi Cloudflare R2 untuk Detail Metadata)
- Fokus: Mengganti fallback API Steam/SteamGridDB yang lambat menjadi Cloudflare R2.
- Perubahan:
  - Mengubah fungsi inti di `SteamStoreService.cs` menjadi tarikan HTTP GET langsung ke URL R2 (`/Metadata/{appId}.json`).
  - Menghapus lebih dari 500 baris logika kompilasi raw JSON karena data R2 sudah ter-merge sesuai skema sebelumnya.
  - Memastikan halaman Home dan GamesPage menerima cover `library_capsule` dan hero dengan efisiensi O(1) fetch.
- Build: Build succeeded (`0 Error(s)`, `0 Warning(s)`) menggunakan MSBuild.
- Next: Menunggu perintah fitur selanjutnya.

### 2026-06-01 (Batch : Fix Error 153 YouTube via Local Wrapper + Virtual Host)
- Fokus: Menuntaskan error `153` pada tutorial video Bypass tanpa mengubah tampilan UI overlay yang sudah diset.
- Perubahan:
  - Menambahkan wrapper HTML lokal `Assets/Web/youtube-player.html` yang memuat YouTube IFrame API.
  - `WebView2` di `BypassGameDetailPage` sekarang memuat wrapper lewat virtual host mapping:
    - host: `appassets.example`
    - URL player: `https://appassets.example/youtube-player.html?videoId=...`
  - Menghapus pola load langsung embed/NavigateToString untuk player utama agar origin/referrer valid.
  - Tetap mempertahankan overlay video, ukuran, tombol close, dan stop playback saat close/back/unload (UI sama seperti sebelumnya).
  - Menambahkan wrapper HTML sebagai content yang di-copy ke output build (`PreserveNewest`).
- Build: `Build succeeded` (`0 Error(s)`, `0 Warning(s)`) pada `Debug x64` dengan `OutDir=Debug-preview`.
- Next: Validasi runtime 2 skenario:
  1) video embeddable harus play normal in-app tanpa `153`,
  2) video non-embeddable tetap fallback aman.

### 2026-06-01 (Batch : Tutorial Video Bypass via youtube.json + ETag)
- Fokus: BypassGameDetail tutorial video memakai metadata `youtube.json` (remote update + ETag) dan player overlay besar (parity feel seperti modal media GameDetail).
- Perubahan:
  - Tambah service baru `IBypassTutorialVideoService` + `BypassTutorialVideoService` untuk load `youtube.json` dari repo override dengan cache lokal + validasi `If-None-Match` / `ETag`.
  - Tambah model `BypassTutorialVideo` serta konstanta baru:
    - `YoutubeTutorialUrl`
    - `YoutubeTutorialCacheFileName`
    - `YoutubeTutorialEtagFileName`
  - `BypassGameDetailViewModel` sekarang menarik metadata tutorial video per game/kategori (`byAppId` -> `byCategory` -> `default`) dan expose:
    - `TutorialVideoTitle`
    - `TutorialVideoEmbedUrl`
    - `TutorialVideoWatchUrl`
    - `TutorialVideoThumbnailUrl`
  - `BypassGameDetailPage`:
    - hapus embed hardcoded di kotak kecil,
    - thumbnail tetap ringan di halaman,
    - klik thumbnail membuka `WebView2` overlay besar (modal) agar nyaman ditonton,
    - saat ditutup/back/unload otomatis `Navigate("about:blank")` agar video berhenti (tidak lanjut muter di background),
    - konfigurasi WebView2 diperkecil overhead UI (status bar/context menu/devtools dimatikan).
- Build: `Build succeeded` (`0 Error(s)`, `0 Warning(s)`) pada `Debug x64` dengan `OutDir=Debug-preview`.
- Next: Uji runtime langsung beberapa video dari `youtube.json` untuk verifikasi:
  1) update remote terdeteksi tanpa rebuild,
  2) close overlay menghentikan audio/video,
  3) tidak ada lagi Error 153 pada video valid embeddable.

### 2026-06-01 (Batch : Penutupan Gap Parity Alur Fix 3rd-Party)
- Fokus: Menutup gap parity lanjutan alur fix 3rd-party di Bypass Detail agar semakin setara dengan pola GameHub.
- Perubahan:
  - Menambahkan konfirmasi antivirus pihak ketiga sebelum lanjut proses (user bisa lanjutkan atau batalkan).
  - Menambahkan fallback manual path selection menggunakan `FolderPicker` saat path instalasi game tidak terdeteksi otomatis.
  - Menambahkan dukungan extract non-zip melalui fallback `7z` CLI (ZIP tetap lewat extractor native).
  - Menambahkan auto-create shortcut desktop setelah sukses fix saat `use_shortcut=true`, dengan prioritas `exe_hint`.
  - Menambahkan akses window aktif di `App.xaml.cs` (`MainWindowInstance`) untuk inisialisasi picker.
- Build: `Build succeeded` (`0 Error(s)`, `0 Warning(s)`) pada `Debug x64`.
- Next: QA runtime end-to-end untuk skenario:
  1) third-party AV terdeteksi (batal vs lanjut),
  2) auto path gagal lalu pilih manual folder,
  3) archive non-zip dengan/ tanpa `7z`,
  4) shortcut creation saat `use_shortcut=true`.

### 2026-06-01 (Batch : Implementasi Alur Fix 3rd-Party di Bypass Detail)
- Fokus: Menerapkan alur proses fix untuk kategori `3rd-party` pada `BypassGameDetail` dan memastikan `Aktivasi Offline` baru memulai proses setelah user menekan dialog **Lanjut Bypass**.
- Perubahan:
  - `BypassGameDetailViewModel`:
    - Mengubah `StartBypassGameCommand` dari placeholder menjadi proses async ber-step:
      1) check antivirus aktif,
      2) detect install path Steam,
      3) tambah exclusion Defender,
      4) download file fix,
      5) extract,
      6) replace file game,
      7) cleanup.
    - Menambahkan state UI proses: `IsBypassProcessing`, `BypassProgressPercent`, `BypassProgressMessage`, `BypassErrorMessage`.
    - Menambahkan guard kategori: `Steam Account/Steam Sharing` tidak menjalankan alur fix 3rd-party.
  - `BypassGameDetailPage.xaml`:
    - Tombol `Mulai Bypass Game` sekarang disable saat proses berjalan.
    - Menambahkan progress bar + pesan progress + pesan error di bawah tombol aksi.
  - `BypassGameDetailPage.xaml.cs`:
    - Untuk badge `Aktivasi Offline`, proses fix benar-benar dimulai saat user klik **Lanjut Bypass** di dialog.
    - Untuk non-offline langsung jalankan command async.
- Build: `Build succeeded` (`0 Error(s)`, `0 Warning(s)`) pada `Debug x64`.
- Next: parity lanjutan agar 100% setara GameHub: manual path picker saat game tidak terdeteksi, confirm antivirus pihak ketiga (lanjut/batal), dukungan extract non-zip (jika source bukan zip), serta auto-create shortcut berbasis `exe_hint`.

### 2026-06-01 (Batch : UX Dialog Add Game + Case Denuvo + Polish Dialog Remove)
- Fokus: Menambahkan feedback proses `Add Game` yang jelas untuk user (seperti GameHub) dengan tema monokrom NexaPlay.
- Perubahan:
  - Menambahkan dialog proses native di `GameDetailPage` dengan judul **Mengunduh & Memasang**.
  - Dialog proses menampilkan progres 0-100% dan status fase ramah user:
    - mengunduh,
    - memverifikasi,
    - memasang,
    - selesai/gagal.
  - Menambahkan guard case Denuvo sebelum `Add Game`:
    - jika game Denuvo dan tidak ada di `Bypass Games` (gabungan cek `fix_games` + `steam_games`),
    - tampil dialog informasi user-friendly bahwa game belum tersedia di Bypass Games.
  - Mengganti dialog remove-blocked dari `ContentDialog` menjadi overlay custom agar styling konsisten:
    - efek lighting/glow lebih tebal,
    - visual hitam-putih,
    - tombol `Mengerti` dengan hover transparan monokrom (tanpa biru sistem).
  - Menambahkan `RemoveGameResult` untuk komunikasi hasil remove yang lebih eksplisit dari service ke ViewModel/UI.
- Build: Pending (jalankan build gate setelah patch ini).
- Next: Verifikasi runtime skenario Add sukses/gagal/cancel dan skenario Denuvo-unavailable agar pesan UI sesuai ekspektasi.

### 2026-06-01 (Batch : Parity UX Add/Remove GameDetail + Dialog Installed)
- Fokus: Menyamakan UX `Add Game` di GameDetail dengan pola GameHub (toggle tombol + peringatan saat remove diblokir).
- Perubahan:
  - Tombol aksi utama GameDetail sekarang toggle otomatis:
    - belum terpasang script: `Add Game`,
    - sudah terpasang script: `Remove Game` (bukan `Installed` pasif).
  - `GameDetailViewModel` tetap berperan orchestration:
    - command `AddGameCommand` tetap satu pintu,
    - jika `IsGameInstalled=true`, command tersebut otomatis jalankan flow remove,
    - jika remove diblokir karena game masih terinstall di Steam, ViewModel hanya expose state dialog request.
  - `IAddGameService` diubah agar remove mengembalikan hasil terstruktur (`RemoveGameResult`) dengan flag `BlockedByInstalledGame`.
  - `AddGameService` menerapkan parity check `appmanifest_*`:
    - jika appmanifest ada, remove diblokir dan hasil error dikembalikan ke ViewModel.
  - `GameDetailPage.xaml.cs` menambahkan dialog dark-theme monokrom saat remove diblokir:
    - title: `Game Masih Terinstall`,
    - pesan: minta uninstall dari Steam dulu sebelum remove script.
- Build: Pending (jalankan MSBuild gate setelah patch ini).
- Next: Uji runtime alur 2 skenario:
  1) Add sukses -> tombol berubah jadi Remove Game.
  2) Remove saat appmanifest masih ada -> dialog blokir tampil sesuai tema NexaPlay.

### 2026-06-01 (Batch : Parity Add Game GameDetail dengan GameHub)
- Fokus: Menerapkan logika `Add Game` di `GameDetailPage` agar sedekat mungkin dengan flow GameHub (source API berantai + install script).
- Perubahan:
  - Port ulang `Infrastructure/Services/AddGameService.cs` berbasis flow GameHub:
    - baca `api.json` dan iterasi `api_list` berurutan (`enabled`, `success_code`, `unavailable_code`),
    - fallback antar sumber API sampai ada hasil,
    - progress fase `start/download/validate/install/done`,
    - validasi file ZIP (`PK` magic bytes),
    - install `.lua` + extract `.manifest` ke `depotcache`,
    - comment out `setManifestid(...)` seperti behavior GameHub,
    - dukungan cancel internal per `appid` melalui token map.
  - Kontrak service `IAddGameService` ditambah `CancelAdd(string appId)` agar lifecycle proses tetap ditangani di layer service (SOLID).
  - Menambahkan file `data/api.json` ke NexaPlay (disalin dari GameHub) dan set `CopyToOutputDirectory=PreserveNewest`.
  - `GameDetailViewModel` tetap hanya orchestration UI; timeout paksa 60 detik dihapus agar flow runtime `Add Game` tidak memutus fallback chain lebih cepat dari GameHub.
- Build: Pending (jalankan MSBuild gate setelah batch ini).
- Next: Validasi runtime klik `Add Game` pada beberapa appid (sukses, gagal, dan cancel), lalu sinkronkan detail status/parity matrix jika diperlukan.

### 2026-05-27 (Batch : UI Settings Page)
- Fokus: UI Settings Page
- Perubahan: 
  - Mengganti layout `SettingsPage.xaml` menjadi desain monokrom (*clean enterprise theme*) sesuai aturan UI NexaPlay tanpa menggunakan emoji.
  - Menerapkan data *dummy* secara *hardcode* dalam XAML untuk bagian *License Information*, *Actions*, dan *Application Update* tanpa menyentuh struktur backend.
  - Menggunakan ikon-ikon native Segoe Fluent (`FontIcon`) yang mewakili masing-masing aksi dan kategori data dengan elegan.
- Build: Pass (0 Error, 0 Warning)
- Next: Menyambungkan *fields dummy* pada *Settings Page* ke data backend atau menunggu instruksi page selanjutnya.

### 2026-05-27 (Batch : Perbaikan Detail UI Steam Sharing & Error Video)
- Fokus: Memperbaiki UI pada section Steam Sharing yang tidak sesuai instruksi dan memperbaiki masalah pemutaran YouTube (Error 153).
- Perubahan: 
  - Mengubah warna catatan dan ikon peringatan kembali ke monokrom putih/hitam (menghapus emoji berwarna).
  - Menambahkan divider putih halus pada atas dan bawah teks Catatan.
  - Memisahkan seksi Video YouTube menjadi blok "Tutorial Video" tersendiri.
  - Mengubah teks pelaporan akun dan merubah behavior tombol *Copy* untuk berubah menjadi teks "Copied!" dengan *background* transparan saat ditekan (serta menghapus efek *hover* biru bawaan sistem).
  - Mengubah UI "Alt + F4" menjadi bentuk *code block* menyatu dengan teks menggunakan `InlineUIContainer`.
  - Mengatasi Error 153 (Playback Restricted) YouTube WebView2 dengan menyuntikkan origin standar.
- Build: Success
- Next: Menambahkan logika untuk menekan tombol "Mulai Proses Bypass" (Aktivasi Offline) dan "Mainkan Game" (Steam Sharing) menuju ke eksekusi game.

### 2026-05-27 (Batch : Implementasi UI Dialog Khusus Aktivasi Offline & Konsistensi Cover Art)
- Fokus: UI Dialog Khusus Aktivasi Offline & Konsistensi Cover Art (BypassGameDetailPage)
- Perubahan:
  - Mengubah logika section 3rd-party (Mulai Bypass & info card) agar tetap muncul walau game berlabel "Aktivasi Offline" (`ShowThirdPartySection`).
  - Menambahkan *ContentDialog* khusus "Aktivasi Offline" murni bertema hitam-putih. Tidak ada warna aksen biru/merah/kuning.
  - Memperbaiki layout teks petunjuk menggunakan `Grid` 2 kolom untuk mencapai *hanging indent* rata kiri yang rapi dengan simbol hubung tebal (`-`).
  - Menimpa style bawaan WinUI: Checkbox `Checked` dan tombol `Primary` di-*override* berlatar putih dengan font/tick hitam, termasuk pada state `:Hover` (`#E5E5E5`) dan `:Pressed` (`#CCCCCC`).
  - Menyelaraskan logika `CoverArtUrl` di halaman Detail dengan logika *enrichment* 4-level fallback dari BypassGamesPage (memilih `BypassEntry.PosterUrl` sebagai prioritas pertama).
- Build: Pass
- Next: Menuggu Instruksi lebih lanjut dari user.


### 2026-05-26 (Batch : Replikasi UX GameHub Fix Games)
- Fokus: UI BypassGamesPage (Replikasi UX GameHub Fix Games)
- Perubahan: 
  - Update `GameCategory` enum dan `FixEntry` model (tambah AktivasiOffline & SteamSharing).
  - Rewrite `BypassGamesDataService` untuk memparsing `steam_games.json` dan properti baru.
  - Rewrite `BypassGamesViewModel` dengan logika filter (Kategori, Standard/Premium, Search) dan cover enrichment 4 level fallback.
  - Rewrite `BypassGamesPage.xaml` dan code-behind dengan layout responsif (ItemsWrapGrid), custom badge biru untuk AKTIVASI OFFLINE dan STEAM SHARING, badge Premium/Standard.
- Build: Pass
- Next: Menambahkan navigasi dari item ke detail, dan menguji layout di runtime saat membuka halaman BypassGames.

### 2026-05-26 (Batch: BypassGameDetailPage)
- Fokus: Membuat halaman detail khusus untuk Bypass Games (BypassGameDetailPage) — terpisah dari GameDetailPage.
- Perubahan:
  - **[NEW] `BypassGameDetailViewModel.cs`**: ViewModel baru dengan pipeline fetch metadata identik GameDetailViewModel (IMetadataService → ISteamStoreService → INexaPlayOverrideService). Prioritas fallback hero/icon sama persis.
  - **[NEW] `BypassGameDetailPage.xaml`**: Layout XAML menduplikasi bagian atas GameDetailPage (Hero banner, gradient overlay, deskripsi, genre tags, metadata row: APP ID / RELEASE DATE / DEVELOPER / PUBLISHER / PRICE, badge PREMIUM/STANDARD/DENUVO, tombol CEK BYPASS). Bagian MEDIA ke bawah dikosongkan sesuai instruksi.
  - **[NEW] `BypassGameDetailPage.xaml.cs`**: Code-behind dengan SafeUri, BoolToVis, Denuvo pulse animation, responsive hero sizing.
  - **[MODIFY] `App.xaml.cs`**: Register `BypassGameDetailViewModel` di DI container.
  - **[MODIFY] `BypassGamesPage.xaml.cs`**: Navigasi `OnGameCardClicked` diubah dari `GameDetailPage` → `BypassGameDetailPage`.
- Build: ✅ Sukses (0 Error, 0 Warning)
- Next: Mengisi konten bagian bawah BypassGameDetailPage (aksi bypass, status, dsb).

Tanggal: 26 Mei 2026
- Fokus: Hero Skeleton/Shimmer BypassGameDetailPage agar tidak muncul layar hitam saat metadata/image hero masih memuat.
- Perubahan:
  - Menambahkan overlay skeleton khusus area hero di BypassGameDetailPage.xaml (base placeholder + shimmer sweep), terpisah dari shimmer global page.
  - Menambahkan event ImageOpened/ImageFailed pada HeroImage untuk menyembunyikan skeleton hanya saat image sudah siap/selesai fallback.
  - Menambahkan kontrol lifecycle shimmer hero di BypassGameDetailPage.xaml.cs (show saat HeroBackgroundUrl berubah atau IsDetailLoading aktif, hide saat image selesai load, update range saat resize).
- Build: Pass (0 Error, 0 Warning).
- Next: Validasi runtime dengan koneksi lambat/revisit cepat untuk memastikan transisi hero halus tanpa flash hitam.

Tanggal: 26 Mei 2026
- Fokus: Perkuat efek putih Shimmer/Skeleton dan terapkan ke GameDetailPage untuk hero + media gambar metadata.
- Perubahan:
  - `BypassGameDetailPage`: intensitas skeleton putih diperjelas (base putih + sweep lebih terang) agar loading hero tidak terlihat hitam.
  - `GameDetailPage.xaml`: tambahkan skeleton overlay + shimmer sweep untuk image hero utama, card MEDIA carousel, blok GAME OVERVIEW screenshots, dan POST-ABOUT screenshots.
  - `GameDetailPage.xaml`: semua image metadata terkait diberi event `Loaded/ImageOpened/ImageFailed` + `Opacity` awal 0 agar reveal terjadi setelah image siap.
  - `GameDetailPage.xaml.cs`: tambah lifecycle shimmer per-overlay (start/stop), auto-hide skeleton saat image loaded/failed, dan cleanup storyboard saat page keluar.
- Build: Pass (0 Error, 0 Warning).
- Next: Uji runtime skenario jaringan lambat dan navigasi cepat untuk memastikan tidak ada flash hitam dan shimmer hilang tepat saat image siap.

Tanggal: 26 Mei 2026
- Fokus: Hilangkan placeholder broken image (ikon silang) pada GameDetail/BypassGameDetail saat gambar metadata gagal lalu terlambat berhasil.
- Perubahan:
  - Analisis: terjadi race `ImageFailed` lalu `ImageOpened`/URL update, sehingga glyph broken-image sempat terlihat sebelum cover final muncul.
  - `GameDetailPage.xaml.cs`: saat `ImageFailed` kini image di-`Collapsed`, skeleton tetap tampil, shimmer dihentikan; saat `ImageOpened` image direveal kembali.
  - `GameDetailPage.xaml`: top icon sekarang memakai pipeline skeleton/shimmer + event load/opened/failed yang sama agar ikon silang tidak muncul.
  - `BypassGameDetailPage.xaml/.cs`: top icon juga ditambahkan skeleton/shimmer + fail handling collapse image; hero fail handling disamakan.
- Build: Pass (0 Error, 0 Warning).
- Next: Retest runtime pada game yang metadata art-nya sering fallback/terlambat (hero + top icon) untuk memastikan tidak ada frame ikon silang sama sekali.

Tanggal: 26 Mei 2026
- Fokus: Khusus BypassGameDetailPage â€” hapus badge Denuvo, tampilkan badge Aktivasi Offline/Steam Sharing dari source bypass yang benar.
- Perubahan:
  - `BypassGameDetailViewModel` tidak lagi membaca badge dari `GameEntry` (karena field tidak ada).
  - Menambahkan properti `BypassEntry` (`FixEntry`) dan resolver lintas source bypass (`fix_games`, `steam_games`, `new_fix_games`) berdasarkan `AppId`.
  - Binding badge di `BypassGameDetailPage` sekarang valid: `AKTIVASI OFFLINE` dan `STEAM SHARING` membaca dari `BypassEntry`.
  - Denuvo badge tetap dihapus khusus halaman detail bypass sesuai arahan.
- Build: Pass (setelah perbaikan compile error CS1061).
- Next: Validasi runtime beberapa game campuran (steam sharing + non-sharing) untuk memastikan badge tampil konsisten tanpa memengaruhi page detail utama.

Tanggal: 26 Mei 2026
- Fokus: BypassGameDetailPage - hapus tombol CEK BYPASS dari metadata strip.
- Perubahan:
  - Menghapus elemen Button CEK BYPASS di BypassGameDetailPage.xaml (termasuk ikon dan tooltip) agar area metadata lebih bersih sesuai arahan.
  - Tidak mengubah behavior inti data/detail game dan tidak menyentuh halaman lain.
- Build: Build checkpoint akan mengikuti setelah batch edit ini.
- Next: Lanjut evaluasi elemen UI lain di BypassGameDetailPage sesuai prioritas user.
Tanggal: 26 Mei 2026
- Fokus: Sinkronisasi badge BypassGameDetailPage agar selalu sama dengan card yang diklik.
- Perubahan:
  - Root cause dianalisis: detail page resolve ulang status berdasarkan ppid dengan prioritas source berbeda dari card (ix_games bisa menimpa steam_games), sehingga label bisa salah.
  - BypassGamesPage.xaml.cs: navigasi detail sekarang mengirim payload (appId, FixEntry terpilih) dari card aktif.
  - BypassGameDetailPage.xaml.cs: OnNavigatedTo menerima payload tuple dan meneruskan FixEntry itu ke ViewModel.
  - BypassGameDetailViewModel.cs: LoadAsync overload baru menerima preferredBypassEntry; jika ada, dipakai sebagai source utama badge. Fallback resolver tetap ada, dengan prioritas steam_games -> fix_games -> new_fix_games.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: Validasi runtime silang-tab (Semua vs Steam Sharing) pada appid yang sama untuk pastikan badge konsisten dengan card asal klik.
Tanggal: 26 Mei 2026
- Fokus: Standardisasi badge Bypass (tanpa ikon, label Akun Steam, dan warna tosca Aktivasi Offline).
- Perubahan:
  - BypassGamesPage.xaml: label kategori UI Steam Sharing diubah menjadi Akun Steam (tag tetap steam-sharing).
  - BypassGamesPage.xaml: badge card STEAM SHARING diubah jadi AKUN STEAM.
  - BypassGamesPage.xaml: badge AKTIVASI OFFLINE di card diubah ke tema tosca (#0F766E + border #5EEAD4).
  - BypassGameDetailPage.xaml: menghapus FontIcon pada badge status (tanpa ikon), mengubah label STEAM SHARING jadi AKUN STEAM, dan menyamakan warna tosca untuk AKTIVASI OFFLINE.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: Validasi runtime visual untuk memastikan badge card dan detail konsisten pada game dengan status steam-sharing maupun ktivasi_offline.
Tanggal: 26 Mei 2026
- Fokus: Dokumentasi naming parity untuk status Steam Sharing di area Bypass.
- Perubahan:
  - Menambahkan catatan eksplisit bahwa istilah UI Steam Sharing diganti menjadi Akun Steam (display only).
  - Menegaskan label UI card/detail memakai AKUN STEAM, sementara tag/source backend tetap steam-sharing.
  - Menambahkan catatan yang sama di MIGRATION_PARITY_MATRIX.md bagian Bypass agar konsisten lintas dokumen.
- Build: Tidak diperlukan (perubahan dokumentasi saja).
- Next: AI berikutnya mengikuti naming ini agar tidak terjadi mismatch istilah UI vs source data.
Tanggal: 26 Mei 2026
- Fokus: BypassGameDetailPage skenario 1 (tanpa status tambahan) - isi konten bawah metadata.
- Perubahan:
  - Menambahkan section Informasi Penting khusus kondisi default/no-status di bawah row metadata (BypassGameDetailPage.xaml).
  - Menambahkan 3 kartu informasi monokrom elegan (Antivirus, Windows Update, Laporkan Masalah) dengan ikon putih (tanpa emoji, tanpa warna-warni ala GameHub).
  - Menambahkan tombol putih Mulai Bypass Game dengan hover/pressed state yang jelas dan tetap clean.
  - BypassGameDetailViewModel: tambah properti ShowDefaultNoStatusSection (aktif jika bukan AKTIVASI OFFLINE dan bukan AKUN STEAM) serta command placeholder StartBypassGameCommand untuk checkpoint tahap ini.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: Lanjut skenario status berikutnya (Aktivasi Offline / Akun Steam / kombinasi) dengan pola layout yang konsisten.
Tanggal: 26 Mei 2026
- Fokus: Bedah sidebar Bypass menjadi 3rd Party vs Akun Steam + scope konten detail default.
- Perubahan:
  - BypassGamesPage: kategori dipisah menjadi dua dropdown default terbuka (`3rd Party` dan `Steam Sharing`), dengan isi:
    - `3rd Party`: Semua, Ubisoft, EA, Rockstar, PlayStation, Other.
    - `Steam Sharing`: satu entry `Akun Steam` (tanpa subkategori tambahan).
  - BypassGamesViewModel: filter source dipisah tegas:
    - `steam-sharing` hanya membaca `_steamGames`.
    - area `3rd Party` membaca `_allFixes` dan mengecualikan item steam (`!IsSteamType`).
  - BypassGameDetailViewModel: section informasi default/no-status sekarang hanya tampil untuk non-steam tanpa Aktivasi Offline, sehingga akun steam tidak menampilkan konten info default yang khusus 3rd party.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: Validasi runtime lintas dropdown untuk memastikan list dan konten detail selalu sesuai status sumber.
Tanggal: 26 Mei 2026
- Fokus: Koreksi arsitektur dropdown Bypass ke sidebar utama (bukan di body halaman).
- Perubahan:
  - `BypassGamesPage` dikembalikan ke desain kategori seperti sebelumnya (bar kategori horizontal di area halaman, bukan sidebar kiri internal page).
  - `MainWindow` sidebar item `Bypass` sekarang punya submenu default drop-down saat aktif: `3rd Party` dan `Steam Sharing`.
  - Klik submenu sidebar mengirim parameter kategori ke `BypassGamesPage` (`all` atau `steam-sharing`) agar filter langsung sesuai.
  - Submenu Bypass hanya tampil saat pane sidebar terbuka dan menu `Bypass` sedang aktif agar tetap rapi.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: QA runtime pada interaksi hover sidebar + navigasi submenu untuk memastikan state aktif dan filter tetap sinkron.
Tanggal: 26 Mei 2026
- Fokus: Finalisasi pemisahan sidebar Bypass (3rd Party vs Steam Sharing) + auto dropdown elegan.
- Perubahan:
  - `BypassGamesPage`: kategori `Akun Steam` dihapus dari bar kategori 3rd Party agar tidak bercampur.
  - `MainWindow` sidebar: submenu Bypass sekarang auto-drop saat sidebar terbuka (tanpa perlu klik Bypass dulu), dengan ikon panah bawah pada item Bypass.
  - Desain submenu diperbarui jadi panel tersambung (single container) berisi dua item: `3rd Party` dan `Steam Sharing`, lebih rapi dan konsisten.
  - Klik submenu `3rd Party`/`Steam Sharing` tetap mengarahkan filter ke kategori yang benar (`all` / `steam-sharing`) dan state aktif visual ikut berpindah.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: Validasi runtime spacing/hover/sidebar collapse-expand untuk polish akhir visual.
Tanggal: 26 Mei 2026
- Fokus: Polish UX dropdown Bypass + final scope kategori Steam Sharing.
- Perubahan:
  - Posisi chevron dropdown Bypass digeser ke kiri agar tidak terlalu menempel pojok kanan.
  - Submenu `3rd Party` dan `Akun Steam` diberi ikon masing-masing (putih, non-emoji) untuk keterbacaan.
  - Halaman Bypass sekarang mode-aware:
    - Jika di `3rd Party`, tampil kategori 3rd-party (`Semua` s.d. `Other`).
    - Jika di `Steam Sharing`, bar kategori hanya menampilkan satu tombol `Steam Sharing` (tanpa kategori lain).
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: QA runtime visual untuk memastikan state aktif submenu + mode kategori konsisten di semua alur navigasi.
Tanggal: 26 Mei 2026
- Fokus: Konsistensi naming submenu + perbaikan efek search saat switch grup Bypass.
- Perubahan:
  - Sidebar submenu: label item kedua diubah dari `Akun Steam` menjadi `Steam Sharing` (istilah Akun Steam tetap khusus badge).
  - `BypassGamesPage.OnNavigatedTo`: saat navigasi dari submenu sidebar, `SearchQuery` dibersihkan dulu sebelum `SetCategory(...)` agar state pencarian lama tidak ikut memfilter hasil saat berpindah 3rd Party <-> Steam Sharing.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: Validasi runtime bahwa switch submenu selalu menampilkan list default grup tanpa residu pencarian sebelumnya.
Tanggal: 26 Mei 2026
- Fokus: Sinkronisasi final search-switch kategori + badge Steam Sharing di detail.
- Perubahan:
  - `BypassGamesViewModel.SetCategory(...)` sekarang jadi sumber tunggal untuk switch kategori: saat kategori berubah, search otomatis di-reset (tanpa trigger filter ganda) sehingga pindah 3rd Party <-> Steam Sharing tidak mewarisi query lama.
  - `BypassGamesPage` tidak lagi reset search manual di code-behind, agar alur tidak duplikat.
  - `BypassGameDetailViewModel.ShowSteamSharingBadge` diperketat: badge hanya tampil jika `BypassEntry.Category == SteamSharing` (khusus entry yang memang punya field `category: steam-sharing`), bukan untuk semua steam type.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: QA runtime pada sampel steam_games campuran (dengan/ tanpa field category) untuk verifikasi badge detail konsisten dengan card.
Tanggal: 26 Mei 2026
- Fokus: Rapikan UI kategori tanpa pembungkus + immersive detail bypass + hilangkan trigger efek search dari submenu.
- Perubahan:
  - `BypassGamesPage`: wrapper card/border besar pada area kategori dihapus; kini langsung tombol kategori saja sesuai desain ringkas.
  - `MainWindow`: mode immersive (fullscreen tanpa sidebar/top shell) kini berlaku juga untuk `BypassGameDetailPage`, setara `GameDetailPage`.
  - `BypassGamesViewModel.SetCategory(...)`: reset search otomatis saat switch kategori dihapus agar klik submenu `3rd Party/Steam Sharing` tidak lagi memicu perubahan perilaku search.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: QA runtime khusus perpindahan submenu sidebar untuk verifikasi search textbox dan hasil list tetap stabil.
Tanggal: 27 Mei 2026
- Fokus: UI Sidebar Bypass + Layout BypassGameDetailPage (3rd Party no-status section).
- Perubahan:
  - `MainWindow.xaml`: Hapus box/border terluar dropdown Bypass. Submenu sekarang tampil langsung indented di bawah item Bypass (tanpa container terpisah yang terlihat seperti kotak ke-2).
  - `MainWindow.xaml.cs`: Warna aktif/pasif submenu diupdate — aktif memakai `#1AFFFFFF` (transparan putih), ikon dan teks berubah warna mengikuti status aktif (putih = aktif, abu-abu = pasif).
  - `BypassGameDetailPage.xaml`: Section default/no-status (3rd Party tanpa Aktivasi Offline) diubah ke layout 2 kolom — kiri: info cards + tombol, kanan: cover art game (library capsule portrait 258×387).
  - `BypassGameDetailViewModel.cs`: Tambah properti `CoverArtUrl` dengan fallback pipeline: `library_capsule_2x` → `library_600x900_2x` → `LibraryCapsuleUrl` → `PopularCoverImageUrl` → `HeaderImageUrl`.
  - `BypassGameDetailPage.xaml.cs`: Tambah handler `CoverArtImage_ImageOpened/Failed` untuk reveal/hide skeleton cover art.
- Build: MSBuild Debug x64 OutDir=Debug-preview sukses (0 Error, 0 Warning).
- Next: Validasi runtime — cek cover art portrait muncul di kanan untuk game 3rd party tanpa Aktivasi Offline; pastikan tidak mengganggu layout skenario lain (Aktivasi Offline / Steam Sharing).

Tanggal: 1 Juni 2026
- Fokus: Polishing UX dialog `GameDetail` untuk flow Add Game (tipografi + aksi batal saat proses).
- Perubahan:
  - `GameDetailPage.xaml`: skala tipografi dialog diperkecil agar konsisten dengan tema (`AddGame`, `UiInfo`, `RemoveBlocked`) dan tidak terlalu dominan di layar.
  - `GameDetailPage.xaml`: tombol dialog proses Add Game kini bind dinamis `Batal` saat proses berjalan dan `Mengerti` saat proses selesai/gagal/dibatalkan.
  - `GameDetailViewModel.cs`: tambah orkestrasi cancel Add Game (`_addGameCts`, `IsAddGameCancelRequested`, `HandleAddGameDialogAction`) tanpa memindahkan logika IO dari service.
  - `GameDetailPage.xaml.cs`: handler klik dialog Add Game diarahkan ke `HandleAddGameDialogAction`.
- Build: `dotnet build` Debug x64 dengan OutDir `Debug-preview` sukses (0 Error, 0 Warning).
- Next: QA runtime khusus flow Add Game: tekan `Batal` saat download/install, verifikasi status menjadi batal dan file skrip tidak terpasang.
Tanggal: 1 Juni 2026
- Fokus: Parity `Online-Fix` di `GameDetail` + checklist performa untuk action dialog.
- Perubahan:
  - `GameDetailViewModel.cs`: alur `Online-Fix` diubah jadi state dialog end-to-end:
    - cek ketersediaan,
    - konfirmasi apply,
    - proses download/extract dengan progress,
    - case unavailable / game belum terpasang / gagal / dibatalkan / sukses.
  - `GameDetailViewModel.cs`: menambahkan action dialog `Primary/Secondary` agar tombol `Batal` benar-benar membatalkan proses via `CancellationToken`.
  - `GameDetailPage.xaml`: menambahkan overlay dialog khusus `Online-Fix` bertema monokrom (tidak memakai biru default sistem).
  - `GameDetailPage.xaml.cs`: menambah handler klik dialog `OnlineFixDialogPrimary_Click` dan `OnlineFixDialogSecondary_Click`.
  - `MIGRATION_PARITY_MATRIX.md`: status `Apply fix flow` dan `Online-Fix action` diperbarui menjadi `Done`.
  - `AI_HANDOFF_PROMPT.md`: menambahkan blok `Performance guardrail tambahan` untuk menjaga kelancaran action flow.
- Build: pending verifikasi build gate setelah batch ini.
- Next: QA runtime 5 skenario Online-Fix (available, unavailable, game-not-installed, cancel saat apply, sukses apply lalu unfix).
Tanggal: 1 Juni 2026
- Fokus: Menambahkan fallback source `Online-Fix` dan menjaga parity patch `unsteam.ini`.
- Perubahan:
  - `AppConstants.cs`: menambahkan `OnlineFixFallbackUrl` ke `https://github.com/madoiscool/lt_api_links/releases/download/unsteam/Win64.zip`.
  - `OnlineFixService.CheckAvailabilityAsync(...)`:
    - cek source utama `OnlineFix1/{appid}.zip` tetap prioritas,
    - jika gagal, otomatis probe source fallback.
  - `OnlineFixService.ApplyAsync(...)`:
    - download mencoba source utama lebih dulu,
    - jika tidak sukses, otomatis fallback ke source GitHub `Win64.zip`,
    - hasil tetap diekstrak ke folder game sesuai flow Online-Fix.
  - `OnlineFixService`: menambahkan patch otomatis `unsteam.ini` (`<appid>` -> appid aktual) jika file tersebut ada di hasil ekstraksi.
- Build: pending verifikasi build gate setelah patch fallback ini.
- Next: QA runtime dua jalur source (primary down -> fallback up) dan verifikasi `unsteam.ini` benar terpatch.
Tanggal: 1 Juni 2026
- Fokus: Perbaikan parity `UnOnline-Fix` + error message game belum terinstall + hapus lighting dialog remove-blocked.
- Perubahan:
  - `GameDetailViewModel.cs`:
    - `RemoveFixAsync` tidak lagi tergantung `FixEntry` (yang bisa null), sekarang memakai `Game.AppId` agar `UnOnline-Fix` benar-benar dieksekusi.
    - Menambahkan dialog state khusus unfix: `Menghapus Online-Fix` -> `UnOnline-Fix Berhasil/Gagal`.
    - Progress callback Online-Fix kini menangkap status `Failed` secara eksplisit dan langsung memakai `state.Error`, sehingga case game belum terinstall tidak jatuh ke pesan generik.
  - `OnlineFixService.cs`:
    - Parser unfix log diperketat agar mengabaikan baris metadata (`Date:`, `AppId:`, `Files:`); hanya daftar file fix yang dihapus.
  - `GameDetailPage.xaml`:
    - Menghapus elemen radial glow/lighting pada dialog `Game Masih Terinstall` sesuai request.
- Build: pending verifikasi build gate setelah batch ini.
- Next: QA runtime `UnOnline-Fix` untuk game applied-state lama (FixEntry null) + verifikasi pesan game belum terinstall tampil konsisten.
Tanggal: 1 Juni 2026
- Fokus: UX remove game di Library + sinkronisasi data Library setelah remove dari GameDetail.
- Perubahan:
  - `LibraryPage.xaml.cs`:
    - Konfirmasi remove di Library sekarang setelah sukses menjalankan animasi card menghilang (fade + slide) sebelum refresh data.
    - `OnNavigatedTo` kini selalu cek sinkronisasi source library saat page dibuka kembali; jika ada perubahan dari halaman lain (contoh remove dari GameDetail), data Library otomatis reload terbaru.
  - `LibraryViewModel.cs`:
    - `RemoveGameWithResultAsync` ditambah opsi `reloadOnSuccess` agar Library bisa menampilkan animasi dulu sebelum reload.
    - Menambahkan `RefreshIfLibraryChangedAsync()` untuk membandingkan source library aktual vs snapshot ViewModel dan reload bila berbeda.
- Build: pending verifikasi build gate setelah batch ini.
- Next: QA runtime dua skenario:
  1) remove langsung dari Library -> card hilang animasi lalu list konsisten,
  2) remove dari GameDetail -> kembali ke Library data sudah tidak nyangkut.

Tanggal: 1 Juni 2026
- Fokus: Perbaikan gap kosong card Library setelah Remove Game.
- Perubahan:
  - LibraryPage.xaml.cs: animasi remove dipindah dari ListViewItem container ke root card (ContentTemplateRoot) agar container recycle tidak mewarisi state visual lama.
  - LibraryPage.xaml.cs: reset state visual setelah animasi (Opacity dan TranslateTransform pada card + container) sebelum reload data.
- Build: dotnet build NexaPlay.slnx -c Debug sukses (0 error).
- Next: Validasi runtime berulang remove beberapa item berurutan untuk memastikan tidak ada slot kosong tersisa.
Tanggal: 1 Juni 2026
- Fokus: UX dialog Add Game untuk case game belum tersedia.
- Perubahan:
  - GameDetailViewModel.cs: menambah state ShowAddGameDialogProgress agar progress bar + persen bisa disembunyikan kondisional.
  - GameDetailViewModel.cs: saat status case "belum tersedia", progress/persen otomatis disembunyikan.
  - GameDetailPage.xaml: blok progress Add Game kini bind ke visibilitas ShowAddGameDialogProgress.
- Build: dotnet build NexaPlay.slnx -c Debug sukses (0 error).
- Next: QA runtime case add game unavailable untuk memastikan dialog tampil informatif tanpa 0%.
Tanggal: 2026-06-01
- Fokus: Kejelasan pesan user pada alur Bypass Game 3rd-party.
- Perubahan: Menambahkan dialog info/sukses/gagal di BypassGameDetail agar case penting tampil sebagai popup (kategori tidak didukung, file fix kosong, error proses, butuh Administrator, dan sukses akhir); teks konfirmasi antivirus disamakan substansinya dengan GameHub tetapi tetap memakai UI NexaPlay.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Samakan wording dialog error edge-case lain bila diperlukan, tanpa mengubah alur teknis fix.

Tanggal: 2026-06-01
- Fokus: Konsistensi wording dialog Bypass Game agar pesan user lebih jelas.
- Perubahan: Menyelaraskan copy dialog/error untuk case utama (kategori tidak didukung, file fix belum ada, antivirus dibatalkan, manual folder invalid, dan kebutuhan Administrator); mempertegas title/CTA dialog antivirus serta picker folder agar action user tidak ambigu.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Jika diperlukan, samakan wording ini ke halaman bypass lain agar tone pesan global konsisten.

Tanggal: 2026-06-01
- Fokus: Konsistensi style tombol dialog bypass.
- Perubahan: Menambahkan helper tema tombol dialog pada `BypassGameDetailPage` untuk seluruh dialog baru (konfirmasi antivirus, pilih folder, info hasil) dengan kombinasi tombol putih dan hover transparan/soft sesuai style NexaPlay.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Jika diinginkan, samakan helper ini ke dialog bypass lama agar seluruh halaman bypass satu gaya.

Tanggal: 2026-06-01
- Fokus: Konsistensi terminologi UI Bypass.
- Perubahan: Mengganti seluruh teks UI user-facing pada alur Bypass Detail yang masih memakai kata `fix` menjadi `bypass` (progress message, judul dialog, dan instruksi offline) agar selaras dengan rename GameHub.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Audit ringan halaman bypass lain untuk memastikan tidak ada istilah `Fix Games` tersisa di UI user-facing.

Tanggal: 2026-06-01
- Fokus: Stabilitas ekstraksi archive bypass saat WinRAR gagal.
- Perubahan: Memperbaiki fallback ekstraksi agar jika WinRAR gagal otomatis mencoba 7-Zip; memperbaiki argumen password WinRAR (quoted) dan mode tanpa password (`-p-`) untuk menghindari prompt interaktif/error saat proses background.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Jika user masih menemui error code spesifik, capture pesan lengkap error gabungan WinRAR/7-Zip untuk pemetaan penyebab per game archive.

Tanggal: 2026-06-01
- Fokus: Parity teknis download Google Drive agar ekstraksi bypass konsisten dengan GameHub.
- Perubahan: Port logika download GameHub ke BypassGameDetailViewModel: ekstraksi fileId dari URL, multi-metode URL (`drive.usercontent` -> extracted confirm link -> `drive.google.com/uc`), deteksi respons HTML konfirmasi, retry/backoff, validasi signature file archive (RAR/ZIP) sebelum ekstrak, dan pesan error spesifik jika file hasil download bukan archive.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Uji runtime pada link GDrive yang sebelumnya gagal (termasuk multipart RAR) dan verifikasi file hasil download tidak lagi berukuran HTML kecil.

Tanggal: 2026-06-01
- Fokus: Hard parity cabang antivirus dan Windows Defender exclusion pada alur Bypass.
- Perubahan: Menambahkan `EnsurePathExcludedAsync` dengan hasil detail (`success`, `needsAdmin`, `defenderMissing`, `error`) agar flow keputusan setara GameHub; antivirus check error teknis kini non-fatal (tetap lanjut), sementara penolakan user tetap fatal; saat Defender tidak tersedia langkah exclusion dilewati dengan dialog info, dan saat butuh admin ditampilkan pesan instruktif yang tegas.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Validasi runtime 3 skenario cabang (Defender available, Defender missing, butuh admin) untuk memastikan perilaku identik dengan GameHub.

Tanggal: 2026-06-01
- Fokus: Perbaikan crash format pada langkah exclusion saat rerun bypass.
- Perubahan: Memperbaiki konstruksi command PowerShell di `EnsurePathExcludedAsync` (hapus `string.Format` yang berbenturan dengan kurung kurawal script) sehingga error `Input string was not in a correct format...` tidak muncul lagi saat proses bypass ulang.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Uji ulang bypass pada game yang sama untuk memastikan exclusion step lanjut normal ke download/extract.

Tanggal: 2026-06-01
- Fokus: Peningkatan UX progress bypass agar lebih informatif seperti GameHub.
- Perubahan: Menambah tampilan persen global progress di UI Bypass Detail (`BypassProgressPercentText`), menambah detail status kecil (`BypassProgressDetail`), serta membuat progress download lebih halus berbasis bytes (bukan loncat per-file) dengan pemetaan tetap pada rentang 40%-70%.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Validasi runtime pada file besar agar animasi progress download terlihat halus dan akurat.

Tanggal: 2026-06-01
- Fokus: Kejelasan flow manual folder saat game path tidak terdeteksi.
- Perubahan: Menyamakan behavior dengan GameHub untuk case manual path: jika user batal/tidak memilih folder kini muncul pesan jelas `Anda belum memilih folder game.` (bukan dialog kosong); menambahkan fallback friendly error jika exception tanpa message agar popup `Bypass Gagal` selalu berisi informasi.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Uji ulang skenario `detect path gagal -> pilih manual -> cancel` dan `detect path gagal -> pilih manual -> pilih folder` untuk memastikan UX sesuai.

Tanggal: 2026-06-01
- Fokus: Perbaikan pesan error kosong pada kegagalan membuka folder picker manual.
- Perubahan: Menambahkan fallback detail error di `SelectManualFolderAsync` agar popup tidak lagi berhenti pada teks `Gagal membuka pemilih folder:` kosong; sekarang selalu tampil alasan default yang jelas jika message exception kosong.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Validasi ulang pada mesin/user yang sebelumnya memunculkan pesan kosong untuk memastikan informasi kini konsisten tampil.

Tanggal: 2026-06-01
- Fokus: Parity mekanisme pilih folder manual dengan GameHub.
- Perubahan: Mengganti jalur utama pemilih folder manual menjadi dialog `FolderBrowserDialog` via proses PowerShell STA (mirip pendekatan GameHub) untuk menghindari kegagalan `FolderPicker` WinRT di environment tertentu; `FolderPicker` WinRT tetap dipakai sebagai fallback.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Uji runtime `detect path gagal -> pilih folder manual` pada mesin yang sebelumnya gagal untuk memastikan dialog folder kini terbuka normal.

Tanggal: 2026-06-01
- Fokus: Menyamakan tampilan dialog pilih folder agar modern seperti GameHub.
- Perubahan: Mengubah prioritas pemilih folder manual ke WinRT `FolderPicker` (UI explorer modern) sebagai jalur utama; dialog legacy `FolderBrowserDialog` dipertahankan hanya sebagai fallback saat WinRT gagal di environment tertentu.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Verifikasi visual bahwa dialog folder kini tampil modern pada flow `path tidak terdeteksi`.

Tanggal: 2026-06-01
- Fokus: Konsistensi tampilan pemilih folder agar tetap modern.
- Perubahan: Menghapus percobaan API `Microsoft.Windows.Storage.Pickers` (belum tersedia di Windows App SDK project saat ini) yang menyebabkan fallback ke dialog lama; memperbarui jalur fallback ke `OpenFileDialog` mode folder (`CheckFileExists=false`, `ValidateNames=false`) via PowerShell STA sehingga tampilan picker tetap explorer-style modern, bukan tree-view klasik.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Verifikasi visual di flow manual path bahwa dialog yang tampil konsisten modern pada mesin user.

Tanggal: 2026-06-01
- Fokus: Parity proses akhir bypass untuk sinkronisasi Steam Launch Options.
- Perubahan: Menambahkan dukungan `launch_option` pada `FixEntry` parser; menambahkan API `ISteamService.SetLaunchOptionsAndRestartAsync` dan implementasi di `SteamPlatformService` (kill Steam -> backup `localconfig.vdf` -> upsert `LaunchOptions` berdasarkan AppID -> restart Steam); menghubungkan flow ini di akhir `StartBypassGameAsync` setelah replace file; mendukung token `{exe_name}` dan `{exe_path}` berbasis `exe_hint` + folder game terpilih.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Validasi runtime pada entri JSON yang memiliki `launch_option` dan cek hasil di Steam Properties > General > Launch Options.

Tanggal: 2026-06-01
- Fokus: Penyesuaian aturan launch option custom agar sesuai skema JSON user.
- Perubahan: Gating launch option di akhir bypass kini hanya aktif jika `use_shortcut=true` dan `exe_hint` tersedia; sumber command diambil dari field entry (`launch_option`/alias) atau mapping eksternal `launch_options.json` berbasis AppID (mendukung format seperti `"204100": "\"...exe\" %command%"`); parser `appid` pada `new_fix_games.json` juga dibuat toleran untuk tipe string/number.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Uji runtime pada satu game dengan `use_shortcut+exe_hint` dan satu game tanpa field tersebut untuk memastikan hanya game target yang mendapat LaunchOptions.

Tanggal: 2026-06-01
- Fokus: Otomatisasi penuh launch option tanpa mapping JSON manual.
- Perubahan: Menghapus ketergantungan runtime ke `launch_options.json`; launch option kini dibentuk otomatis sebagai `"full_exe_path" %command%` dari hasil deteksi `gamePath` + `exe_hint` (langsung cek path direct lalu fallback pencarian rekursif di folder instalasi game); gating tetap hanya untuk entry `use_shortcut=true` dan `exe_hint` tersedia.
- Build: `dotnet build NexaPlay/NexaPlay.csproj -c Debug -p:Platform=x64` sukses (0 error, 0 warning).
- Next: Uji runtime beberapa game dengan struktur subfolder exe berbeda untuk memastikan resolve `exe_hint` selalu tepat.

## 10. Update Log Ringkas

Tambahkan catatan baru di atas bagian ini setiap selesai batch penting.
Format:

```text
Tanggal:
- Fokus:
- Perubahan:
- Build:
- Next:
```

Tanggal: 9 Juni 2026
- Fokus: Memulihkan `SettingsPage` yang blank setelah integrasi UI update.
- Perubahan: `SettingsPage.xaml` memakai `Converter={StaticResource BoolToVis}` untuk blok progres update, tetapi resource itu tidak tersedia saat runtime dan membuat halaman Settings gagal render. Solusinya diubah mengikuti pola page lain di repo: `Visibility` kini memakai `x:Bind local:SettingsPage.BoolToVis(ViewModel.IsInstallingUpdate)` dengan helper statis lokal di code-behind, sehingga `SettingsPage` bisa terbuka normal kembali tanpa bergantung pada resource converter global.
- Build: `MSBuild Debug x64 /p:OutDir=Debug-settingsfix` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: Uji runtime buka `Settings` lalu pastikan section `Application Update` tampil dan tombol manual check/update tetap berfungsi.

Tanggal: 9 Juni 2026
- Fokus: Menyiapkan aset release awal untuk flow auto update berbasis `setup.exe`.
- Perubahan: Menambahkan folder `release` berisi `NexaPlaySetup.iss` untuk compile installer Inno Setup, `update-stable.json` sebagai template manifest GitHub, `Generate-UpdateManifest.ps1` untuk hitung SHA-256 + generate manifest final, dan `README.md` berisi urutan publish -> build installer -> generate manifest -> upload GitHub Release.
- Build: Tidak mengubah kode runtime app; belum perlu build tambahan selain verifikasi batch sebelumnya.
- Next: Publish `Release`, compile `NexaPlay-Setup.exe` dengan Inno Setup, generate `update-stable.generated.json`, lalu upload installer + manifest ke GitHub.

Tanggal: 9 Juni 2026
- Fokus: Menyatukan endpoint update agar seluruh flow release memakai repo `adii83/NexaPlay`.
- Perubahan: `AppConstants.AppUpdateManifestUrl` diubah agar runtime scan update membaca `https://raw.githubusercontent.com/adii83/NexaPlay/main/NexaPlay/release/update-stable.json`. Template `release/update-stable.json` dan panduan `release/README.md` juga diubah agar `installerUrl` mengarah ke asset GitHub Release repo `adii83/NexaPlay`, sementara manifest disimpan langsung di path `NexaPlay/release/update-stable.json` pada branch `main`.
- Build: Perlu verifikasi ulang setelah perubahan endpoint manifest.
- Next: Build ulang, lalu lakukan rilis pertama `v1.0.0` ke repo `adii83/NexaPlay` memakai `setup.exe`; sesudah itu siapkan `v1.0.1` untuk test auto update end-to-end.

Tanggal: 9 Juni 2026
- Fokus: Meluruskan panduan release agar baseline `v1.0.0` dan test update `v1.0.1` tidak tercampur.
- Perubahan: `release/README.md` sekarang memisahkan dua skenario secara eksplisit: rilis pertama `1.0.0` untuk base install, dan rilis berikutnya `1.0.1` untuk uji auto update. Contoh perintah `Generate-UpdateManifest.ps1` juga dibuat dua versi agar tidak lagi terkesan harus langsung menaikkan versi ke `1.0.1`.
- Build: Tidak mengubah kode runtime app.
- Next: Ikuti jalur `v1.0.0` dulu untuk installer pertama; setelah terpasang, baru naikkan versi dan generate manifest `v1.0.1` untuk pengujian update otomatis.

Tanggal: 10 Juni 2026
- Fokus: Merapikan `.gitignore` agar aset update/release penting tetap bisa masuk repo tanpa ikut membawa artefak lokal.
- Perubahan: `.gitignore` tidak lagi memblokir seluruh file `*.txt` dan `*.exe`, lalu ditambah pengecualian eksplisit untuk `NexaPlay/release/**` supaya `NexaPlaySetup.iss`, `update-stable.json`, `Generate-UpdateManifest.ps1`, dan `release/README.md` bisa di-commit. Sebaliknya, output installer (`NexaPlay/release/output/`), manifest generated (`update-stable.generated.json`), log lokal, `readgz.exe`, `recovered_edits.md`, dan `scratch_steam_html.txt` sekarang tetap ter-ignore.
- Build: Tidak mengubah kode runtime app.
- Next: Commit folder `NexaPlay/release` bersama perubahan update system, lalu lanjut ke proses publish `Release` dan build installer Inno Setup.

Tanggal: 10 Juni 2026
- Fokus: Mengatasi build installer yang setelah install tampil sebagai `Kesalahan jaringan` dan terkesan kehilangan asset.
- Perubahan: Audit `dotnet publish Release` menunjukkan folder publish sebenarnya sudah membawa `Assets/` dan `data/api.json`, jadi masalah utama bukan asset statis hilang. Akar yang lebih kuat justru `PublishTrimmed=true` pada `Release`, yang menghasilkan trim warnings tepat di jalur `LicenseService`, `LicenseStore`, dan `AppUpdateService` berbasis `System.Text.Json`. `NexaPlay.csproj` sekarang diubah agar `PublishTrimmed=False` untuk release installer, dengan catatan bahwa jalur JSON runtime app belum trim-safe.
- Build: `dotnet publish -c Release -p:Platform=x64 -r win-x64 --self-contained true` sukses lagi setelah trimming dimatikan, dan warning trim di jalur lisensi/update hilang.
- Next: Build ulang `setup.exe` dari output publish release yang baru, reinstall, lalu retest aktivasi license. Jika masih gagal, baca `%LOCALAPPDATA%\NexaPlay\nexaplay.log` untuk pesan exception sebenarnya, bukan hanya banner UI `Kesalahan jaringan`.

Tanggal: 10 Juni 2026
- Fokus: Memastikan asset SVG logo ikut ke output publish/install.
- Perubahan: `NexaPlay.csproj` sekarang memberi `CopyToOutputDirectory=PreserveNewest` pada `Assets/logo.svg` dan `Assets/logo_text.svg`. Verifikasi publish release menunjukkan kedua file sudah muncul di `bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/Assets/`, sehingga header/overlay yang memakai `ms-appx:///Assets/logo.svg` dan `ms-appx:///Assets/logo_text.svg` tidak lagi kehilangan logo setelah build installer baru.
- Build: `dotnet publish -c Release -p:Platform=x64 -r win-x64 --self-contained true` sukses, dan SVG terkonfirmasi ada di folder publish.
- Next: Compile ulang `NexaPlaySetup.iss` dari publish terbaru, reinstall, lalu verifikasi logo NexaPlay tampil di header dan overlay lisensi/startup.

Tanggal: 10 Juni 2026
- Fokus: Melengkapi dokumentasi release/update agar urutan rilis ke depan tidak membingungkan.
- Perubahan: `release/README.md` dirombak menjadi panduan operasional penuh untuk rilis awal dan rilis update berikutnya. Dokumen sekarang menjelaskan sinkronisasi versi (`AppConstants`, `.iss`, GitHub tag, manifest), urutan aman `publish -> build installer -> upload release asset -> generate manifest -> commit manifest`, kapan manifest boleh diupdate, cara menahan dialog update sementara, dan troubleshooting umum seperti cache update, error jaringan pasca-install, serta asset SVG yang hilang.
- Build: Tidak mengubah kode runtime app.
- Next: Ikuti checklist di `release/README.md` setiap kali merilis versi baru agar manifest tidak mendahului asset release dan user tidak melihat update yang belum siap.

Tanggal: 10 Juni 2026
- Fokus: Menyesuaikan mekanisme update agar user menjalankan installer sendiri setelah download selesai.
- Perubahan: Flow updater tidak lagi memakai argumen silent install. `AppUpdateInstallerArguments` dikosongkan, helper PowerShell sekarang hanya menunggu app utama tertutup lalu membuka `NexaPlay-Setup.exe` biasa tanpa auto-install dan tanpa auto-reopen app. Teks dialog startup/settings serta dokumentasi `release/README.md` juga diperbarui agar menjelaskan bahwa setelah download selesai, NexaPlay akan menutup diri lalu membuka installer untuk dilanjutkan manual oleh user.
- Build: Perlu verifikasi ulang setelah perubahan alur updater.
- Next: Build ulang, test update dari dialog startup/settings, lalu pastikan setelah download selesai installer Inno Setup terbuka normal dan user bisa melanjutkan wizard install secara manual.

Tanggal: 10 Juni 2026
- Fokus: Memasang icon `.ico` NexaPlay ke exe, shortcut desktop, Start Menu, taskbar, dan wizard installer.
- Perubahan: Dibuat `Assets/Icons/app.ico` multi-resolution dari `Assets/Icons/logo.png` melalui `release/Generate-AppIcon.ps1`. `NexaPlay.csproj` sekarang memakai `ApplicationIcon=Assets/Icons/app.ico` dan meng-copy file itu ke output publish. `release/NexaPlaySetup.iss` juga memakai `SetupIconFile` yang sama, serta shortcut installer dipaksa memakai `{app}\app.ico`. `release/README.md` ditambah panduan refresh icon setelah source PNG diubah.
- Build: Perlu verifikasi ulang setelah pemasangan icon ke exe dan installer.
- Next: Publish ulang + compile installer ulang, lalu cek bahwa icon tampil konsisten di exe, shortcut desktop, taskbar, Start Menu, dan wizard Inno Setup.

Tanggal: 10 Juni 2026
- Fokus: Menghilangkan false-positive dialog update setelah app sudah berhasil update, serta memastikan icon runtime taskbar/window mengikuti `app.ico`.
- Perubahan: `AppUpdateService` sekarang hanya memakai cache update jika `state.CurrentVersion` cocok dengan `AppConstants.AppVersion`. Cache hasil versi lama tidak lagi dipakai setelah app berpindah versi, sehingga kasus "baru selesai update tapi startup masih bilang versi 1.0.0 -> 1.0.1" tidak terulang. `MainWindow.xaml.cs` juga ditambah `ApplyWindowIcon(appWindow)` yang memanggil `appWindow.SetIcon()` ke `Assets/Icons/app.ico`, supaya icon window aktif, thumbnail taskbar, dan taskbar button tidak lagi jatuh ke icon generik/putih walau installer/shortcut sudah benar.
- Build: Perlu verifikasi ulang setelah patch cache update dan runtime window icon.
- Next: Build ulang publish + reinstall, lalu uji dua hal: startup tidak lagi memunculkan dialog update stale setelah pindah versi, dan icon runtime di taskbar/window thumbnail menampilkan NexaPlay icon yang benar.

Tanggal: 10 Juni 2026
- Fokus: Mengembalikan generator `app.ico` ke mode full-canvas tanpa crop otomatis.
- Perubahan: `release/Generate-AppIcon.ps1` sekarang default memakai seluruh kanvas `logo.png` apa adanya. Crop hanya aktif jika switch `-EnableCrop` diberikan. `app.ico` juga sudah digenerate ulang dari `Assets/Icons/logo.png` terbaru dalam mode no-crop.
- Build: Tidak mengubah kode runtime app.
- Next: Publish ulang + compile installer ulang untuk memastikan exe/shortcut/taskbar membaca `app.ico` hasil terbaru tanpa crop.


Tanggal: 9 Juni 2026
- Fokus: Implementasi fondasi update system NexaPlay berbasis `setup.exe` dengan UI hitam-putih yang konsisten.
- Perubahan: Menambahkan `IAppUpdateService` + `AppUpdateService` untuk cek manifest update GitHub, cache status update, compare versi, download installer, verifikasi SHA-256, lalu menjalankan helper updater PowerShell untuk silent install dan reopen app. `SettingsViewModel` + `SettingsPage` sekarang memakai data update dinamis dan dialog `Update Tersedia` bergaya dark monokrom. `MainWindow` juga melakukan startup update check setelah warmup; jika ada versi baru, user bisa langsung setuju dan app akan memakai `StartupOverlay` untuk progres download sebelum menyerahkan proses ke helper updater.
- Build: `MSBuild Debug x64` dengan `OutDir=bin\\x64\\Debug-preview\\` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: Siapkan manifest update GitHub yang nyata + artifact `setup.exe` release test/pre-release untuk validasi end-to-end download, silent install, dan reopen app. Jika perlu, ganti helper PowerShell menjadi updater `.exe` dedicated di batch berikutnya.

Tanggal: 9 Juni 2026
- Fokus: Rapikan timing dialog update startup agar muncul setelah Home selesai tampil.
- Perubahan: `MainWindow.xaml.cs` sekarang menunggu `StartupOverlay` benar-benar collapse dan `HomePage` sudah visible dulu lewat `WaitForHomeReadyAsync()`, baru setelah itu memunculkan dialog `Update Tersedia`. Hasilnya user melihat halaman Home utuh terlebih dahulu, lalu baru mendapat prompt update otomatis bila manifest mendeteksi versi baru.
- Build: `MSBuild Debug x64` dengan `OutDir=bin\\x64\\Debug-preview\\` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: Validasi runtime dengan manifest update nyata untuk memastikan prompt tidak muncul saat loading masih aktif dan tetap muncul konsisten beberapa ratus milidetik setelah Home selesai terbuka.

Tanggal: 9 Juni 2026
- Fokus: Rapikan shell startup dan aktivasi lisensi agar tampil full-area tanpa sidebar kiri.
- Perubahan: `MainWindow.xaml.cs` ditambah `UpdateShellChrome()` yang otomatis memaksa `SplitView` ke mode overlay tanpa pane saat `StartupOverlay`, `LicenseOverlay`, atau `ValidatingLicenseOverlay` aktif; state shell dipulihkan lagi setelah overlay selesai. Jalur immersive detail lama untuk `GameDetailPage` dan `BypassGameDetailPage` tetap dipertahankan.
- Build: Build normal `Debug x64` gagal copy output karena `NexaPlay.exe` sedang lock oleh proses aktif; build verifikasi dengan `OutDir=bin\\x64\\Debug-preview\\` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: Uji runtime visual saat cold startup, validasi lisensi online, dan flow activation key baru untuk memastikan sidebar benar-benar tidak terlihat dan kembali normal setelah overlay ditutup.

### 2026-05-26
- Fokus: Fix jumlah kolom card Home Popular & Games — parity GameHub Fix Games (6 kolom di fullscreen).
- Perubahan:
  - **Root cause ditemukan**: `minCardWidth=200` + threshold `>=1380` untuk 6 kolom terlalu ketat. Di layar 1366px fullscreen, usable width hanya ~1258px sehingga tidak pernah masuk breakpoint 6-kolom (sebelumnya hanya 5 kolom).
  - `GamesPage.xaml.cs` `ApplyGamesGridLayout`: breakpoint diubah `>=1380->6, >=1080->5, >=800->4` menjadi **`>=1100->6, >=880->5, >=680->4`**. `minCardWidth` 200→150, `maxCardWidth` 320→280. SlotWidth hanya di-clamp dari atas (fluid seperti CSS grid GameHub).
  - `HomePage.xaml.cs` `ApplyPopularGridLayout`: breakpoint dan konstanta disamakan persis dengan `GamesPage`. Referensi: GameHub menggunakan `xl:grid-cols-6` mulai 1280px, sedangkan content area NexaPlay sudah >= 1100px (sidebar 68px + padding 56px dikurangi).
  - Analisis matematis: layar 1366px fullscreen → usable GamesPage 1258px ≥ 1100 → **6 kolom** ✓; layar 1920px → usable 1812px → **6 kolom** ✓.
- Build: `Build succeeded`, `0 Error(s)`, `0 Warning(s)`.
- Next: Validasi runtime visual — cek 6 kolom muncul di fullscreen 1366px dan 1920px, badge PREMIUM/DENUVO masih terbaca di card yang lebih kecil.

### 2026-05-26
- Fokus: Finalisasi Layout List & Deteksi Library GameHub
- Perubahan:
  - Mengubah layout `LibraryPage` menjadi format List modern dengan penempatan AppID sejajar Genre, serta memodifikasi ikon Remove menjadi putih berlatar putih.
  - Mengintegrasikan fungsi pendeteksi game (ListLibraryGames) membaca `.lua` di folder `stplug-in` agar cara deteksinya sama persis 100% dengan internal GameHub.
  - Mengadaptasi desain label Denuvo dan Premium/Standard agar identik dengan `GamesPage`.
  - Membersihkan kode sisa (_build error/warning_) peninggalan formasi Grid, serta membatasi Pagination ke 10 item.
- Build: MSBuild bersih tanpa error ViewModel/CS0649 warning.
- Next: Menunggu arahan selanjutnya.

### 2026-05-25
- Fokus: Implementasi paritas UI `LibraryPage` berdasarkan desain `GamesPage` (modern, dark theme).
- Perubahan:
  - Menyusun ulang XAML `LibraryPage` agar serasi dengan identitas NexaPlay (membuang emoji, memperbaiki penempatan action bar, tombol _Restart Steam_ dll).
  - Mengimplementasikan `LibraryGameCard` UI model pada `LibraryViewModel` yang mem-parsing `InstalledGame`.
  - Mengadaptasi sistem pagination dan _responsive grid view_ dengan _resize debounce_ sama persis dengan `GamesPage`.
  - Membuat _Empty State_ UI jika tidak ada game yang terdeteksi, dengan _Call to Action_ menuju `GamesPage`.
  - Menangani _Unknown Game_ secara elegan dengan visual placeholder tanpa cover art.
- Build: Build sukses untuk verifikasi _C# code behind_ (meski _Architecture Warning_ untuk self-contained MSBuild muncul, tidak mempengaruhi code behavior).
- Next: Menunggu instruksi selanjutnya untuk fitur tambahan Library atau area lain.

### 2026-05-25
- Fokus: Checkpoint awal sesi (sinkronisasi konteks + baseline verification sebelum edit kode).
- Perubahan:
  - Membaca dokumen wajib berurutan: `README.md` -> `AGENTS.md` -> `ONBOARDING_ZERO_TO_PARITY.md` -> `MIGRATION_PARITY_MATRIX.md` -> `AI_HANDOFF_PROMPT.md` -> `AI_HANDOFF_HOME_HISTORY.md`.
  - Menetapkan guardrail sesi: tidak redesign lintas halaman, tidak menurunkan parity GameHub, dan tetap page-by-page.
- Build: baseline build gate `Debug x64` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: Menunggu instruksi pengguna untuk mulai modifikasi.

### 2026-05-23
- Fokus: Perbaikan bug caching transient error (429 Too Many Requests) di `SteamStoreService` & Navigation Cache GamesPage.
- Perubahan:
  - Mengubah `GetDetailAsync` dan `BuildMergedMetadataJsonAsync` agar tidak menyimpan ke cache jika terjadi error jaringan/transient dari Steam API.
  - Menambahkan pengecekan pada cache lama: jika file cache menyimpan status error (misal: `"stage":"steam_appdetails"`), cache akan diabaikan dan dihapus sehingga metadata bisa di-fetch ulang (menyelesaikan masalah "nyangkut" tanpa screenshots & about).
  - Menambahkan `NavigationCacheMode="Required"` pada `GamesPage.xaml` agar tidak kembali ke page 1 dan scroll awal saat user melakukan back navigation dari halaman Game Detail.
  - Memodifikasi urutan prioritas gambar cover di `HomeViewModel.cs` dan `GamesViewModel.cs` dengan menukar prioritas sehingga `assets.library_capsule` dari metadata mentah sekarang diprioritaskan di atas `detail.LibraryCapsule2xUrl`.
- Build: `Build succeeded` (`0 Error(s)`, `0 Warning(s)`).
- Next: Menunggu instruksi selanjutnya.

### 2026-05-26
- Fokus: Redesign Library Page parity dengan GameHub + Polish UI.
- Perubahan:
  - Mengubah logika deteksi library di `LibraryViewModel` untuk memakai `_addGame.ListLibraryGames()` yang memindai script `.lua` config stplug-in GameHub, bukan ACF Steam.
  - Menyelaraskan tampilan kartu di `LibraryPage` (cover vertikal, badge label Denuvo, dan tipe Premium/Standard) persis dengan UI halaman Home dan Games.
  - Memperbaiki tombol hapus (tong sampah) dengan background putih yang rapi dan penempatan AppID agar selaras dengan tata letak genre.
  - Mematikan animasi internal per-item `ListView` saat paginasi di `LibraryPage` untuk menghilangkan efek 'card berguguran' dan menggantinya dengan animasi kustom mulus se-halaman.
  - Mengatur padding agar scrollbar `LibraryPage` tepat menempel pada tepi ujung layar (seperti di halaman Home).
  - Menambahkan animasi sci-fi "Laser Scanner/Sweep" berulang memutari keseluruhan *border* dan *background* pada bar pencarian Library.
  - Mengubah tombol `Clear Filter` dan penutup (X) di `GamesPage` menjadi putih dengan teks/ikon hitam agar kontrasnya lebih terlihat jelas oleh pengguna.
- Build: `Build succeeded`.
- Next: Menunggu instruksi selanjutnya.

### 2026-05-23
- Fokus: checkpoint awal sesi (sinkronisasi konteks + baseline verification sebelum edit kode).
- Perubahan:
  - Membaca dokumen wajib berurutan: `README.md` -> `AGENTS.md` -> `ONBOARDING_ZERO_TO_PARITY.md` -> `MIGRATION_PARITY_MATRIX.md` -> `AI_HANDOFF_PROMPT.md` -> `AI_HANDOFF_HOME_HISTORY.md`.
  - Menetapkan guardrail sesi: tidak redesign lintas halaman, tidak menurunkan parity GameHub, dan tetap page-by-page.
- Build: baseline build gate `Debug x64` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: menunggu instruksi pengguna untuk mulai modifikasi.

### 2026-05-23
- Fokus: hardening performa `Games` skala 160k + clean warning build.
- Perubahan:
  - Terapkan **genre alias-map** di `GamesViewModel` agar variasi label genre tetap ter-filter akurat tanpa ubah UI.
  - Tambah **precomputed filter-index cache** ke disk (`%LocalAppData%\\NexaPlay\\runtime_catalog_sources\\games_filter_index_cache.json`) untuk percepat cold start halaman `Games`.
  - Strategi load `Games` menjadi cache-first: baca index cache dulu, fallback build dari snapshot metadata jika cache belum ada/invalid.
  - Filter/search tetap lightweight lewat index (`appid`, `nameLower`, `isPremium`, `hasDenuvo`, `genreTokens`) dan hydrate card hanya untuk page aktif.
  - Rapikan warning `WMC1506` aman di `HomePage.xaml` dengan mengubah beberapa `x:Bind` statik ke `Mode=OneTime`.
- Build: `Build succeeded` (`0 Warning(s)`, `0 Error(s)`).
- Next: validasi runtime cold-start vs warm-start `Games` serta akurasi filter genre untuk label alias (`RPG/Role-Playing`, `MMO/Massively Multiplayer`, dll).

### 2026-05-23
- Fokus: optimasi performa `Games` untuk dataset besar (160k) pada search/filter/genre-status.
- Perubahan:
  - `IMetadataService` ditambah `GetCatalogSnapshotAsync()`; `MetadataService` expose snapshot index in-memory supaya `Games` tidak hydrate metadata per-item saat filtering.
  - `GamesViewModel` memakai **lightweight filter index** (`appid`, `nameLower`, `isPremium`, `hasDenuvo`, `genreTokens`) yang dibangun sekali saat load.
  - Filter `Status Game` (`STANDARD/PREMIUM`) dan `Protection` (`DENUVO/NON-DENUVO`) sekarang dieksekusi langsung di index ringan (tanpa fetch metadata berulang).
  - Filter `Genres` diubah ke token overlap berbasis `GameEntry.Genre` (normalisasi split `,` + lowercase), agar konsisten dengan sumber metadata yang juga dipakai GameDetail.
  - Search diberi debounce `220ms` agar tidak men-trigger re-filter berat pada setiap ketikan.
  - Hydrate `FixEntry` untuk UI card tetap lazy hanya pada item halaman aktif (`kolom x 10`), sehingga layout/pager existing tidak berubah.
- Build: `Build succeeded` (`0 Error(s)`, `3 Warning(s)` WMC1506 lama di `HomePage.xaml`).
- Next: validasi runtime khusus beban nyata (search cepat, kombinasi filter genre+status, resize fullscreen/windowed, dan klik ke GameDetail) untuk cek latency UI dan akurasi hasil.

### 2026-05-23
- Fokus: remap sumber data `Games` ke baseline katalog metadata (`steam_data.json.gz`) tanpa mengubah layout/pola pagination.
- Perubahan:
  - `GamesViewModel` tidak lagi mengambil list dari `IBypassGamesDataService`; source awal diganti ke `IMetadataService` agar list berasal dari katalog penuh (160k baseline).
  - Ditambahkan kontrak baru `IMetadataService.GetAllCatalogAppIdsAsync()` dan implementasinya di `MetadataService` untuk expose daftar appid dari index runtime katalog.
  - `GamesViewModel` kini mengelola page secara lazy per appid: hydrate card hanya untuk item halaman aktif (`kolom x 10 baris`), mempertahankan layout card dan mekanik pager saat ini.
  - Mapping card tetap menggunakan metadata yang sama (title/publisher/poster/premium) dan tetap klik ke `GameDetailPage` dengan appid yang sama, sehingga alur detail/parity tidak berubah.
- Build: `Build succeeded` (`0 Error(s)`, `3 Warning(s)` WMC1506 lama di `HomePage.xaml`).
- Next: validasi runtime `Games` untuk search/filter di dataset besar (160k) dan cek respons pagination/resize tetap mulus.

### 2026-05-23
- Fokus: checkpoint awal sesi (sinkronisasi konteks + baseline verification sebelum edit kode).
- Perubahan:
  - Membaca dokumen wajib berurutan: `README.md` -> `AGENTS.md` -> `ONBOARDING_ZERO_TO_PARITY.md` -> `MIGRATION_PARITY_MATRIX.md` -> `AI_HANDOFF_PROMPT.md` -> `AI_HANDOFF_HOME_HISTORY.md`.
  - Menetapkan guardrail sesi: tidak redesign lintas halaman, tidak menurunkan parity GameHub, dan tetap page-by-page.
- Build: baseline build gate `Debug x64` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: lanjut eksekusi batch berikutnya dengan fokus aktif di `Games`/parity runtime sesuai prioritas terakhir handoff.

### 2026-05-23
- Fokus: stabilisasi besar `Games Page` agar parity perilaku `Home > Popular Games` tercapai (responsive grid, pagination, anti-glitch resize, dan state pager aktif).
- Perubahan:
  - **Grid + Pagination Core**
    - `GamesViewModel`: page size diubah dari statis menjadi dinamis mengikuti kolom (`kolom x 10 baris`) agar target “kebawah 10 card” tercapai.
    - `GamesPage.xaml.cs`: layout kolom tetap memakai breakpoint parity Home (`6/5/4/3`) + rasio card `1:1.5` + `ItemsWrapGrid` runtime.
  - **Anti Glitch Resize (Fullscreen/Windowed)**
    - Resize pakai debounce untuk menghindari update beruntun saat drag/transisi mode window.
    - Posisi scroll dipertahankan saat resize-update dengan capture/restore `VerticalOffset`, sehingga viewport tidak lompat ke atas.
    - Animasi transisi page disuppress saat update karena resize agar tidak terasa seperti “ganti page”.
    - Bug page aktif yang sempat balik ke page 1 saat toggle fullscreen/windowed sudah ditangani (menjaga page aktif saat kolom berubah).
  - **Autofill Bottom Row**
    - Setelah tuning anti-glitch, update kolom di jalur debounce diaktifkan kembali secara aman agar autofill card bawah tetap jalan pada fullscreen.
  - **Pager UX**
    - Tombol angka pager dibuat state-aware: hanya page aktif yang `background putih + teks hitam`; page nonaktif tetap dark.
    - Visibilitas angka page menyesuaikan total page (`ShowPage1/2/3`).
  - **Smooth Transition**
    - Next/Prev/GoTo page diberi animasi fade+slide yang lebih halus untuk perpindahan page normal (bukan resize).
  - **Parity Pattern Home Popular**
    - `Games` collection diubah ke `ObservableCollection` + sinkronisasi incremental (`add/remove/update` per item) agar tidak replace list kasar, sehingga flicker berkurang signifikan sambil mempertahankan autofill.
- Build:
  - Beberapa build normal sempat terkena file lock `NexaPlay.exe` saat app berjalan.
  - Build verifikasi menggunakan `OutDir=Debug-preview` konsisten sukses di checkpoint akhir (`0 Error(s)`, warning lama Home `WMC1506` non-blocking).
- Next:
  - Runtime QA final khusus `Games`:
    1) stress test toggle fullscreen/windowed berulang di page 1/2/3,
    2) verifikasi tidak ada kedipan yang mengganggu,
    3) verifikasi autofill baris bawah tetap penuh lintas mode window + DPI.
  - Jika masih ada micro-flicker device-spesifik, lanjut batch kecil “batched UI update during resize” tanpa ubah behavior inti/parity.

### 2026-05-23
- Fokus: stabilisasi glitch `Games` saat pagination dan transisi fullscreen <-> windowed.
- Perubahan:
  - `GamesViewModel.cs`: `UpdateGridColumns` tidak lagi reset kasar ke halaman 1; sekarang menjaga posisi list relatif dengan hitung ulang index awal item agar resize terasa stabil.
  - `GamesPage.xaml.cs`: update kolom saat resize dibuat debounce `140ms` (pola yang dipakai di histori Home) untuk mengurangi refresh beruntun/jitter saat drag resize.
  - `GamesPage.xaml.cs`: tambah animasi transisi saat ganti page (fade + slide halus) dipicu saat `CurrentPageLabel` berubah.
  - `GamesPage.xaml`: tombol angka pager `1/2/3` diubah jadi background putih dengan teks hitam sesuai request.
- Build: build normal sempat merah karena file lock `NexaPlay.exe`; build verifikasi dengan `OutDir=Debug-preview` sukses (`0 Error(s)`, `0 Warning(s)`).
- Next: validasi runtime fokus di `Games`: spam klik `Prev/Next/1/2/3` dan uji resize windowed/fullscreen berulang untuk cek apakah glitch sudah hilang total.

### 2026-05-23
- Fokus: ubah logika page `Games` menjadi 10 baris kebawah per halaman (kolom tetap adaptif seperti `Home > Popular`).
- Perubahan:
  - `GamesViewModel.cs`: ganti page size statis `10` menjadi dinamis `kolom x 10` (`RowsPerPage=10`, `PageSize => _gridColumns * RowsPerPage`).
  - `GamesViewModel.cs`: tambah `UpdateGridColumns(int columns)` untuk sinkron jumlah item per halaman saat lebar grid berubah (fullscreen/windowed).
  - `GamesPage.xaml.cs`: setelah `ApplyGamesGridLayout`, kirim jumlah kolom aktif ke ViewModel agar pagination ikut pola layout runtime.
- Build: `Build succeeded`, `0 Error(s)`; warning non-blocking termasuk lock file `NexaPlay.exe` karena app sedang berjalan.
- Next: validasi visual di runtime bahwa tiap halaman terisi penuh 10 baris kebawah untuk semua mode window, lalu fine-tune jika ada gap sisa di row terakhir.

### 2026-05-23
- Fokus: rapikan spacing card `Games` agar parity `Home Popular` + set page size kecil untuk performa UX list.
- Perubahan:
  - `GamesPage.xaml`: `GridViewItem` margin diubah `0 -> 8` agar gap horizontal/vertikal antar card sama ritmenya dengan `Home`.
  - `GamesPage.xaml`: corner radius card disamakan ke `12` (root + image + gradient layer) agar kontur card tidak terlihat “pecah” antar item.
  - `GamesViewModel.cs`: `PageSize` diubah `30 -> 10` sesuai request “kebawahnya 10 card”.
- Build: setelah patch batch ini dijalankan build ulang (Debug x64) untuk validasi.
- Next: validasi visual langsung di window app; jika masih ada deviasi, copy nilai spacing persis dari template `Home` (badge/title offset) ke `Games`.

### 2026-05-23

- Fokus: parity UI `Games` terhadap mekanik layout `Home Popular` + stabilisasi UX filter/search/pager.
- Perubahan:
  - `GamesPage` top bar dipoles bertahap: alignment field search, logo kiri, tombol search icon putih, dan tips SteamDB.
  - Overlay filter `Games` distabilkan: jarak checkbox `Status Game` disamakan ritmenya dengan `Genre`, panel tetap hitam solid dan melayang di atas card.
  - Animasi buka/tutup filter ditambah (fade + slide halus) di `GamesPage.xaml.cs`.
  - Pager diubah dari posisi melayang ke **footer konten** (dipindah ke `GridView.Footer`) dengan model `Prev | 1 | 2 | 3 | Next`.
  - Crash `RelayCommand<int>` akibat `CommandParameter` string diperbaiki dengan parsing aman di `GoToPage(string pageText)`.
  - Engine layout card `Games` diparitas-kan dengan `Home Popular`: `GamesGrid_SizeChanged` + `ApplyGamesGridLayout` memakai breakpoint kolom `6/5/4/3`, perhitungan slot width, rasio card `1:1.5`, dan set `ItemsWrapGrid.ItemWidth/ItemHeight/MaximumRowsOrColumns` secara runtime.
  - Warning `CS0219` di `MainWindow.xaml.cs` dibersihkan (variabel `title` tidak dipakai).
- Build: checkpoint terakhir `Build succeeded`, `0 Error(s)`, warning tersisa `WMC1506` lama di `HomePage.xaml`.
- Next: lanjutkan parity logika `Games` load-more/fill-row bawah mengikuti pola `Home` (target rows penuh per kolom) sebelum masuk integrasi metadata besar 160k.

### 2026-05-23

- Fokus: Stabilitas awal transisi fokus dari `Home` ke `Games`.
- Perubahan:
  - Fix crash `NullReferenceException` di `GamesViewModel` saat constructor (urutan inisialisasi `SelectedGenres` vs `SearchQuery` + guard null di `ApplyFiltersAndPagination`).
  - Fix crash `XamlParseException` di `GamesPage` karena resource key `NexaPrimaryButtonStyle` tidak ditemukan (style reference dihapus).
  - Crash logger dipindah ke path tetap `D:\My Project\NexaPlay\crash.txt` dan diubah overwrite per crash terbaru (tidak append historis panjang).
  - Hotfix defensif UI `Games`: binding genre via handler + converter URL image aman untuk mencegah crash parsing image.
- Build: `Build succeeded` (`0 Error(s)`).
- Next: lanjut wiring data `Games` ke metadata source besar (160k) dengan query ringan + cache, tanpa menurunkan performa.







### 2026-05-27 (Batch : Implementasi UI Steam Sharing)
- Fokus: Menambahkan tampilan khusus untuk "Steam Sharing" (Akun Steam) di dalam BypassGameDetailPage untuk mengakomodasi instruksi panjang.
- Perubahan:
  - FixEntry.cs: Menambahkan properti Username.
  - BypassGamesDataService.cs: Memperbarui ParseFixGamesJson dan ParseSteamGamesJson untuk mengekstrak field username.
  - BypassGameDetailViewModel.cs: Menambahkan logika ShowSteamSection, mapping SteamUsername / SteamPassword, serta implementasi command untuk *Copy* data dan ReportAccountCommand.
  - BypassGameDetailPage.xaml: Membungkus section bypass 3rd party sebelumnya dan menambahkan <StackPanel> baru khusus untuk ShowSteamSection.
  - BypassGameDetailPage.xaml: Mendesain grid informasi akun dan integrasi YouTube dengan WebView2 di kolom kiri, serta layout grid numbering instruksi (1-12) panjang yang tertata rapi di kolom kanan.
  - Memperbaiki parsing tag </Grid> yang tertinggal dan menghapus properti invalid ClipToBounds pada tag XAML.
- Build: Build Succeeded, 0 Error(s).
- Next: Implementasi tombol "Mulai Bypass" untuk akun yang bukan steam sharing (action command) dan penyelesaian integrasi metadata library capsule cover.

### 2026-05-27 (Batch : Fix UI Steam Account & YouTube Embed Performance)
- Fokus: Memperbaiki binding UI dan performa video YouTube khusus untuk game tipe "Akun Steam" (tanpa badge Steam Sharing) dan Cover Art 3rd Party.
- Perubahan:
  - BypassGameDetailViewModel.cs: Menambahkan pemanggilan OnPropertyChanged(nameof(CoverArtUrl)) yang tertinggal sehingga *Cover Art* (library capsule) pada section "Mohon Dibaca Sebentar" (3rd Party) dan section lainnya sekarang muncul dengan benar seperti pada page Bypass.
  - BypassGameDetailPage.xaml: Mengganti WebView2 YouTube yang me-load langsung menjadi strategi *Click-to-Play* (Thumbnail overlay dengan ikon *Play*) demi menjaga performa aplikasi agar tidak lemot saat halaman dibuka.
  - BypassGameDetailPage.xaml.cs: Menambahkan event handler TutorialThumbnail_Tapped yang secara dinamis melakukan inisialisasi EnsureCoreWebView2Async() dan memuat URL YouTube hanya saat pengguna menekan tombol Play pada thumbnail.
- Build: Build Succeeded, 0 Error(s).
- Next: Menunggu validasi user, lalu lanjut ke pembuatan fungsi aksi tombol "Mulai Bypass".

### 2026-05-27 (Batch : Implementasi Dapatkan Kode Verifikasi Steam Guard)
- Fokus: Menambahkan fitur pengambilan kode Steam Guard langsung dari email master secara otomatis via IMAP untuk game tipe Akun Steam.
- Perubahan:
  - Menginstal package MailKit dan MimeKit.
  - FixEntry.cs & BypassGamesDataService.cs: Menambahkan dan mem-parsing properti DapatkanKode.
  - SteamGuardService.cs: Membuat service IMAP baru (berjalan secara asinkron) untuk menyaring kode 5 karakter terbaru dari noreply@steampowered.com.
  - BypassGameDetailViewModel.cs: Menambahkan command GetSteamGuardCodeAsync dan state pendukung (ShowDapatkanKode, SteamGuardCode, IsLoadingKode).
  - BypassGameDetailPage.xaml: Menambahkan seksi UI "Dapatkan Kode Verifikasi" di bawah section Password dengan tombol trigger dan animasi loading ProgressRing.
- Build: Build Succeeded (x64), 0 Error(s).
- Next: Implementasi tombol "Mulai Bypass" untuk akun yang bukan steam sharing (action command) dan penyelesaian integrasi akhir.

### 2026-05-27 (Batch : Redesign UI & UX Dapatkan Kode Steam Guard)
- Fokus: Memperbaiki tata letak (layout) dan pengalaman pengguna pada bagian "Dapatkan Kode Verifikasi" berdasarkan feedback, agar tidak berdesakan dan tampil lebih rapi.
- Perubahan:
  - BypassGameDetailViewModel.cs: Menambahkan properti `HasSteamGuardCodeResult` untuk trigger visibilitas papan hasil, `IsSteamGuardCodeSuccess` untuk memvalidasi format kode, dan `CopySteamGuardCode()` untuk fitur penyalinan teks hasil kode.
  - BypassGameDetailPage.xaml: Memisahkan teks judul dan teks instruksi menggunakan `StackPanel` (Spacing=4) agar jarak vertikal lebih proporsional dan tidak mepet.
  - BypassGameDetailPage.xaml: Mengubah tata letak "Dapatkan Kode" menjadi form horizontal di samping teks (menggunakan Grid) agar selaras dengan desain *modern-app*.
  - BypassGameDetailPage.xaml: Menambahkan elemen Papan Hasil (Border gelap) yang akan muncul otomatis usai loading selesai (`ProgressRing` berhenti).
  - BypassGameDetailPage.xaml: Papan Hasil mendukung teks panjang (Wrap) agar pesan error yang panjang tidak terpotong (contoh: "Tidak ada kode Steam yang Masuk!!...").
  - BypassGameDetailPage.xaml: Jika pengambilan kode berhasil (5 karakter valid), papan hasil akan menampilkan tombol "Copy Code" di sebelah kanannya yang tersambung dengan fitur copy clipboard.
- Build: Build Succeeded (x64), 0 Error(s).
- Next: Finalisasi dan melanjutkan ke fungsionalitas "Mulai Bypass Game".

### 2026-05-27 (Batch : Pemisahan Instruksi & Styling Khusus Steam Sharing)
- Fokus: Memisahkan dan merapikan panduan "Instruksi Penggunaan" berdasarkan kategori akun (Family Sharing vs Offline Mode) serta memastikan konsistensi desain warna monokrom NexaPlay.
- Perubahan:
  - BypassGameDetailPage.xaml: Memecah blok "Instruksi Penggunaan" menjadi dua versi. Versi 1-12 untuk akun biasa (Family Sharing) dan versi 1-6 khusus untuk akun kategori `steam-sharing` (Offline Mode).
  - BypassGameDetailPage.xaml: Menukar (*swap*) binding logika visibilitas (`InverseBoolToVis` dan `BoolToVis` terhadap `ShowSteamSharingBadge`) agar instruksi Family Sharing muncul pada game *SteamAccount* biasa, dan instruksi Offline Mode muncul pada game dengan *badge* `steam-sharing`.
  - BypassGameDetailPage.xaml: Mendesain ulang gaya kotak peringatan (Step 5 - PENTING Aktifkan Offline Mode) dengan layout spesifik (border kiri lebih tebal) namun mengembalikan palet warnanya menjadi murni monokrom (hitam, putih, abu-abu gelap) tanpa warna-warni bawaan referensi gambar.
- Build: Build Succeeded (x64), 0 Error(s).
- Next: Menambahkan logika untuk mengeksekusi game (Mulai Bypass / Aktivasi Offline).




