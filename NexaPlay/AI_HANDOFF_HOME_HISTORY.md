# AI Handoff - Home Page History (Arsip)

Dokumen ini menampung riwayat detail perbaikan yang dominan terkait **Home Page**.
Tujuan: menjaga `AI_HANDOFF_PROMPT.md` tetap fokus ke konteks aktif terbaru (saat ini: **Games Page**), tanpa kehilangan jejak keputusan lama di Home.

## Cara pakai

1. Saat fokus aktif bukan Home, baca ringkas dokumen ini untuk memahami konteks lama.
2. Saat mengerjakan Home lagi, gunakan dokumen ini sebagai sumber detail historis utama.
3. Untuk update harian lintas fitur, tetap catat di `AI_HANDOFF_PROMPT.md` bagian `## 10. Update Log Ringkas`.

### 2026-05-21 (New Fix AppId Mode + ETag Fetch)

- Fokus: Memperbaiki kondisi kosong pada `New Bypass Games` ketika `new_fix_games.json` hanya berisi appid.
- Perubahan:
  - `IMetadataService` ditambah `GetNewFixAppIdsAsync()`.
  - `MetadataService`:
    - menambah fetch daftar appid berbasis ETag (`If-None-Match`) untuk:
      - `appid_populer.json`
      - `new_fix_games.json`
    - menambah cache file + etag file lokal:
      - `appid_populer_cache.json`, `appid_populer.etag`
      - `new_fix_appids_cache.json`, `new_fix_games.etag`
    - parser appid sekarang toleran untuk format array angka, object keyed appid, maupun nested object/array yang mengandung `appid`.
  - `HomeViewModel`:
    - section `New Bypass Games` sekarang baca appid via `GetNewFixAppIdsAsync()`,
    - lalu hydrate konten (judul/publisher/premium) dari metadata appid,
    - cover tetap diperkaya dari API detail jalur SteamGridDB + Steam AppDetails.
- Build: `Build succeeded` (`0 Error(s)`, `3 Warning(s)` WMC1506).
- Next: Uji runtime dengan mengganti isi `new_fix_games.json` untuk memastikan perubahan appid cepat terlihat tanpa menunggu TTL panjang.

### 2026-05-21 (Home Source Switch: New Bypass dari new_fix_games.json)

- Fokus: Mengganti sumber data section `New Bypass Games` dari `fix_games.json` ke `new_fix_games.json`.
- Perubahan:
  - `IBypassGamesDataService` ditambah method `GetNewFixesAsync()`.
  - `BypassGamesDataService` ditambah pipeline cache+download khusus `new_fix_games.json`:
    - URL: `AppConstants.NewFixGamesUrl`
    - Cache file: `AppConstants.NewFixGamesCacheFileName`
    - TTL mengikuti `SafetyNetTtl` seperti source lain.
  - `HomeViewModel` untuk section `New Bypass Games` sekarang memanggil `GetNewFixesAsync()` (bukan `GetAllFixesAsync()`).
- Build: `Build succeeded` (`0 Error(s)`, `3 Warning(s)` WMC1506).
- Next: Validasi runtime bahwa item carousel `New Bypass Games` benar-benar berubah mengikuti isi `new_fix_games.json`.

### 2026-05-21 (New Bypass Hero Fixed Height + Center Crop + Direct API Path)

- Fokus: Menetapkan hero `New Bypass Games` ke ukuran fix, center-crop konsisten, dan memastikan sumber cover memakai jalur API SteamGridDB + Steam AppDetails.
- Perubahan:
  - `HomePage.xaml`:
    - Hero container di-set fix `Height="560"` (tidak lagi dinamis).
    - `ImageBrush` hero diset `Stretch="UniformToFill"` + `AlignmentX="Center"` + `AlignmentY="Center"` untuk crop otomatis di tengah.
  - `HomePage.xaml.cs`:
    - Logika dynamic-height berdasarkan rasio dihapus total.
  - `HomeViewModel.cs`:
    - Enrichment `RecentFixes` sekarang memanggil `ISteamStoreService.GetDetailAsync(appId)` (jalur merged API SteamGridDB + Steam AppDetails), bukan dari katalog JSON.
    - Cover hero prioritas: `assets.library_hero_2x`; jika tidak ada, fallback ke `library_capsule_2x`, lalu ke poster lama.
- Build: `Build succeeded` (`0 Error(s)`, `3 Warning(s)` WMC1506).
- Next: Uji runtime untuk memastikan semua item `New Bypass Games` sudah konsisten tinggi 560 dengan crop tengah, dan verifikasi beberapa appid sample benar-benar mengambil URL `library_hero_2x`.

### 2026-05-21 (New Bypass Games Hero Cover: library_hero_2x + Native Ratio)

- Fokus: Mengganti source cover section `New Bypass Games` ke `library_hero_2x` dan menyesuaikan tinggi card agar mengikuti rasio asli asset Steam.
- Perubahan:
  - `HomeViewModel.cs`:
    - `RecentFixes` sekarang diperkaya metadata per-appid lewat `IMetadataService`.
    - Jika tersedia, `PosterUrl` item `FixEntry` diisi ulang dari `LibraryHero2xUrl` (prioritas utama untuk carousel hero).
  - `HomePage.xaml`:
    - Container hero section diberi nama `NewBypassHeroContainer` dan hook `SizeChanged`.
    - Tinggi default dinaikkan agar tidak terlalu terpotong saat ratio native diterapkan.
  - `HomePage.xaml.cs`:
    - Ditambahkan rasio native Steam hero `3840:1240`.
    - Tinggi container dihitung dinamis: `height = width * 1240 / 3840` dengan minimum aman `420`.
- Build: Kompilasi sempat jalan sampai tahap akhir, tetapi copy output gagal karena file lock `NexaPlay.exe` masih dipakai proses app aktif (`NexaPlay (22264)`), bukan error logic perubahan.
- Next: Tutup app yang sedang berjalan lalu build ulang. Setelah itu validasi visual apakah hero section sudah tampil pas dengan source `library_hero_2x` tanpa crop berlebihan.

### 2026-05-21 (Home Popular Auto Fill: API Enrichment Trigger Fix)

- Fokus: Memperbaiki jalur `auto fill` agar card tambahan otomatis ikut memanggil sumber cover API asli.
- Perubahan:
  - `HomeViewModel.cs`:
    - Pada `UpdatePopularLayoutAsync`, setiap batch `more` hasil isi otomatis sekarang langsung memicu `EnrichPopularCoversFromApiAsync(more)`.
    - Pada `EnsurePopularFilledRowsAsync`, setiap `batch` pengisi sisa kolom juga langsung memicu `EnrichPopularCoversFromApiAsync(batch)`.
  - Hasilnya: tidak hanya load awal dan tombol `Load More`, tetapi juga auto-fill karena resize/layout kini ikut jalur cover API (`library_capsule_2x`).
