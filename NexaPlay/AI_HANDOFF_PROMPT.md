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
7. NexaPlay/.agents/rules/antigravity-rtk-rules.md (aturan menggunakan rtk)


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


## 10. Update Log dan catatan AI
Tambahkan catatan baru di atas bagian ## 10. Update Log Ringkas setiap selesai batch penting.
Format:

```text
Tanggal:
- Fokus:
- Perubahan:
- Build:
- Next:
```

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
