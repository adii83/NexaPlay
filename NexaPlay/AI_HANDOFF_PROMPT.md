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
6. D:\My Project\NexaPlay\.agents\rules\antigravity-rtk-rules.md

Lokasi project utama:
- D:\My Project\NexaPlay\NexaPlay

Lokasi referensi GameHub:
- D:\My Project\NexaPlay\gamehub

Tugas awal sebelum edit:
1. Ringkas pemahaman posisi terakhir NexaPlay.
2. Sebutkan halaman/fokus aktif terbaru.
3. Jalankan baseline build.
4. Baru lanjut implementasi batch kecil.

Jangan redesign semua halaman sekaligus.
Jangan mengurangi feature parity GameHub.
Jangan mengubah behavior inti tanpa alasan kuat dan tanpa cek referensi GameHub.
Jangan lupa ## 10. Update Log Ringkas

Tambahkan catatan baru di atas bagian ini setiap selesai batch penting.
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

### 2026-05-19 (Rapikan parser HTML Additional Information)

- Fokus: Penulisan konten di `ADDITIONAL INFORMATION` masih terlihat kurang rapi karena parsing HTML terlalu minimal.
- Perubahan:
  - `GameDetailPage.xaml.cs` `FormatInlineHtml(...)` diperkuat dengan pendekatan parser ringan:
    - normalisasi newline (`\r\n`/`\r`),
    - konversi `br/p/div/li/ul/ol` ke struktur baris yang lebih rapi,
    - strip tag sisa + HTML decode,
    - normalisasi spasi ganda/newline berlebih,
    - normalisasi bullet agar konsisten (`- `).
  - `GameDetailPage.xaml`:
    - `SUPPORT` sekarang pakai `FormatInlineHtml(ViewModel.DisplaySupport)`.
    - `LEGAL NOTICE` sekarang pakai `FormatInlineHtml(ViewModel.DisplayLegalNotice)`.
  - `SUPPORTED LANGUAGES` dan `DRM NOTICE` tetap pakai formatter yang sama sehingga keempat blok info tambahan punya gaya parsing konsisten.
- Build: `Build succeeded`, `0 Error(s)`, `5 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: Validasi visual runtime pada beberapa game dengan HTML berbeda (languages panjang, DRM multiline, support URL+email) untuk cek wrapping dan readability.

### 2026-05-19 (Simplify Post-About Media Layout 1/2/3)

- Fokus: Menyederhanakan layout screenshot bawah About agar konsisten, tidak acak, dan sesuai arahan: 1 full, 2 stacked (bawah lebih lebar), 3+ model mirror Game Overview dengan card besar di kanan.
- Perubahan:
  - `GameDetailViewModel.cs`:
    - Tambah properti hitung/layout:
      - `PostAboutScreenshotCount`
      - `PostAboutScreenshotUrl1/2/3`
      - `HasPostAboutLayoutSingle`, `HasPostAboutLayoutDouble`, `HasPostAboutLayoutTriple`
    - Tetap gunakan filter existing: sumber screenshot metadata, exclude screenshot yang sudah dipakai di `GAME OVERVIEW`, dedupe URL.
  - `GameDetailPage.xaml`:
    - Ganti layout post-about lama menjadi 3 mode sederhana:
      1. **Single**: 1 gambar full-width.
      2. **Double**: 2 gambar bertumpuk, kartu kedua lebih tinggi/lebar visual (center-crop).
      3. **Triple+**: 3 gambar dengan komposisi mirror `GAME OVERVIEW` (dua kecil kiri, satu besar kanan).
    - Semua card tetap reuse interaksi media existing (`Tapped`, hover scale handlers).
- Build: `Build succeeded`, `0 Error(s)`, `68 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: Validasi visual runtime di beberapa game (sisa screenshot 1, 2, dan >=3) untuk fine-tune tinggi card jika diperlukan.

### 2026-05-19 (Post-About Screenshot Layout tanpa duplikasi Overview)

- Fokus: Menambahkan screenshot section di bawah WebView2 About dengan gaya mirip referensi teman (hero + grid variatif), tanpa memakai screenshot yang sudah dipakai di GAME OVERVIEW.
- Perubahan:
  - `GameDetailViewModel.cs`: tambah properti post-about:
    - `PostAboutScreenshotUrls` (maks 5),
    - `PostAboutHeroScreenshotUrl`,
    - `PostAboutTailScreenshotUrls`,
    - `HasPostAboutScreenshots`,
    - `HasPostAboutTailScreenshots`.
  - Filter screenshot mengecualikan `OverviewScreenshotUrl1/2/3` + dedupe URL agar tidak muncul ganda.
  - `GameDetailPage.xaml`: tambah section baru di bawah `AboutGameWebView`:
    - 1 hero image besar + tail grid `ItemsRepeater` untuk sisa screenshot,
    - klik/hover tetap reuse handler media existing (`MediaCard_Tapped`, pointer hover),
    - visibilitas pakai properti bool ViewModel agar aman dari generated x:Bind error.
