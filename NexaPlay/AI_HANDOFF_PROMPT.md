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