- Build: Validasi build kena lock file sementara (`CS2012`, `intermediatexaml\\NexaPlay.dll` dipakai proses `Microsoft.UI.Xaml.Markup.Compiler`), bukan error logic patch.
- Next: Ulang build saat lock lepas / app ditutup, lalu uji visual: auto-fill batch baru harus upgrade cover dari header -> `library_capsule_2x` ketika data API tersedia.

### 2026-05-21 (Home Popular Load More: API Cover Enrichment Hook)

- Fokus: Menutup gap di mana card hasil `Load More` belum langsung diperkaya cover dari API asli.
- Perubahan:
  - `HomeViewModel.cs`:
    - `LoadPopularGamesInBackgroundAsync` sekarang memanggil enrichment API dengan scope item yang sedang dirender.
    - `LoadMorePopularGamesAsync` sekarang memanggil enrichment API khusus batch `newGames` setelah append ke grid.
    - `EnrichPopularCoversFromApiAsync` diubah mendukung parameter `targetGames` agar bisa enrichment terfokus (bukan selalu seluruh list).
    - Ditambahkan `_apiEnrichLock` untuk mencegah overlap request enrichment ketika user menekan `Load More` berulang cepat.
- Build: Menunggu verifikasi build sesudah patch.
- Next: Uji runtime Home dengan klik `Tampilkan Lebih Banyak` beberapa kali dan pastikan game baru ikut beralih dari header ke `library_capsule_2x` saat API tersedia.

### 2026-05-21 (Home Popular Cover API Fix: SteamGridDB + Steam AppDetails Priority)

- Fokus: Memperbaiki sumber cover `Popular Games` agar kembali mengutamakan payload API asli (`steamgriddb` + `steamappdetails`), bukan berhenti di data katalog/header.
- Perubahan:
  - `HomeViewModel.cs`:
    - Menambahkan ulang method `EnrichPopularCoversFromApiAsync()` yang sebelumnya hilang (penyebab build error CS0103).
    - Enrichment sekarang memproses kandidat game yang masih kosong `library_capsule_2x` atau masih fallback ke `header`.
    - Untuk tiap kandidat, app memanggil `ISteamStoreService.GetDetailAsync(appId)` (cache-first, lalu API asli), lalu mengambil `LibraryCapsule2xUrl` dari payload merge `assets.library_capsule_2x`.
    - Jika URL portrait API tersedia, item game populer di-`replace` di `ObservableCollection` agar binding UI langsung refresh ke cover portrait asli.
  - Prinsip fallback dipertahankan aman: jika API belum punya `library_capsule_2x`, item tetap bisa pakai `header` sementara, tanpa memutus list load.
- Build: Baseline awal gagal (`CS0103` method hilang). Setelah batch ini, build diulang untuk validasi.
- Next: Verifikasi visual di `Home` bahwa card populer yang sebelumnya hanya tampil `header` kini terisi poster portrait API (`library_capsule_2x`) ketika tersedia.

### 2026-05-21 (Home Popular Cover Logic: Smart URL Reconstruction)

- Fokus: Menangani masalah di mana gambar poster (600x900) tidak muncul pada *game* tertentu, padahal API menyediakan gambar *header* atau *hero banner* di direktori CDN yang sama.
- Perubahan:
  - `MetadataService.cs`: Memperluas area pencarian kata kunci JSON untuk poster vertikal menjadi lebih fleksibel, mencari di bawah key `"library_600x900_2x"`, `"library_600x900"`, dan `"library_capsule_2x"`.
  - `GameEntry.cs`: Menambahkan sistem pintar perakit URL (*Smart URL Reconstruction Fallback*). Berkat struktur CDN Steam yang konsisten, jika metadata JSON gagal menyetor URL poster 600x900 tetapi sukses menyetor URL *header.jpg*, properti `PopularCoverImageUrl` akan merakit paksa (mengganti *string* secara internal) URL *header* tersebut menjadi URL `/library_600x900_2x.jpg`. Ini membuat ribuan *game* yang tadinya tampil abu-abu "NO CONTENT" kini memiliki poster vertikal cantik beresolusi tinggi dengan sempurna!
- Build: `Build succeeded`, `0 Error(s)`, `3 Warning(s)` WMC1506.
- Next: Tes penglihatan visual ke aplikasi. Jika ada *game* spesifik yang tidak memiliki poster, ia akan terpotong secara elegan dari gambar *header* biasa.

### 2026-05-21 (Home Popular Autofill Resilience: Missing Metadata Bypass)

- Fokus: Memperbaiki masalah *autofill* (atau tombol *Load More*) yang terkadang berhenti mendadak di tengah jalan, menyisakan ruang bolong di baris paling bawah.
- Perubahan:
  - `HomeViewModel.cs` (`LoadNextPopularPageAsync`): Mengubah logika tarikan data (*fetch*). Sebelumnya, sistem sekadar mengambil `N` buah *AppId* dan mencoba mengambil metadatanya. Jika beberapa *game* gagal ditarik (karena metadatanya kosong atau belum ada di *database* API), fungsi ini akan mengembalikan data kurang dari yang diminta, yang membuat fungsi *autofill* menyerah (*break*) lebih awal dari seharusnya. Kini, fungsi tersebut menggunakan perulangan persisten (*while loop*) yang akan terus memakan *AppId* dari katalog hingga jumlah data valid (tidak `null`) yang diminta terpenuhi seratus persen (atau sampai katalog benar-benar habis).
- Build: `Build succeeded`, `0 Error(s)`, `3 Warning(s)` WMC1506.
- Next: Tes mengeklik "Tampilkan Lebih Banyak" beberapa kali untuk memastikan baris selalu terisi penuh (rata kanan) tanpa ada sela kosong, terlepas dari ada atau tidaknya metadata *game* yang rusak di *database*.

### 2026-05-21 (Home Popular UI State Cache: Scroll & Navigation Persistence)

- Fokus: Mencegah hilangnya status *scroll* (halaman kembali ke atas) dan hilangnya *ViewModel instance* (kembali ke *default 40 items*) saat pengguna kembali dari halaman *Game Detail*.
- Perubahan:
  - `HomePage.xaml`: Menambahkan `NavigationCacheMode="Required"` pada level `<Page>`. Secara *default*, WinUI 3 akan menghancurkan dan membuat ulang *Page* (serta *ViewModel*-nya yang *transient*) setiap kali ada navigasi bolak-balik. Dengan mewajibkan *cache*, *frame* navigasi akan menyimpan wujud fisik *Page* (termasuk posisi *scroll bar* pada *ScrollViewer*) di dalam memori. Saat mengeklik *Back*, pengguna disajikan objek yang sama persis seperti sebelum mereka pergi.