- Build: `Build succeeded`, `0 Error(s)`, `68 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: Validasi runtime lintas game untuk memastikan pola 1–5 screenshot terlihat natural dan tidak bentrok dengan GAME OVERVIEW.

### 2026-05-19 (Crash Logging + Home ImageSource Guard)

- Fokus: crash saat spam/perf test yang keluar sebagai `-1073741189` dan perlu jejak log yang lebih kaya.
- Perubahan:
  - Root cause baru dari `crash.txt`: binding `HomePage` gagal convert URL ke `ImageSource` (`System.ArgumentException`).
  - `HomePage.xaml` diubah agar `ImageBrush` memakai helper aman (`SafeImageSource`) untuk `PosterUrl` dan `HeaderImageUrl`.
  - `HomePage.xaml.cs` ditambah `SafeImageSource(string?)` dengan fallback `ms-appx:///Assets/StoreLogo.png` saat URL invalid.
  - `App.xaml.cs` logging crash ditingkatkan: append log bertimestamp + source + thread/process untuk `WinUI UnhandledException`, `AppDomain.UnhandledException`, dan `TaskScheduler.UnobservedTaskException`.
  - `run_nexaplay.bat` ditingkatkan: saat output watch mendeteksi `Exited with error code`, script otomatis dump konteks crash ke `nexaplay_crash_context.log` (tail `crash.txt` + event Application/.NET Runtime/WER 15 menit terakhir).
- Build: `Build succeeded`, `0 Error(s)`, `70 Warning(s)` (non-blocking), Debug x64.
- Next: jalankan ulang `run_nexaplay.bat`, lakukan stress-test spam cepat, lalu kirim `crash.txt` + `nexaplay_crash_context.log` terbaru jika masih jatuh.

### 2026-05-19 (Hotfix spinner About WebView2 nyangkut)

- Fokus: loading ring section About tetap muter terus (munyer) dan konten tidak muncul pada beberapa flow revisit/navigasi cepat.
- Perubahan:
  - `GameDetailPage.xaml.cs`: pada jalur `Tier 1` (`_renderedForAppId` sama + `Height > 0`) sekarang memaksa `IsAboutContentLoading = false` sebelum `return`, supaya state loading tidak nyangkut.
  - `GameDetailPage.xaml.cs`: tambah watchdog `StartAboutLoadWatchdog()` (2.5s) setelah `NavigateToString`; jika callback JS tidak datang, fallback mematikan loading ring dan set tinggi minimum aman.
  - `GameDetailPage.xaml.cs`: `NavigationCompleted` sukses sekarang juga mengunci `_renderedForAppId` aktif, jadi revisit tetap stabil walau callback height terlambat.
  - `GameDetailPage.xaml.cs`: `WebMessageReceived` kini validasi `stamp` payload (`payload.Stamp == _expectedRenderStamp`) untuk menolak message stale dari render lama.
- Build: `Build succeeded`, `0 Error(s)`, `70 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: retest runtime skenario `A→list→A`, `A→B→A`, dan scroll cepat pada detail untuk memastikan ring About tidak nyangkut dan konten muncul konsisten.

### 2026-05-19 (Hotfix About kosong setelah patch spinner)

- Fokus: tidak crash, tapi konten About kosong pada sebagian flow revisit cepat.
- Perubahan:
  - `GameDetailPage.xaml.cs`: tambah guard `_hasValidRenderForCurrentApp` agar `Tier 1` hanya aktif setelah render HTML valid benar-benar diterima dari callback height (`WebMessageReceived`), bukan dari event navigasi awal.
  - `GameDetailPage.xaml.cs`: reset `_hasValidRenderForCurrentApp = false` sebelum setiap `NavigateToString` agar render lama tidak dianggap valid untuk request baru.
  - `GameDetailPage.xaml.cs`: hapus set `_renderedForAppId` dari `NavigationCompleted`; sekarang `_renderedForAppId` hanya di-set saat message height valid diterima.
- Build: `Build succeeded`, `0 Error(s)`, `70 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: validasi ulang flow `A→list→A` dan `A→B→A`; pastikan About tampil isi dan tidak blank saat revisit.

