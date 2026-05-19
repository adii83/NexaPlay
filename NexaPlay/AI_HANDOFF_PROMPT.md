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