- Build: `Build succeeded`, `0 Error(s)`, `3 Warning(s)` WMC1506.
- Next: Tes alur navigasi dari Home -> Game Detail -> Back untuk memastikan kelancaran memori sesi aplikasi.

### 2026-05-21 (Home Popular State Persistence: Fix Reset on Resize/Navigate)

- Fokus: Memperbaiki *bug* di mana koleksi "Popular Games" selalu mereset isinya kembali ke jumlah awal (membuang data yang sudah di-*load more*) ketika layar di-*resize*, *fullscreen*, atau saat pengguna kembali dari halaman *Game Detail*.
- Perubahan:
  - `HomeViewModel.cs` (`LoadPopularGamesInBackgroundAsync`): Menambahkan penjagaan (caching) di awal fungsi. Jika `_allPopularAppIds` dan `PopularGames` sudah memiliki isi, fungsi akan langsung `return` tanpa melakukan *fetch* ulang yang menghancurkan memori sesi pengguna. Ini menjaga posisi *scroll* saat kembali dari *Game Detail*.
  - `HomeViewModel.cs` (`UpdatePopularLayoutAsync`): Mengubah kalkulasi variabel `needed` yang sebelumnya dipatok keras di `_popularColumns * 8` baris, menjadi dinamis dengan mempertimbangkan jumlah `PopularGames.Count` yang sedang tampil (mengambil nilai tertinggi). Sehingga, data yang sudah ditarik via "Tampilkan Lebih Banyak" tidak akan dibabat/dipotong saat layar di-*resize*, melainkan hanya akan disusun ulang bentuk gridnya (disesuaikan ke kelipatan kolom baru).
- Build: `Build succeeded`, `0 Error(s)`, `3 Warning(s)` WMC1506.
- Next: Menjaga stabilitas sesi pengguna, pastikan navigasi maju-mundur dan perubahan resolusi tidak lagi menghapus memori *scroll* daftar *games*.

### 2026-05-21 (Home Popular Anti-Glitch: Fractional Resize & Transition Lock)

- Fokus: Menghilangkan efek *glitch*, *stuttering* (patah-patah), dan animasi lompat-lompat (*flying cards*) yang terasa sangat mengganggu ketika pengguna menyeret layar (*drag resize*). Tujuannya memberikan *feel* aplikasi premium yang *fluid* (mulus).
- Perubahan:
  - `HomePage.xaml.cs`: Mengubah kalkulasi `slotWidth` dari `Math.Floor` menjadi pembagian fraksional `(usableWidth / columns) - 0.2`. Pengurangan sangat kecil (`0.2px`) tetap mencegah kartu tumpah ke baris bawah akibat pembulatan *DPI scaling*, tetapi pembagian fraksional membuat ukuran kartu melebar dan mengecil secara sangat mulus mengikuti setiap piksel pergeseran mouse (menghilangkan efek patah-patah / *staircase* yang sebelumnya tertahan per integer pixel).
  - `HomePage.xaml`: Mematikan animasi `AddDeleteThemeTransition` dan `RepositionThemeTransition` pada `GridView.ItemContainerTransitions` (hanya menyisakan `EntranceThemeTransition` untuk pembukaan awal). Ini mencegah efek "kartu berhamburan/beterbangan" ke seluruh arah ketika *breakpoint* kolom berubah dari 5 ke 6 (di mana penambahan item baru untuk fitur *auto-fill* terjadi).
- Build: `Build succeeded`, `0 Error(s)`, `3 Warning(s)` WMC1506.
- Next: Validasi pengalaman pengguna secara *real-time* dengan melakukan *drag-resize* pelan dan cepat untuk merasakan perbedaan kelancaran transisi ukurannya.

### 2026-05-21 (Home Popular Scale-Up: Fullscreen 6-Col, Windowed 5-Col)

- Fokus: Merespons keluhan pengguna mengenai card yang terlalu kecil (dan label PREMIUM/DENUVO bertumpuk tidak rapi) saat memaksakan 7 kolom di layar *fullscreen* dan 6 kolom di *windowed*.
- Perubahan:
  - `HomePage.xaml.cs`: Menurunkan target jumlah kolom menjadi maksimal 6 untuk *fullscreen* (lebar sisa >= 1380) dan 5 untuk *windowed* (lebar sisa >= 1080).
  - `HomePage.xaml.cs`: Memperbesar `minCardWidth` (dari 170 ke 200) dan `maxCardWidth` (dari 250 ke 320) agar *grid* memberikan ruang nafas yang lega untuk teks judul dan rentetan label status.
  - `HomeViewModel.cs`: Mengubah target awal _popularColumns dari 6 menjadi 5 untuk efisiensi *fetch* perdana di ukuran standar layar aplikasi.
- Build: `Build succeeded`, `0 Error(s)`, `3 Warning(s)` WMC1506.
- Next: Validasi *runtime* visual untuk mengecek kenyamanan ukuran card baru di layar pengguna.

### 2026-05-21 (Hotfix: Layout Rounding Math.Floor Wrap Fix & e.NewSize.Width)

- Fokus: Memperbaiki masalah grid yang memutus kolom lebih awal (misal target 7 kolom tapi secara visual menjadi 6 baris) yang membuat kalkulasi auto-fill terlihat berantakan/terbalik.
- Perubahan:
  - `HomePage.xaml.cs`: Menambahkan `Math.Floor` pada `slotWidth` (`usableWidth / columns`) sehingga kalkulasi lebar item pasti selalu muat di dalam kontainer, mencegah item terakhir tumpah ke baris baru akibat pembulatan *DPI scaling* WinUI.
  - `HomePage.xaml.cs`: Mengganti patokan dari `PopularGamesHeaderGrid.ActualWidth` (yang salah menempel di Section 1) menjadi parameter `newWidth` (mengambil langsung dari event `SizeChanged` via `e.NewSize.Width`) untuk ukuran yang akurat saat *resize*.
- Build: `Build succeeded`, `0 Error(s)`, `3 Warning(s)` WMC1506.
- Next: Validasi runtime untuk memastikan 6 kolom (windowed) dan 7 kolom (fullscreen) sudah tersusun rapi serta auto-fill baris terakhir terisi normal.

### 2026-05-21 (Hotfix: Fullscreen 7 Column Detection + Autofill Always-On)