### 2026-05-19 (Tracing WebView2 About untuk kasus blank)

- Fokus: kasus About masih blank (tidak crash), perlu jejak event runtime agar akar penyebab terlihat jelas.
- Perubahan:
  - `GameDetailPage.xaml.cs`: tambah trace file `about_webview_trace.log` di folder output aplikasi (`AppContext.BaseDirectory`).
  - Tambah log detail di alur About: awal render (`rawLen`), abort empty source, hit `Tier1`, hasil `StripAboutGameHeading` (`cleanLen`), fallback strip bila hasil kosong, `NavigateToString` stamp, `NavigationCompleted` sukses/gagal, hasil `ExecuteScript`, payload `WebMessageReceived`, drop reason (appId/stamp/invalid height), apply height, dan watchdog release.
- Build: `Build succeeded`, `0 Error(s)`, `70 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: reproduksi blank sekali lagi lalu kirim isi `about_webview_trace.log` terbaru untuk analisa akar penyebab final.

### 2026-05-19 (Fix parser payload WebView2 + klarifikasi lokasi log)

- Fokus: About tetap blank walau trace aktif.
- Root cause dari trace:
  - `WebMessageReceived` menerima JSON valid (`{"appId":...,"stamp":...,"height":...}`) tapi parser C# gagal map properti lowercase, sehingga jatuh ke jalur `invalid-height` dan height tidak pernah diaplikasikan.
- Perubahan:
  - `GameDetailPage.xaml.cs`: parser `JsonSerializer.Deserialize` dibuat `PropertyNameCaseInsensitive=true`.
  - `GameDetailPage.xaml.cs`: `WebViewHeightPayload` ditandai `[JsonPropertyName("appId"|"stamp"|"height")]` agar mapping payload JS stabil.
  - Klarifikasi lokasi trace: log ditemukan di output runtime aktif, contoh `bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\about_webview_trace.log` (bisa berbeda dari `Debug-preview` tergantung cara run).
- Build: gagal sementara karena file lock `CS2012` (`Microsoft.UI.Xaml.Markup.Compiler`), bukan error logic code.
- Next: stop proses run/watch yang masih aktif lalu build ulang; retest About dan cek trace apakah `WebMessage apply` sudah muncul.

### 2026-05-19 (Cleanup final: hapus tracing debug WebView2 About)

- Fokus: kembalikan implementasi Smart Height Cache ke mode final (tanpa disk I/O tambahan).
- Perubahan:
  - `GameDetailPage.xaml.cs`: hapus seluruh tracing debug `about_webview_trace.log` (`LogAbout`, `Truncate`, field trace path/lock, dan seluruh call log di render/navigation/message/watchdog).
  - Parser JSON payload `WebMessageReceived` yang sudah diperbaiki tetap dipertahankan (`PropertyNameCaseInsensitive` + `[JsonPropertyName]`) agar height callback stabil.
  - Alur `Tier 1 / Tier 2 / Tier 3` tetap dipertahankan apa adanya.
- Build: `Build succeeded`, `0 Error(s)`, `70 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: smoke test ulang `A→A`, `A→B→A`, dan cold open setelah restart app untuk memastikan behavior final tetap smooth.

### 2026-05-19 (Stabilkan glitch WebView2 + native-only scroll)

- Fokus: kurangi glitch/flicker pada About WebView2 dan pastikan scroll selalu mengikuti ScrollViewer native.
- Perubahan:
  - `GameDetailPage.xaml.cs`: hentikan render prematur saat `OnNavigatedTo` (sebelum `LoadAsync` selesai) dan render dilakukan setelah data detail siap, untuk mengurangi transisi kosong→isi yang memicu glitch visual.
  - `GameDetailPage.xaml`: `AboutGameWebView` di-set `IsHitTestVisible="False"` agar WebView2 tidak menangkap wheel/pointer scroll; hasilnya scroll tetap pure native dari `ScrollViewer` parent.
  - Mekanisme Smart Height Cache (Tier1/Tier2/Tier3) tetap dipertahankan.