- Fokus: Memperbaiki regresi kritis yang membuat fullscreen justru turun jadi 5 kolom dan autofill baris bawah tidak konsisten.
- Perubahan:
  - `HomePage.xaml`:
    - header popular diberi `x:Name="PopularGamesHeaderGrid"` sebagai width anchor yang stabil.
    - `PopularGamesGrid` dipaksa `HorizontalAlignment="Stretch"` untuk menghindari pengukuran lebar berbasis konten.
  - `HomePage.xaml.cs`:
    - perhitungan kolom sekarang memakai lebar `PopularGamesHeaderGrid.ActualWidth` (fallback ke `PopularGamesGrid.ActualWidth` bila perlu), bukan lebar yang bisa bias saat layout transisi.
    - breakpoint kolom diubah agar 7 kolom lebih mudah tercapai pada fullscreen umum:
      - `>=1320 => 7`
      - `>=1120 => 6`
      - `>=1080 => 5`
      - `>=860 => 4`
  - `HomeViewModel.cs`:
    - `EnsurePopularFilledRowsAsync()` kini selalu dieksekusi setelah update layout (tidak lagi dibatalkan hanya karena kolom sama), sehingga baris bawah tetap ditop-up ke kelipatan kolom aktif.
- Build: `dotnet build NexaPlay.slnx -c Debug -p:Platform=x64` sukses, `0 Error(s)`, `0 Warning(s)` (OutDir `build/debug-home-hotfix-7col`).
- Next: Validasi runtime langsung pada fullscreen user. Jika masih mentok 6/5 karena DPI ekstrem, lanjut tuning final threshold berbasis telemetry `ActualWidth` dari mesin user.

### 2026-05-21 (Home Popular Fullscreen 7-Column Tuning + Anti-Glitch Incremental Update)

- Fokus: Mengatasi kasus fullscreen yang masih mentok 6 kolom, sekaligus meredam glitch visual saat responsif resize.
- Perubahan:
  - `HomePage.xaml.cs`:
    - ukuran card diperkecil lagi agar 7 kolom realistis di fullscreen lintas user:
      - `minCardWidth: 185 -> 170`
      - `maxCardWidth: 265 -> 250`
    - strategi hitung kolom diganti ke breakpoint adaptif (berbasis `usableWidth`) agar lebih deterministik:
      - `>=1540 => 7 kolom`
      - `>=1300 => 6 kolom`
      - `>=1080 => 5 kolom`
      - `>=860 => 4 kolom`
    - slot width tetap dihitung responsif dan dikunci ke rentang aman agar proporsi card stabil.
  - `HomeViewModel.cs`:
    - `UpdatePopularLayoutAsync` tidak lagi me-reset `PopularGames` dengan object baru setiap perubahan kolom.
    - update sekarang incremental (add/remove di tail) untuk mengurangi flicker/glitch "refresh ketara".
    - auto-fill baris bawah (`EnsurePopularFilledRowsAsync`) tetap dipertahankan agar baris tidak menyisakan item tunggal.
- Build: `dotnet build NexaPlay.slnx -c Debug -p:Platform=x64` sukses, `0 Error(s)`, `0 Warning(s)` (OutDir `build/debug-home-7col-smooth`).
- Next: Validasi langsung di fullscreen apakah sudah konsisten 7 kolom; jika masih ada device yang mentok 6, turunkan lagi `minCardWidth` tipis (contoh `166`) tanpa mengorbankan keterbacaan label.

### 2026-05-21 (Home Popular 6-Column Default, 7-Column Fullscreen, Auto Fill Bottom Row)

- Fokus: Memastikan mode default menampilkan 6 card per baris, mode fullscreen bisa 7 card per baris, serta mencegah baris paling bawah tersisa bolong (misalnya cuma 1 card).
- Perubahan:
  - `HomePage.xaml.cs`:
    - engine layout dirombak agar sizing card responsif dengan target kolom lebih padat:
      - rentang card width diturunkan (`min 185`, `max 265`) sehingga fullscreen dapat mencapai 7 kolom.
      - hitungan kolom tetap otomatis (`ItemsWrapGrid`) tapi kini lebih pas untuk target 6/7.
    - event resize dibuat debounce (`160ms`) dan sinkron ke ViewModel hanya saat jumlah kolom benar-benar berubah, agar transisi fullscreen/windowed tidak terasa "refresh kasar".
  - `HomeViewModel.cs`:
    - baseline `_popularColumns` diubah dari `5` ke `6` (default app window -> 6 card per baris).
    - `UpdatePopularLayoutAsync(columns)` tetap menjaga target initial `8` baris, sekaligus menyesuaikan jumlah item saat kolom berubah.
    - ditambahkan `EnsurePopularFilledRowsAsync()` untuk auto top-up item sampai `PopularGames.Count` kelipatan kolom aktif; ini mengurangi kondisi baris bawah bolong.
    - `LoadMorePopularGamesAsync()` kini memanggil auto fill row setelah batch tambahan masuk.
  - `HomePage.xaml`:
    - padding outer grid popular dikembalikan ke `0` untuk menghindari offset ganda dan menjaga alignment area kanan-kiri terhadap header section yang sama.
- Build: `dotnet build NexaPlay.slnx -c Debug -p:Platform=x64` sukses, `0 Error(s)`, `0 Warning(s)` (OutDir `build/debug-home-6-7-fill`).
- Next: Validasi runtime 2 mode (default + fullscreen) untuk memastikan benar dapat 6/7 kolom konsisten dan baris bawah terisi penuh; jika masih ada mismatch tepi kanan, lanjut fine tune outer gutter berbasis angka tetap dari container header.

### 2026-05-21 (Home Popular Outer Gutter Lock + Stronger Hover Lighting)

- Fokus: Mengunci alignment kanan-kiri card populer agar stabil antar mode window/fullscreen, sekaligus memperjelas efek hover putih agar card terpilih lebih terlihat premium.
- Perubahan:
  - `HomePage.xaml`:
    - `PopularGamesGrid` diberi `Padding="12,0,12,0"` untuk outer gutter tetap (kiri/kanan simetris terhadap area konten).
  - `HomePage.xaml.cs`:
    - kalkulasi layout menyesuaikan padding tersebut dengan `outerPadding=24` (total kiri+kanan) sehingga pembagian kolom tetap akurat dan tepi kanan card lebih konsisten.
  - `App.xaml` (`PopularGameCardStyle`):
    - hover border diperjelas: `BorderThickness 1.5 -> 2.2`, `BorderBrush #70FFFFFF -> #CCFFFFFF`.
    - opasitas hover dinaikkan ke penuh (`1.0`) agar state card aktif lebih jelas saat pointer over.
- Build: `dotnet build NexaPlay.slnx -c Debug -p:Platform=x64` sukses, `0 Error(s)`, `0 Warning(s)` (OutDir `build/debug-home-gutter-hover`).
- Next: Validasi visual di fullscreen/windowed untuk memastikan edge kanan card dan jarak header sudah lock; setelah confirmed, lanjut batch source cover API/fallback Home.

### 2026-05-21 (Home Popular Gap Sync Fullscreen vs Windowed)

- Fokus: Menyamakan jarak kiri/kanan/bawah Popular card agar tidak terlihat kepotong saat pindah fullscreen/non-fullscreen, dan memastikan batas kanan card sejajar area kanan header ("Jelajahi Games").
- Perubahan:
  - `HomePage.xaml`:
    - `GridViewItem Margin` diseragamkan ke `8` di semua sisi (gap horizontal = gap vertikal).
    - hapus hardcoded `Height="330"` pada container card populer; card kini mengikuti tinggi slot layout yang dihitung runtime.
  - `HomePage.xaml.cs`:
    - rumus layout diubah ke model **slot** (bukan card langsung): `ItemWidth/ItemHeight` menghitung ukuran slot termasuk margin item.
    - `ItemHeight` kini menghitung `cardHeight + gap`, sehingga jarak bawah tidak lagi "kepotong" saat resize mode.
    - `ItemWidth` dibagi presisi dari `usableWidth / columns` agar sisi kanan grid terisi rapi dan tidak menyisakan ruang berlebih yang terlihat janggal.
- Build: `dotnet build NexaPlay.slnx -c Debug -p:Platform=x64` sukses, `0 Error(s)`, `0 Warning(s)` (OutDir `build/debug-home-gap-sync`).
- Next: Validasi visual runtime pada dua mode (windowed + fullscreen) untuk cek alignment kanan card terhadap header kanan; setelah ini layout dianggap lock dan lanjut batch source cover API.

### 2026-05-21 (Home Popular Final Layout Pass: Symmetric Gap + No Resize Rebind + Refined Hover)

- Fokus: Menangani keluhan layout yang belum premium: gap bawah terlihat kepotong, ruang kanan berlebih, hover card terasa "kotak", dan refresh terlalu kentara saat pindah fullscreen/non-fullscreen.
- Perubahan:
  - `HomePage.xaml`:
    - spacing item diatur ulang (`GridViewItem Margin="8,8,8,10"`) agar vertical gap bawah lebih terbaca dan tidak terlihat menyatu.
    - edge padding grid dikembalikan ke `Padding="0"` supaya kalkulasi lebar kolom benar-benar simetris kiri-kanan.
    - `HoverTitleLayer` diberi margin/padding bawah yang lebih aman agar transisi judul tidak tampak terpotong.
  - `HomePage.xaml.cs`:
    - resize tidak lagi memicu rebind jumlah item (`UpdatePopularLayoutAsync`) sehingga efek refresh saat resize/fullscreen jauh lebih halus.
    - kalkulasi item width diubah ke perhitungan presisi (tanpa `Floor`) + gap 16 agar sisa ruang kanan tidak menumpuk.
    - `ItemHeight` tetap rasio 2:3 dan dihitung presisi.
  - `App.xaml` (`PopularGameCardStyle`):
    - hover scale diturunkan drastis (`1.03` -> `1.008`) supaya card tidak terlihat berubah jadi kotak saat membesar.
    - pressed scale juga diperingan (`0.98` -> `0.994`) agar interaksi lebih elegan.
    - timing transisi diperhalus agar motion terasa lebih premium, bukan "loncat".
- Build: `dotnet build NexaPlay.slnx -c Debug -p:Platform=x64` sukses, `0 Error(s)`, `0 Warning(s)` (OutDir `build/debug-home-layout-fix2`).
- Next: Uji visual runtime di 3 mode (normal, maximized, fullscreen). Jika grid sudah stabil, lanjut batch berikutnya khusus source cover API (`library_capsule_2x -> header -> NO CONTENT`) tanpa ubah layout lagi.

### 2026-05-21 (Home Popular Spacing + Anti Flicker Resize + Hover Title Reveal)

- Fokus: Mengurangi card yang terlalu rapat, menghilangkan kedipan saat resize responsif, dan mengubah judul jadi muncul hanya saat hover dengan bayangan gradient dari bawah.
- Perubahan:
  - `HomePage.xaml`:
    - tambah jarak antar-card via `GridViewItem Margin="6"` dan edge padding grid `Padding="8,0,8,0"` agar layout tidak nempel.
    - judul card default disembunyikan; diganti layer `HoverTitleLayer` (opacity 0 + offset Y) yang muncul saat hover.
    - layer judul punya gradient tipis hitam dari bawah ke atas untuk memperjelas teks tanpa menutupi cover.
  - `HomePage.xaml.cs`:
    - resize handler Popular dibuat debounce (`130ms`) agar saat window di-drag tidak terus-terusan rebind jumlah item (sumber blink/kedipan).
    - `UpdatePopularLayoutAsync` sekarang dipanggil hanya saat jumlah kolom benar-benar berubah.
    - tambah animasi hover title (fade + slide-up) via pointer enter/exit pada card.
    - kalkulasi layout disesuaikan dengan `outerPadding` + `gap` agar ruang kanan-kiri lebih seimbang.
- Build: `dotnet build NexaPlay.slnx -c Debug -p:Platform=x64` sukses, `0 Error(s)`, `0 Warning(s)` (OutDir `build/debug-home-spacing`).
- Next: Validasi runtime feel hover dan responsif pada beberapa ukuran window; jika sudah pas, lanjut batch berikutnya ke validasi source cover API (`library_capsule_2x -> header -> fallback text`).

### 2026-05-21 (Home Popular Responsive Columns + 8 Full Rows Baseline)

- Fokus: Merapikan jarak card Home Popular, membuat kolom benar-benar responsif, dan memastikan baseline load tampil penuh 8 baris (bukan baris bawah jomplang).
- Perubahan:
  - `HomePage.xaml`:
    - Popular list tetap virtualized (`GridView + ItemsWrapGrid`) dan sekarang dipasang hook `SizeChanged` untuk layout responsif.
  - `HomePage.xaml.cs`:
    - tambah kalkulasi kolom otomatis berdasarkan lebar aktual area list,
    - `ItemWidth/ItemHeight` disetel dinamis agar isi ruang tetap pas dan rasio card dipertahankan portrait 2:3,
    - kolom hasil hitung dikirim ke ViewModel agar jumlah data yang ditampilkan sinkron.
  - `HomeViewModel.cs`:
    - page size awal sekarang mengikuti `jumlahKolom * 8` (default first open menargetkan 8 baris penuh),
    - load-more mengikuti kelipatan kolom agar tambahan batch tidak merusak ritme grid,
    - cache loaded popular dipertahankan agar re-layout tidak mem-fetch ulang dari nol.