- Build: `Build succeeded`, `0 Error(s)`, `68 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: verifikasi UX bahwa area About tetap terbaca mulus saat scroll cepat; catatan: karena hit test dimatikan, link/interaksi di dalam konten WebView2 tidak bisa diklik (sesuai mode pure native scroll).

### 2026-05-19 (Hotfix crash spam-load Game Detail)

- Fokus: crash acak saat user spam klik/navigasi cepat (load detail bertumpuk, pindah game cepat, dan scroll/media update bersamaan).
- Perubahan:
  - `GameDetailPage.xaml.cs`: tambah guard lifecycle (`_isPageActive`), navigation session gate, dan cancellation token per navigasi agar async lama tidak meneruskan update UI setelah page berpindah.
  - `GameDetailPage.xaml.cs`: event WebView2 `WebMessageReceived` sekarang validasi payload JSON (`appId`, `stamp`, `height`) untuk menolak message lama/stale dari render sebelumnya.
  - `GameDetailPage.xaml.cs`: timer carousel diberi guard saat page tidak aktif / belum loaded / detail masih loading.
  - `GameDetailViewModel.cs`: `LoadAsync` diberi versi request (`_loadVersion`) + cancellation checks agar hasil fetch lama tidak menimpa state fetch terbaru saat klik cepat.
  - Entry tambahan non-fokus yang sempat ditulis sebelumnya sudah dihapus agar log kembali murni mengikuti alur fokus kamu.
- Build: `Build succeeded`, `0 Error(s)`, `68 Warning(s)` (non-blocking), Debug x64.
- Next: stress-test runtime dengan skenario spam (A→list→A, A→B→A, scroll+klik media cepat) dan cek apakah `crash.txt` masih terisi.

### 2026-05-19 (Smart Height Cache + Fix 4 masalah WebView2)

- Fokus: (1) Konten bawah terpotong. (2) Heading "About the Game" duplikat dari HTML Steam. (3) Spacing heading terlalu rapat. (4) Flash loading ring setiap revisit — implementasi smart height cache.

- Root cause terpotong: Buffer height kurang + timeout terlalu pendek.
- Root cause heading duplikat: Steam `detailed_description` mengandung `<h2>About the Game</h2>` sebagai heading pertama, sementara kita sudah punya section divider native XAML. Fix: `StripAboutGameHeading()` regex strip.
- Root cause flash loading: `IsAboutContentLoading=true` selalu di-set tanpa cek apakah height sudah diketahui.

- Perubahan utama:
  1. `GameDetailPage.xaml.cs` — Static `_heightCache: Dictionary<int, double>` + `_renderedForAppId: int` sebagai field class.
  2. `RenderAboutGameWebView()` — Logika 3-tier:
     - **Tier 1**: AppId sama + WebView2 sudah punya height → `return` sepenuhnya (0ms, skip NavigateToString).
     - **Tier 2**: AppId pernah dikunjungi → `_heightCache.TryGetValue()` → set height instan, `IsAboutContentLoading = false`, render di background tanpa loading ring.
     - **Tier 3**: Cold open → `IsAboutContentLoading = true` seperti biasa.
  3. `AboutGameWebView_WebMessageReceived()` — Setelah height diterima dari JS: simpan `_heightCache[appId] = height` + update `_renderedForAppId = appId`.
  4. `OnNavigatedFrom()` — Hapus `AboutGameWebView.Close()` agar WebView2 instance tetap hidup (mendukung Tier 1).
  5. `GameDetailPage.xaml` — Tambah ProgressRing overlay untuk about section (visible saat `IsAboutContentLoading`), hapus native TextBlock "ABOUT THE GAME" (sudah terkandung di HTML Steam sebagai h2 heading).
  6. CSS update: `line-height: 1.85`, heading `margin: 28px 0 16px 0`, `p margin: 0 0 14px 0`, `li margin-bottom: 6px`.
  7. JS height measurement: buffer `+32px`, timeout `1500ms`, `window.onload` + `setTimeout(100ms)`.
  8. `StripAboutGameHeading()` — Regex strip `<h1/h2/h3>About the/this Game</h1/h2/h3>` dari HTML sebelum render.

- Source WebView2: `DisplayRichDescription` = `DetailedDescription ?? AboutTheGame` (tidak ada filter comparison).
- Memory overhead _heightCache: `~12 byte` per game pernah dikunjungi — tidak signifikan.
- Build: `0 Error(s)`, `68 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: Test revisit flow (A→list→A, A→B→A), pastikan Tier1 dan Tier2 berjalan smooth.

### 2026-05-19 (Fix About duplikat + heading NexaPlay style + gap bawah)