- Build: `Build succeeded` via `OutDir=Debug-preview`, `0 Error(s)`, warning non-blocking `WMC1506` tetap ada.
- Next: Verifikasi runtime pada berbagai ukuran window untuk memastikan baris bawah tidak jomplang dan jarak antar card sudah nyaman.

### 2026-05-21 (Home Popular Layout Stabilization + Lightweight Startup)

- Fokus: Menstabilkan layout Home Popular agar tidak jadi kotak, jumlah card per baris konsisten, dan loading awal lebih ringan supaya Home tidak "nget".
- Perubahan:
  - `App.xaml`:
    - `PopularGameCardStyle` tidak lagi hardcode tinggi `160`; tinggi/lebar kini mengikuti ukuran item dari halaman pemakai style.
  - `HomePage.xaml`:
    - Popular list dipindah dari `ItemsRepeater` ke `GridView + ItemsWrapGrid` (virtualized),
    - ukuran item dikunci `220x330` (rasio 2:3),
    - `MaximumRowsOrColumns=5` untuk tata letak horizontal yang rapi dan konsisten.
  - `HomeViewModel.cs`:
    - page size popular diubah ke `15` (pas 5x3 per batch) agar awal render lebih ringan,
    - fetch metadata page dilakukan paralel terbatas (`concurrency=6`) supaya tidak serial lambat,
    - jika batch awal tidak punya cover (`library_capsule_2x/header`) sama sekali, trigger refresh metadata di background lalu refresh list populer tanpa blocking startup.
  - `GameEntry.cs`:
    - `HeaderImageUrl` tidak lagi menebak URL default CDN saat kosong, agar validasi source field Home lebih jujur terhadap data asli cache/API.
- Build: `Build succeeded` via `OutDir=Debug-preview`, `0 Error(s)`, warning non-blocking `WMC1506` tetap ada.
- Next: Validasi runtime apakah Home sudah menampilkan layout portrait 5 kolom stabil + cover berasal dari field metadata yang benar; jika masih kosong, cek isi cache source runtime untuk appid sample.

### 2026-05-21 (Home Popular - Fix Source Field library_capsule_2x + Ratio Lock 2:3)

- Fokus: Menjawab mismatch visual card Home (terlihat kotak) dan memastikan source cover benar-benar mengambil `library_capsule_2x` dari struktur metadata nested.
- Perubahan:
  - `MetadataService.cs`:
    - parser asset diubah agar bisa baca URL dari format nested:
      - `assets.library_capsule_2x[0].url`
      - `assets.header[0].url`
    - fallback tetap dipertahankan, tetapi prioritas sekarang tepat untuk Home card: `library_capsule_2x` lalu `header`.
  - `HomePage.xaml`:
    - `UniformGridLayout.ItemsStretch` diubah ke `None` agar item tidak dipaksa melebar.
    - `Button` card popular dikunci `Width=220` sehingga proporsi card `220x330` stabil (2:3), tidak berubah jadi kotak di layar lebar.
- Build: Compile tervalidasi lewat `OutDir=Debug-preview` -> `Build succeeded`, `0 Error(s)`. Build normal sempat gagal copy karena `NexaPlay.exe` sedang berjalan (file lock process aktif).
- Next: Verifikasi runtime apakah semua card sudah menampilkan framing portrait sesuai `library_capsule_2x`; jika ada game yang tetap bukan portrait, cek data source per appid di cache lokal.

### 2026-05-21 (Home UI Ratio Tune - library_capsule_2x 600x900)

- Fokus: Menyamakan proporsi visual card `Popular Games` dengan aset `library_capsule_2x` (portrait 2:3) agar framing, hover, dan badge placement lebih pas.
- Perubahan:
  - `HomePage.xaml`:
    - `UniformGridLayout.MinItemWidth` dinaikkan ke `220`.
    - Tinggi card popular disetel ke `330` (rasio 2:3 terhadap lebar target 220).
    - Margin badge kiri/kanan atas dirapikan (`10`) agar tidak mepet saat hover scale.
    - Judul bawah dirapikan (`FontSize=15`, `SemiBold`, margin bawah lebih aman) agar tetap terbaca tanpa bertabrakan dengan edge card.
- Build: `Build succeeded`, `0 Error(s)`, `3 Warning(s)` (`WMC1506` non-blocking pada binding OneWay).
- Next: Validasi runtime pada beberapa game portrait untuk cek crop/komposisi, lalu final micro-tuning tinggi (±10 px) jika diperlukan.

### 2026-05-21 (Home UI Popular Card Portrait + Cover Fallback)

- Fokus: Menyesuaikan layout card `Popular Games` di Home menjadi portrait dan menyamakan prioritas source cover image.
- Perubahan:
  - `HomePage.xaml`:
    - card Popular diubah dari landscape pendek (`Height=160`) ke portrait (`Height=300`),
    - grid layout dirapikan untuk pola multi-kolom portrait (`MinItemWidth=210`, `MaximumRowsOrColumns=6`),
    - source image card diganti ke `PopularCoverImageUrl`,
    - jika cover kosong tampil fallback teks `NO CONTENT`.
  - `GameEntry.cs`:
    - tambah `LibraryCapsule2xUrl`,
    - tambah `RawHeaderImageUrl`,
    - tambah `PopularCoverImageUrl` dengan prioritas: `library_capsule_2x` lalu `header`; jika keduanya kosong -> `null`.
  - `MetadataService.cs`:
    - parser runtime catalog sekarang membaca field `library_capsule_2x`,
    - mapping ke `GameEntry.LibraryCapsule2xUrl`.
  - `HomePage.xaml.cs`:
    - tambah helper `IsNullOrWhiteSpace(...)` untuk binding fallback visibility.
- Build: `Build succeeded`, `0 Error(s)`, `3 Warning(s)` (`WMC1506` non-blocking pada binding OneWay yang sudah ada).
- Next: Validasi runtime visual Home (kepadatan card portrait + fallback teks), lalu cek apakah perlu tuning kecil tinggi card untuk rasio akhir yang paling mirip referensi.

### 2026-05-21 (Warning Audit Batch 3 - NU1902 SharpCompress)

- Fokus: Menutup advisory `NU1902` tanpa menurunkan parity fitur apply/extract OnlineFix.
- Perubahan:
  - `NexaPlay.csproj`:
    - upgrade `SharpCompress` dari `0.38.0` ke `0.48.1` (latest stable saat audit).
  - `OnlineFixService.cs`:
    - migrasi fallback ekstraksi SharpCompress ke API baru:
      - `ArchiveFactory.OpenArchive(stream, new ReaderOptions())`
      - ekstraksi entry via `entry.WriteToFile(..., new ExtractionOptions { Overwrite = true })`
    - flow log dan daftar file extracted tetap dipertahankan.
- Build: `Build succeeded`, `0 Warning(s)`, `0 Error(s)`.
- Next: Lanjut smoke test manual fitur Apply Online Fix untuk validasi runtime end-to-end pasca-upgrade paket.

### 2026-05-21 (Warning Audit Batch 2 - BypassGames CS1522)

- Fokus: Menutup warning `CS1522` (empty switch block) tanpa mengubah behavior flow bypass.
- Perubahan:
  - `BypassGamesPage.xaml.cs`:
    - hapus blok `switch (state.Phase)` yang kosong (hanya komentar) di progress callback.
    - progress callback tetap no-op sesuai kondisi UI helper yang masih dikomentari.
- Build: `Build succeeded`, `0 Error(s)`. Warning `CS1522` hilang.
- Next: Lanjut audit warning tersisa `NU1902` (`SharpCompress` vulnerability advisory) dan tentukan patch versi paket yang aman untuk parity.

### 2026-05-21 (Warning Audit Batch 1 - HomePage Nullability)

- Fokus: Menurunkan warning C# paling aman tanpa mengubah behavior UI.
- Perubahan:
  - `HomePage.xaml.cs`:
    - `_carouselTimer` diubah menjadi nullable (`DispatcherTimer?`) untuk menutup `CS8618`.
    - signature `CarouselTimer_Tick` diubah ke `object? sender` untuk menutup `CS8622`.
- Build: `Build succeeded`, `0 Error(s)`. Warning `CS8618` + `CS8622` hilang. Tersisa `CS1522` (BypassGamesPage) dan `NU1902` (SharpCompress advisory).
- Next: Audit warning `CS1522` di `BypassGamesPage.xaml.cs` lalu lanjut evaluasi paket `SharpCompress`.

### 2026-05-21 (Game Detail Denuvo Badge Blink Visibility Fix)

- Fokus: Membuat efek kedap-kedip label Denuvo di Game Detail benar-benar terlihat jelas tanpa hover.
- Perubahan:
  - `GameDetailPage.xaml.cs`:
    - samakan karakter pulse dengan pola di Home (opacity `1.0 -> 0.3`, `700ms`, auto-reverse, repeat forever),
    - tambah pulse pada `DenuvoBadgeGlowOverlay` (`0.0 -> 0.42`) agar efek kedip lebih tegas secara visual,
    - reset state animasi saat stop (`DenuvoBadge.Opacity = 1`, `DenuvoBadgeGlowOverlay.Opacity = 0`).
- Build: `dotnet build NexaPlay.csproj -c Debug -r win-x64 /p:OutDir=...Debug-preview` sukses, `0 Error(s)`, warning lama tetap.
- Next: Validasi visual runtime pada game yang `HasDenuvo=true`; jika sudah sesuai lanjut audit warning satu per satu.

### 2026-05-21 (Background HEAD Sync + Silent Hot-Reload Metadata)

- Fokus: Sinkronisasi metadata latar belakang berbasis HTTP HEAD agar startup tetap instan dari cache lokal.
- Perubahan:
  - `MetadataService.EnsureIndexedAsync` diubah ke strategi cache-first:
    - jika cache lokal essential ada, index langsung dibangun dari disk (tanpa blocking jaringan),
    - lalu background update dijalankan fire-and-forget.
  - Tambah `PerformBackgroundUpdateAsync()`:
    - jalankan `SyncSourcesCoreAsync(... useHeadCheck: true)` di belakang layar,
    - jika ada file berubah, rebuild index RAM (hot-reload) secara senyap.
  - `DownloadIfNeededAsync(...)` ditambah mode `useHeadCheck`:
    - kirim `HttpMethod.Head`,
    - bandingkan `Last-Modified` vs `File.GetLastWriteTimeUtc`,
    - skip download jika belum berubah,
    - saat HEAD gagal dan cache masih < `SafetyNetTtl`, skip download,
    - saat cache sudah tua, fallback GET.
  - TTL metadata dikonsolidasikan ke `SafetyNetTtl` (24 jam) untuk flow metadata.
  - `steam_games.json` tetap hanya untuk deteksi proteksi saat build index (bukan override field katalog).
- Build: `Build succeeded`, `0 Error(s)`, warning non-blocking tetap ada.
- Next: Jalankan verifikasi runtime skenario 1st launch vs relaunch + cek log "not modified on GitHub" dan "hot-reload index".

### 2026-05-21 (Baseline Recovery MetadataService Contract + Cache TTL)

- Fokus: Menstabilkan baseline build sebelum batch fitur, sesuai startup task handoff.
- Perubahan:
  - `MetadataService` sekarang mengimplementasikan kontrak `IMetadataService.IsCacheAvailable`.
  - Tambah helper aman `IsFileUsable(...)` untuk validasi cache file metadata (exists + size > 0).
  - `AppConstants` ditambahkan TTL yang direferensikan service metadata:
    - `SteamDataCacheTtl = 24 jam`
    - `BypassGamesCacheTtl = 24 jam`
- Build: `Build succeeded`, `0 Error(s)`, warning non-blocking tersisa (`NU1902 SharpCompress`, `CS8618`, `CS8622`, `CS1522`, `WMC1506`).
- Next: Lanjut batch kecil berikutnya pada warning cleanup aman (mulai dari `HomePage` nullability timer) tanpa mengubah behavior parity GameHub.

### 2026-05-22 (4)

- Fokus: UI shell cleanup � hapus top bar judul page (Home/Games/Library/Bypass/Settings) agar konten menyatu langsung ke atas.
- Perubahan:
  1. `MainWindow.xaml`: `ContentTopBarRow` di-set `Height=0`.
  2. `MainWindow.xaml`: `PageTopBar` di-set `Visibility=Collapsed`.
  3. `MainWindow.xaml`: `ContentFrame` dipindah ke `Grid.Row=0` agar menempati area penuh konten.
  4. `MainWindow.xaml`: `StartupOverlay` `Grid.RowSpan` disesuaikan ke `1`.
  5. `MainWindow.xaml.cs`: `SetShellDetailMode` diselaraskan agar top bar tetap hidden di mode normal.
  6. `MainWindow.xaml.cs`: `NavigateTo` tidak lagi bergantung pada `PageTitleText`.