- Fokus: (1) Konten "About the Game" tampil dua kali. (2) Heading di dalam WebView2 tidak berformat NexaPlay. (3) Gap besar antara konten dan "Additional Information".
- Root cause duplikat: `RenderAboutGameWebView()` menggabungkan `about_the_game` + `detailed_description`. Untuk beberapa game, `detailed_description` berisi subset dari `about_the_game` (bukan identik) sehingga keduanya ikut tampil. Fix: hapus `detailed_description` dari render — `about_the_game` sudah berisi seluruh konten.
- Root cause gap bawah: JS mengukur `document.documentElement.scrollHeight` yang include extra viewport space. Fix: wrap konten dalam `div#nexacontent`, ukur `scrollHeight` dari div itu saja.
- Perubahan:
  1. `GameDetailPage.xaml.cs` `RenderAboutGameWebView()`: Hapus `detailedHtml` — hanya render `aboutHtml`. Content dibungkus `<div id="nexacontent">`.
  2. CSS heading baru: `h1,h2,h3,h4,.bb_h1,.bb_h2,.bb_h3` → `text-transform: uppercase`, `letter-spacing: 2px`, `border-left: 4px solid #FFFFFF`, `padding-left: 12px` — sesuai NexaPlay section header style.
  3. JS `NavigationCompleted`: Ukur `el.scrollHeight` dari `#nexacontent` untuk tinggi yang akurat.