- Build: `Build succeeded`, `0 Error(s)`.
- Next: Validasi runtime visual spacing atas di semua page dan detail page transition.
### 2026-05-22 (3)

- Fokus: Fix crash `0xC000027B` + perbaiki `run_nexaplay.bat`.
- Root cause: Di `GameDetailViewModel` constructor, parameter `nexaPlayOverride` diterima tapi **tidak di-assign** ke field `_nexaPlayOverride`. Akibatnya `_nexaPlayOverride` selalu `null`, dan saat `LoadAsync` memanggil `_nexaPlayOverride.GetCatalogOverrideAsync()`, terjadi `NullReferenceException` yang menjadi crash `0xC000027B` di WinUI XAML runtime.
- Perubahan:
  1. `GameDetailViewModel.cs`: Tambah `_nexaPlayOverride = nexaPlayOverride;` di constructor.
  2. `run_nexaplay.bat`: Rewrite total � sekarang pakai MSBuild build + `start /wait` exe. Setelah app exit, otomatis cek exit code. Kalau crash: dump `crash.txt` (last 60 lines) + Event Viewer events ke `nexaplay_crash_context.log`. File crash context selalu terbaru setiap ada crash.
- Build: `Build succeeded`, `0 Error(s)`, `0 Warning(s)`.
- Next: Test runtime � verifikasi crash sudah hilang, override hero muncul, dan crash log auto-generated saat ada crash.
### 2026-05-22 (2)

- Fokus: Fix bug library_hero_2x override tidak terapply + ETag caching.
- Perubahan:
  1. **Bug fix root cause**: `library_hero_2x` dari NexaPlay override sudah berhasil di-set di `GameEntry.LibraryHero2xUrl`, tapi enrichment di `HomeViewModel.EnrichRecentFixesHeroCoverAsync` dan fallback di `GameDetailViewModel.LoadAsync` selalu mendahulukan data Steam API � sehingga override selalu tertimpa.
  2. `HomeViewModel.cs`: Inject `INexaPlayOverrideService`. Di `EnrichRecentFixesHeroCoverAsync`, cek override hero dulu � kalau ada, skip API enrichment untuk hero. Capsule tetap bisa di-enrich dari API kalau tidak di-override.
  3. `GameDetailViewModel.cs`: Inject `INexaPlayOverrideService`. `HeroBackgroundUrl` dan `GameIconUrl` sekarang cek override catalog dulu sebelum fallback ke API/catalog.
  4. `NexaPlayOverrideService.cs`: Full rewrite dengan ETag caching. Alur: load disk cache instant ? fetch GitHub dengan `If-None-Match` ETag saat startup pertama ? kalau `304 Not Modified` skip download. ETag disimpan di `nexaplay_override.etag`. Cek hanya saat app pertama buka, tidak background polling.
- Build: `Build succeeded`, `0 Error(s)`, `0 Warning(s)`.
- Next: Test runtime � verifikasi hero override muncul di Home New Bypass dan Game Detail. Verifikasi ETag caching (cek log `NexaPlayOverride`).
### 2026-05-22

- Fokus: Implementasi NexaPlay Override Service � sparse override per-appid dari repo GitHub terpisah.
- Perubahan:
  1. `AppConstants.cs`: Tambah `NexaPlayOverrideUrl` (raw GitHub `adii83/Nexaplay-Metadata-Override`) dan `NexaPlayOverrideCacheFileName`.
  2. `Core/Models/NexaPlayOverrideModels.cs`: Model `NexaPlayCatalogOverride`, `NexaPlayDetailOverride`, `NexaPlayScreenshotOverride`, `NexaPlayMovieOverride`.
  3. `Contracts/Services/INexaPlayOverrideService.cs`: Interface dengan `GetCatalogOverrideAsync`, `GetDetailOverrideAsync`, `GetOverriddenAppIdsAsync`, `RefreshAsync`.
  4. `Infrastructure/Services/NexaPlayOverrideService.cs`: Implementation � download + disk cache (24h TTL) + JSON parser untuk format sparse.
  5. `Infrastructure/Services/MetadataService.cs`: Inject `INexaPlayOverrideService`, apply catalog overrides setelah `BuildIndexAsync` (override semua field GameEntry termasuk cover, hero, icon, price, protection, dll).
  6. `Infrastructure/Services/SteamStoreService.cs`: Inject `INexaPlayOverrideService`, apply detail overrides setelah `ParseMergedDetail` (override screenshots, movies, about, sysreq, dll).
  7. `App.xaml.cs`: Register `INexaPlayOverrideService` sebagai singleton DI.
- Build: `Build succeeded`, `0 Error(s)`, `0 Warning(s)`.
- Next: Test runtime � tambah entry di `nexaplay_override.json` repo GitHub, buka app, verifikasi override catalog dan detail terapply. Lalu lanjut polish area lain.
### 2026-05-20 (Hardening MVVMTK0045 — Partial Property Migration)

- Fokus: Eliminasi seluruh 63 warning `MVVMTK0045` agar semua `[ObservableProperty]` AOT-safe untuk WinUI 3 / WinRT marshalling.
- Perubahan:
  - `NexaPlay.csproj`: tambah `<LangVersion>preview</LangVersion>` agar compiler mendukung sintaks partial property C# 13+.
  - 6 ViewModel dimigrasi dari pola field lama (`private bool _isLoading`) ke partial property (`public partial bool IsLoading { get; set; }`):
    - `GameDetailViewModel.cs` (18 properti)
    - `HomeViewModel.cs` (6 properti)
    - `BypassGamesViewModel.cs` (12 properti)
    - `GamesViewModel.cs` (4 properti)
    - `SettingsViewModel.cs` (12 properti)
    - `LibraryViewModel.cs` (8 properti)
  - Default value yang sebelumnya inline (`= string.Empty`, `= Array.Empty<>()`) dipindah ke constructor masing-masing ViewModel karena partial property C# 13 tidak boleh punya initializer inline.
  - `MainViewModel.cs` tidak menggunakan `[ObservableProperty]` sehingga tidak perlu diubah.
  - Semua `partial void On...Changed` callback tetap berfungsi tanpa perubahan.
- Build: `Build succeeded`, `0 Error(s)`, `5 Warning(s)` (hanya NU1902 SharpCompress). Warning MVVMTK0045: **0** (sebelumnya 63).
- Next: Smoke test runtime; lanjut prioritas berikutnya dari matrix parity.

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