- Build: `0 Error(s)`, `68 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: Restart app, validasi heading UPPERCASE + garis putih kiri, tidak ada duplikat konten, gap bawah berkurang.

### 2026-05-19 (Refactor WebView2 About — gabung konten + fix crash)

- Fokus: Fix konten "About the Game" kosong, gabungkan `about_the_game` + `detailed_description` dalam satu WebView2, dan fix crash `XamlParseException` saat launch.
- Root cause konten kosong: WebView2 mendengarkan `DisplayDetailedDescription` yang return empty jika `detailed_description == about_the_game` (mayoritas game Steam).
- Root cause crash: Stale build artifact (`obj/`) dari perubahan sebelumnya. Diselesaikan dengan `Clean` + rebuild.
- Perubahan:
  1. `GameDetailViewModel.cs`: Property baru `DisplayAboutTheGame` → `Detail?.AboutTheGame`. `DisplayDetailedDescription` tetap ada untuk konten bonus yang berbeda. `_isAboutContentLoading` masih ada di ViewModel tapi tidak digunakan di UI (page-level `IsDetailLoading` yang handle loading).
  2. `GameDetailPage.xaml.cs`: Trigger berubah ke `DisplayAboutTheGame`. Handler baru `RenderAboutGameWebView()` menggabungkan about + detailed (jika berbeda) dalam satu HTML. CSS heading diperbaiki: h1=20px, h2=17px, h3=15px, bb_h1/bb_h2/bb_h3 sesuai. Separator `.nexa-separator` antar konten. Hapus referensi `IsAboutContentLoading`.
  3. `GameDetailPage.xaml`: Hapus ProgressRing about terpisah — loading sudah di-handle page-level `IsDetailLoading` (ProgressRing di baris 98-106). WebView2 langsung visible tanpa binding loading.
  4. `GameDetailPage.xaml`: Grid GAME OVERVIEW `Height` dari 520 → 480 (dari batch sebelumnya).
- Catatan: Tidak ada API call ganda — `LoadAsync` → `GetDetailAsync(appId)` fetch semuanya (about, detailed, screenshots, metadata) dalam satu panggilan. WebView2 hanya merender data dari `Detail` yang sudah tersedia.
- Build: Clean + rebuild `Build succeeded`, `0 Error(s)`, `68 Warning(s)` (non-blocking).
- Next: Restart app, validasi konten "About the Game" tampil untuk semua game.

### 2026-05-19 (Fix WebView2 konten terpotong — LayoutCycle-safe height)

- Fokus: WebView2 "About the Game" konten terpotong karena JS height feedback dihapus di batch sebelumnya.
- Root cause sesi sebelumnya: JS `postMessage(scrollHeight)` → `sender.Height = height` dilakukan secara langsung di `WebMessageReceived` (masih dalam layout pass yang sama) → WinUI 3 mendeteksi layout cycle.
- Fix proper: Kembalikan JS height feedback, tapi defer `sender.Height = height` via `DispatcherQueue.TryEnqueue()`. Assignment sekarang terjadi di frame berikutnya, di luar layout pass aktif — sehingga tidak ada layout cycle.
- Tambahan: `NavigationCompleted` kini menjalankan JS `reportHeight()` langsung + `window.onload` + `setTimeout(600ms)` sebagai fallback untuk gambar lambat. Dengan ini, konten "About the Game" selalu menampilkan tinggi penuh meskipun ada gambar embedded.
- Perubahan:
  1. `GameDetailPage.xaml.cs`: `AboutGameWebView_NavigationCompleted` memanggil JS multi-trigger (`reportHeight()`, `onload`, `setTimeout 600ms`).
  2. `GameDetailPage.xaml.cs`: `AboutGameWebView_WebMessageReceived` mengeset `sender.Height` via `DispatcherQueue.TryEnqueue()` — deferred, aman dari layout cycle.
  3. `GameDetailPage.xaml`: `MaxHeight="1200"` dihapus dari WebView2, hanya `MinHeight="300"` yang dipertahankan sebagai placeholder saat loading.
- Build: `Build succeeded`, `0 Error(s)`, `67 Warning(s)` (non-blocking), `OutDir=Debug-preview`.
- Next: Restart app dan validasi bahwa "About the Game" menampilkan konten lengkap tanpa terpotong. Jika ada LayoutCycle lagi, cek `RootLayout_SizeChanged` sebagai kandidat berikutnya.

### 2026-05-19 (WebView2 Migration for "About the Game")

- Fokus: Migrasi parser Native HTML ke WebView2 untuk sesi "About the Game" demi mencapai 100% UI Parity dengan Steam.
- Perubahan: 
  - Menghapus parser `RichBlock` dari `GameDetailViewModel.cs` dan menghapus `RichBlock.cs` serta `RichBlockTemplateSelector.cs`.
  - Mengimplementasikan `<WebView2>` di `GameDetailPage.xaml` untuk merender konten HTML kotor langsung.
  - Menyuntikkan CSS gelap via C# agar *background* WebView transparan dan *scrollbar* tersembunyi.
  - Mengimplementasikan komunikasi JavaScript (`window.chrome.webview.postMessage`) ke C# (`WebMessageReceived`) untuk membuat tinggi `WebView2` dinamis tanpa *double scrollbar*.
  - Menerapkan pembersihan memori ketat (`WebView2.Close()`) pada `OnNavigatedFrom` untuk menghindari *memory leak*.
- Build: Succeeded, 0 Error(s).
- Next: Evaluasi performa navigasi halaman detail game, atau melanjutkan prioritas navigasi ("Home data parity" / "Update system parity").

### 2026-05-19 (Rich Content & Crash Fixes)

- Fokus: Implementasi parser "Native Rich HTML Renderer" dan perbaikan crash UI.
- Perubahan: 
  - Membuat `RichBlockTemplateSelector` dan `RichBlock` model untuk merender teks, gambar, video, list, dan header native WinUI 3.
  - Memperbaiki parser Regex di `GameDetailViewModel` untuk menerjemahkan `<br>`, `<p>`, dan `<li>` menjadi baris teks native.
  - Mengganti `ItemsRepeater` dengan `ItemsControl` untuk mengatasi *Layout Cycle Crash* (`0xC000027B`) saat men-scroll gambar dinamis.
  - Menyatukan sesi "About the Game" dan "Detailed Description" menjadi satu layout komprehensif.
- Build: Succeeded, verified XAML parsing.
- Next: Evaluasi stabilitas keseluruhan halaman detail game dan optimasi caching gambar jika diperlukan.

### 2026-05-19 (polish ABOUT THE GAME section)

- Fokus: merapikan layout teks "ABOUT THE GAME" dan menyisipkan "GAME OVERVIEW" di halaman Game Detail.
- Perubahan: 
  - Mengubah logika `FormatAboutText` agar mengganti tag `<br>` dengan `\n` (newline) dan bukannya spasi. Hal ini menjaga struktur paragraf asli dari Steam API.
  - Memperbaiki binding `DisplayDetailedDescription` di `GameDetailViewModel` untuk mengembalikan string kosong apabila isinya identik dengan "About The Game", sehingga section "DETAILED DESCRIPTION" ganda tidak dirender di layar.
  - Menambahkan section "GAME OVERVIEW" di atas "ABOUT THE GAME" yang memuat deskripsi pendek dan 3-image grid layout screenshot dengan efek _hover scale_.
- Build: `Build succeeded`, 0 Error(s).
- Next: Menyambungkan navigasi UI "Cek Bypass" ke halaman Bypass Games atau melanjut prioritas UI selanjutnya.

### 2026-05-19 (fix crash System Requirement x:Bind NullReferenceException)

- Fokus: crash `0xC000027B` persisten yang membuat app tertutup sendiri.
- Root cause: Bukan di MEDIA, melainkan di `SystemRequirement`. `x:Bind` WinUI 3 mengalami crash diam-diam (stowed exception) saat mengevaluasi path bersarang sebagai argumen fungsi (contoh: `FormatRequirementItems(ViewModel.Detail.PcRequirementsMinimum)`) ketika objek parent (`ViewModel.Detail`) bernilai `null` saat proses load pertama kali.
- Perubahan:
  - Membuat helper method statis `FormatRequirementsMin(GameDetailEntry? detail)` dan `FormatRequirementsMax(GameDetailEntry? detail)` di `GameDetailPage.xaml.cs`.
  - Mengubah argumen fungsi `x:Bind` di `GameDetailPage.xaml` pada ItemsRepeater System Requirements menjadi passing keseluruhan objek `ViewModel.Detail`, sehingga jika null akan di-_pass_ sebagai null dengan aman tanpa error _null-propagation_.
- Build: `Build succeeded`, `0 Error(s)`.
- Next: Restart app, lalu buka Game Detail. Seharusnya tidak akan ada crash sama sekali dan UI tampil secara penuh.

### 2026-05-19 (pendekatan binding chain ViewModel.Detail.Screenshots)

- Fokus: crash `0xC000027B` persisten meski sudah pakai DispatcherQueue defer.
- Perubahan: Hapus `OnDetailChanged` sepenuhnya dari `GameDetailViewModel.cs`. Ganti binding XAML MEDIA `ItemsRepeater` dari `ViewModel.Screenshots` → `ViewModel.Detail.Screenshots`. x:Bind chain WinUI 3 otomatis update saat `PropertyChanged("Detail")` tanpa perlu `OnPropertyChanged("Screenshots")` manual. WinUI 3 x:Bind juga handle null-propagation jika `Detail` null.
- Build: `Build succeeded`, `0 Error(s)`, `OutDir=Debug-preview`.
- Next: Restart app, test navigasi ke Game Detail — harus tidak crash dan MEDIA carousel muncul.

### 2026-05-19 (fix crash MEDIA + DispatcherQueue defer)

- Fokus: crash `0xC000027B` saat masuk Game Detail setelah fix OnDetailChanged sebelumnya.
- Root cause: `OnDetailChanged` memicu `OnPropertyChanged("Screenshots")` saat `IsDetailLoading = true` → ScrollViewer masih `Collapsed`. WinUI 3 ItemsRepeater crash saat parent di-reveal ke `Visible`. Ini adalah `STATUS_STOWED_EXCEPTION` — exception di dalam binding/realization pipeline yang di-stow lalu re-throw.
- Perubahan: `OnDetailChanged` di `GameDetailViewModel.cs` diubah agar defer notifikasi via `DispatcherQueue.TryEnqueue`. Notifikasi `Screenshots`/`Movies`/`Categories` sekarang baru dikirim di frame berikutnya, setelah `IsDetailLoading = false` dan ScrollViewer sudah `Visible`.
- Build: `Build succeeded`, `0 Error(s)`, `OutDir=Debug-preview`.
- Next: Restart app dan validasi runtime — carousel MEDIA harus muncul tanpa crash.

### 2026-05-19 (fix MEDIA carousel kosong)

- Fokus: MEDIA section di GameDetailPage tidak menampilkan screenshot setelah hotfix crash sebelumnya.
- Root cause: `Screenshots`, `Movies`, `Categories` adalah computed property (`Detail?.Screenshots`, dst). Ketika `Detail` di-assign dari `LoadAsync`, `OnPropertyChanged("Screenshots")` tidak pernah dipanggil — sehingga `ItemsRepeater` MEDIA tidak refresh.
- Perubahan: Tambah `partial void OnDetailChanged(GameDetailEntry? value)` di `GameDetailViewModel.cs` yang memanggil `OnPropertyChanged` untuk `Screenshots`, `Movies`, dan `Categories` setiap kali `Detail` berubah. Ini menangani semua titik assignment sekaligus (LoadAsync dan SelectScreenshot).
- Build: `Build succeeded`, `0 Error(s)`, `67 Warning(s)` (MVVMTK0045 + NU1902, non-blocking), `OutDir=Debug-preview`.
- Next: Validasi visual runtime — screenshot carousel harus muncul di MEDIA section. Jika sudah oke, lanjut polish area lain di GameDetailPage.

### 2026-05-19

- Fokus: hotfix kedua untuk crash `0xc000027b` saat masuk Game Detail.
- Perubahan:
  1. Di `GameDetailPage.xaml`: Mengubah `Mode=OneWay` menjadi `Mode=OneTime` pada _binding_ `IsSelected` di `DataTemplate` `ScreenshotEntry` untuk mencegah _crash_ XAML akibat properti statis.
  2. Di `GameDetailViewModel.cs`: Memperbaiki _double-assignment_ pada properti `Detail` saat `LoadAsync` yang memicu _layout cycle crash_ di WinUI 3 `ItemsRepeater`. `Detail` sekarang dimutasi sebelum di-_assign_ ke _ObservableProperty_.
- Build: `Build succeeded`, `0 Error(s)`.
- Next: Validasi visual ulang di runtime. Jika aman, lanjut penataan `ABOUT THE GAME` dan `SYSTEM REQUIREMENT`.

### 2026-05-19

- Fokus: hotfix crash saat masuk Game Detail setelah perubahan carousel media.
- Perubahan: card carousel `MEDIA` diganti dari `Button` dengan `PopularGameCardStyle` menjadi `Border` interaktif (`Tapped`, hover scale manual). Ini mengurangi risiko runtime crash WinUI dari template/style global saat item media direalisasi. Output Debug normal juga dibuild ulang, bukan hanya Debug-preview.
- Build: `Build succeeded`, `0 Error(s)`, `2 Warning(s)` dengan build Debug normal.
- Next: user perlu retest masuk Game Detail; jika masih crash, cek Event Viewer Application untuk event NexaPlay terbaru.

### 2026-05-19

- Fokus: ubah media Game Detail sesuai referensi carousel teman user.
- Perubahan: label section `SCREENSHOTS` diganti `MEDIA`; preview screenshot besar lama dihapus; daftar screenshot dibuat carousel horizontal full-width dengan panah kiri/kanan, hover scale kecil, dan klik gambar membuka overlay/lightbox; section `TRAILERS` dihapus sementara dari UI.
- Build: `Build succeeded`, `0 Error(s)`, `2 Warning(s)` dengan `OutDir=Debug-preview`.
- Next: validasi visual runtime untuk ukuran carousel, crop thumbnail, dan posisi overlay; jika sudah pas lanjut polish `ABOUT THE GAME` dan `SYSTEM REQUIREMENT`.

### 2026-05-19

- Fokus: polish lanjutan Game Detail layout.
- Perubahan: metadata strip (`APP ID`, `RELEASE DATE`, `DEVELOPER`, `PUBLISHER`, `PRICE`) dibuat rata kanan; badge `STANDARD/PREMIUM` dan `DENUVO` dipindahkan ke kolom kanan; khusus Denuvo ditambahkan tombol UI-only `Cek Bypass`; judul `SYSTEM` diganti `SYSTEM REQUIREMENT` dan dikeluarkan dari kotak; isi requirements dibuat lebih rapat, tanpa label teks `MINIMUM/RECOMMENDED` tambahan di bawah tombol, dan formatter menghapus awalan `Minimum:`/`Recommended:` dari isi spec.
- Build: `Build succeeded`, `0 Error(s)`, `68 Warning(s)` dengan `OutDir=Debug-preview`.
- Next: validasi visual runtime; nanti tombol `Cek Bypass` diarahkan ke halaman Bypass Games setelah behavior detail disepakati.

### 2026-05-19

- Fokus: polish layout Game Detail sesuai arahan user.
- Perubahan: metadata `APP ID`, `RELEASE DATE`, `DEVELOPER`, `PUBLISHER`, `PRICE` dipindah menjadi strip horizontal di atas screenshot; panel kanan diganti menjadi `SYSTEM` requirements card dengan toggle `Minimum`/`Recommended`; badge `STANDARD/PREMIUM` dan `DENUVO` dipindah ke bawah genre di hero; sticky action bar dibuat compact di tengah dan hanya membungkus tiga tombol `Add Game`, `Online-Fix`, `Restart Steam`; teks unavailable pada tombol Online-Fix dihapus.
- Build: `Build succeeded`, `0 Error(s)`, `68 Warning(s)` dengan `OutDir=Debug-preview`.
- Next: validasi visual runtime, lalu poles spacing/tinggi requirements dan action bar bila screenshot runtime masih belum pas.

### 2026-05-19

- Fokus: membuat handoff/prompt AI baru lebih detail.
- Perubahan: dokumen ini diperluas dengan status aktif, aturan UI wajib, metadata rules, workflow build, dan instruksi auto-update handoff.
- Build: `Build succeeded`, `0 Error(s)`, `2 Warning(s)` dengan `OutDir=Debug-preview`.
- Next: lanjut polish Game Detail sticky action bar dan label `STANDARD/PREMIUM/DENUVO`, lalu lanjut konten About Game/media.

### 2026-05-18

- Fokus: Game Detail metadata dan UI.
- Perubahan: protection/Denuvo dihitung dari `fix_games.json`, `new_fix_games.json`, dan `steam_games/steam_games.json`; sticky action/status bar ditambahkan; price diarahkan dari runtime catalog.
- Build: `Build succeeded`, `0 Error(s)` dengan output preview.
- Next: validasi visual runtime dan poles detail layout/action behavior.
