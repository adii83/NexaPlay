# Audit Alur Data NexaPlay

Tanggal audit: 2026-09-13
Cakupan: aplikasi WinUI aktif di dalam `NexaPlay/`; pemeriksaan sumber dan pengukuran data lokal secara baca-saja.
Batasan audit awal: pemeriksaan sumber dan pengukuran data dilakukan baca-saja. Bagian migration status di bawah mencatat implementasi aktual sesudah audit baseline.

## Migration Status - Local SQLite Catalog

Tanggal migrasi: 2026-09-14

| Area | Status | Kondisi aktual |
| --- | --- | --- |
| General Catalog | [DONE] | `data/catalog/catalog_seed.sqlite` menjadi baseline installer; runtime canonical di `%LocalAppData%\NexaPlay\Data\catalog.sqlite`. |
| Bundled Seed | [DONE] | Version 2, 163.262 row, SHA-256 `959676a96aa32d41917f95b2e1177d1eb1bf7a95410afa4a9f3559ae6de73d74`; ikut build/publish sebagai content fisik. |
| Bootstrap | [DONE] | First run offline copy ke `.bootstrap`, validasi SQLite/schema/row/version, lalu atomic move. Runtime valid tidak ditimpa; corrupt DB dipreservasi sebagai `catalog.corrupt.*.sqlite`. |
| Incremental Patch | [DONE] | R2 `catalog_manifest.json` format final; patch berurutan, size/SHA/schema/from/to validation, UPSERT/DELETE/version dalam satu transaction. |
| Remote full SQLite | [RETIRED] | Tidak ada request snapshot atau download `catalog.sqlite` dari R2. |
| Games | [DONE] | Count, paging, search, tier, protection, genre, default price floor berjalan sebagai query SQLite parameterized. |
| Games filter cache | [RETIRED] | Full RAM `GameFilterIndex` dan `games_filter_index_cache_v3.json` tidak lagi dibuat/dibaca; legacy file dibersihkan. |
| Search | [DONE] | Exact AppID dan normalized substring memakai SQLite; debounce 220 ms dan minimum 3 karakter tetap. |
| Home Popular | [DONE] | `appid_populer.json` tetap aktif; SQLite batch lookup mengembalikan card dalam urutan feed asli. |
| Home Recent | [DONE] | Ordered-distinct `new_fix_games`, maksimum 12 SQLite base rows, lalu optional `Metadata/{appid}.json` enrichment berjalan bounded di background setelah local UI tampil. |
| Library | [DONE] | `.lua` tetap sumber membership; metadata card dibaca batch dari SQLite tanpa `Metadata/{appid}.json`. |
| Game Detail | [DONE] base lookup / [UNCHANGED] full detail | Base `GameEntry` dari SQLite; full R2 `Metadata/{appid}.json`, cache 7 hari, parser, WebView2, dan detail override tetap. |
| Bypass | [DONE] listing optimization | Listing membaca file lokal + satu SQLite batch dan membuat zero full-detail request. Full metadata baru diminta oleh Bypass Detail setelah card diklik. Credentials/download/category/premium tetap authority JSON Bypass. |
| Protection / Denuvo | [DONE] | SQLite base + membership fix/new-fix/steam + final NexaPlay protection override. Self-check mencakup kasus A-G. |
| Premium | [DONE] | Stored SQLite `tier` menjadi authority; fallback `GameEntry` memakai 135.000. Boolean premium Bypass tetap independen. |
| Settings - Load Games | [DONE] | Conditional revalidation override + empat dynamic feed + catalog manifest/patch. Parser, overlay, generation, dan ViewModel hanya di-refresh jika source relevan berubah. |
| Clear Cache | [DONE] | Cover/detail cache dan transient card state dibersihkan tanpa menghapus SQLite/feed/ETag/license atau menaikkan catalog generation. Cache epoch mencegah operasi lama mempublikasikan ulang file. |
| Clear Data | [DONE] | Factory reset tetap menghapus `%LocalAppData%\NexaPlay`; restart bootstrap ulang dari seed tanpa network catalog penuh. |
| Bypass detail N+1 | [DONE] | Listing tidak memanggil `GetDetailAsync`; N+1 full metadata retired. |
| Detail/image cache growth | [DONE] | Cover hard cap 250 MB, trim target sekitar 200 MB. Detail TTL 7 hari, hard cap 100 MB, trim target sekitar 90 MB. Maintenance background/periodic. |

### Arsitektur Aktif

```text
Catalog Builder output/catalog.sqlite
        |
        | copied before release
        v
NexaPlay data/catalog/catalog_seed.sqlite
        |
        | first run atomic bootstrap
        v
%LocalAppData%\NexaPlay\Data\catalog.sqlite
        |
   +----+-------+--------+--------+
   |            |        |        |
 Home Popular  Games    Search   Library
   |                              |
   +----------- click AppID -------+
                  |
                  v
       R2 Metadata/{appid}.json

R2 catalog_manifest.json + patches/N.json
        |
        v
validated sequential SQLite transactions

appid_populer.json -> Home order
fix/new_fix/steam_games -> Bypass + protection overlay
```

### Startup Aktual

```text
license flow
-> ensure/open local SQLite
-> Home local catalog available
-> existing cover/Bypass/feed warmups
-> Home and Games local queries
-> hide startup overlay
-> background catalog manifest/patch check
-> generation + Home/Games reload only when version advances
```

Network failure pada manifest/patch bersifat non-fatal: runtime SQLite terakhir tetap dipakai.

Bagian 1-20 setelah migration status ini dipertahankan sebagai **baseline legacy sebelum migrasi**. Pernyataan berwaktu "saat ini" di baseline harus dibaca sebagai kondisi audit 2026-09-13; migration status di atas adalah authority aktual 2026-09-14.

### Source dan Network Classification

| Item | Status | Fungsi |
| --- | --- | --- |
| `catalog_seed.sqlite` | [DONE] ACTIVE | Bundled release baseline. |
| `%LocalAppData%\NexaPlay\Data\catalog.sqlite` | [DONE] ACTIVE | Runtime canonical general listing. |
| `catalog/catalog_manifest.json` | [DONE] ACTIVE | Protocol version/latest/minimum/patch list. |
| `catalog/patches/*.json` | [DONE] ACTIVE | Incremental catalog updates. |
| `steam_data.json`, `steam_data.json.gz` | [RETIRED] general listing | Tidak lagi diminta atau diparse client. |
| `override_data.json` | [RETIRED] general listing | Tidak lagi diminta atau diparse client. |
| `new_games.json`, `new_games_catalog.json` | [RETIRED] general listing | Additions datang melalui patch SQLite. |
| `games_filter_index_cache_v3.json` | [RETIRED] | Tidak lagi dibuat/dibaca. |
| `appid_populer.json` | [DONE] ACTIVE | Urutan Home Popular, ordered-distinct, downloader tunggal `DynamicFeedRefreshService`. |
| `fix_games.json`, `new_fix_games.json`, `steam_games.json` | [DONE] ACTIVE | Bypass/protection feeds; downloader tunggal, validated conditional GET, atomic publish, local parser consumers. |
| `library_capsule.json(.gz)` | [RETIRED] cleanup-only | Tidak ada downloader/parser/DI/runtime consumer. Nama file tersisa hanya untuk penghapusan artifact lama nonfatal. |
| `metadata_detail/{appid}.json` | [DONE] ACTIVE | Lazy full detail untuk General Detail, Bypass Detail, dan Home Recent max 12; atomic write, in-flight dedup, stale fallback, TTL/cap. |

Request general catalog aktif hanya `GET https://meta.nexaplaymetadata.online/catalog/catalog_manifest.json` dan patch relatif yang tercantum. Tidak ada remote full SQLite. Request `Metadata/{appid}.json` tetap hanya untuk detail/fallback existing.

## Final Data-Flow Optimization - Actual Result

Tanggal implementasi/verifikasi lokal: 2026-09-15. Bagian ini dan `Migration Status` adalah authority aktual. Bagian audit Phase 2 di bawah tetap dipertahankan sebagai baseline historis sebelum optimasi.

### Listing dan Detail

```text
Home Popular: appid_populer -> ordered distinct -> SQLite batch -> local card -> background cover byte cache
Home Recent: new_fix_games -> ordered distinct max 12 -> SQLite batch/Bypass fields -> immediate UI -> bounded detail enrichment
Games Browse: effective SQLite price/artwork filters + count -> bounded 3-page lightweight candidate block -> daily deterministic in-memory shuffle -> visible-page hydration
Games Search: whole effective SQLite catalog + explicit filters -> normal page query -> safe card placeholder when artwork is absent
Library: Steam .lua membership -> SQLite batch -> local paging/search
Bypass listing: local fix/steam JSON -> one SQLite batch for missing artwork -> cards; zero GetDetailAsync
General detail: clicked row -> SQLite base -> Metadata/{appid}.json/cache -> NexaPlay hero override -> library_hero -> library_hero_2x -> existing later fallbacks
Bypass detail: clicked row -> SQLite base -> Metadata/{appid}.json/cache -> NexaPlay hero override -> library_hero -> library_hero_2x -> existing later fallbacks + tutorial
```

`GetDetailAsync` production callers setelah source search: `GameDetailViewModel`, `BypassGameDetailViewModel`, dan `HomeViewModel` Recent bounded flow. Home Popular, Games, Library, dan Bypass listing tidak memanggil full metadata.

### Games Ordering dan Query

- Browse berarti `SearchQuery` kosong, termasuk seluruh tier/protection/genre/combined filter. Predicate SQLite wajib `price_normalized >= 100000` dan `NULLIF(TRIM(effective.library_capsule), '') IS NOT NULL`.
- `library_capsule` browse memakai nilai effective yang sama dengan card: `COALESCE(catalog_runtime_overrides.library_capsule, games.library_capsule)`. Header bukan bukti eligibility browse; override nonempty dapat membuat row eligible dan override empty/whitespace membuatnya ineligible.
- Price bands: `>=350000`, `250000-349999`, `180000-249999`, `135000-179999`, `100000-134999`. Batas kontinu berarti SQLite `price DESC` menghabiskan band secara berurutan.
- Candidate block maksimum `pageSize * 3`, hanya membawa AppID + harga. Block dipecah per band lalu diurutkan hash stabil dari local date `yyyyMMdd`, band, block index, dan AppID. Hanya page terpilih dihydrate lewat batch PK.
- Browse session menyimpan local date aktif. Navigasi/reload berikutnya mengambil tanggal baru; koleksi tidak berubah diam-diam saat tengah paging.
- Search memakai seluruh catalog effective tanpa floor Rp100.000 dan tanpa syarat artwork. Exact AppID/normalized name, explicit tier/protection/genre filters, debounce 220 ms, dan cancellation/version gate tetap berlaku. Nonnumeric 1-2 karakter mengosongkan hasil lama; exact numeric 1-2 digit tetap valid.
- Prefetch hanya satu page berikutnya; search/filter/page-size/navigation/cache invalidation membatalkan state lama.
- Count dan candidate/page memakai satu `BuildWhere()` yang sama; tidak ada post-query artwork filter, full-catalog RAM filter, `ORDER BY RANDOM()`, Metadata detail lookup, atau `library_capsule.json(.gz)` request untuk listing Games.

Benchmark bundled seed 163.262 row, warm 5-run median, limit 60:

| Scenario | Before Phase 2 median ms | Pre-artwork filter median ms | Artwork filter final median ms |
| --- | ---: | ---: | ---: |
| default page 1 | 35.073 | 16.743 | 87.645 |
| default page 2 | 31.791 | 17.546 | 83.023 |
| Premium | 198.956 | 96.871 | 81.502 |
| Standard | 178.483 | 105.722 | 74.997 |
| Denuvo | 176.750 | 89.163 | 75.842 |
| Non-Denuvo | 177.882 | 96.503 | 77.368 |
| Genre Action | 180.171 | 93.282 | 73.686 |
| Premium + Denuvo + Action | 182.087 | 88.381 | 70.659 |
| exact AppID 570 | 0.041 | 0.101 | 0.091 |
| normalized `counter strike` | 53.680 | 37.952 | 37.500 |

Artwork access membuat default browse tidak lagi covering-only. Query-shape check menunjukkan table scan sekitar 99-107 ms; browse CTE sekarang memakai existing `idx_games_price`, menurunkannya ke sekitar 83-88 ms. Filtered browse tetap sekitar 71-82 ms dan search tidak berubah material. Representative plan: price-index scan + override PK lookup + temporary B-tree untuk browse ordering; exact AppID memakai games PK; normalized substring memakai covering name index + override PK lookup. Tidak ada schema/index baru.

Snapshot effective runtime SQLite: `>=100K + capsule` 18.928; `>=100K tanpa capsule` 399; `<100K + capsule` 136.964; `<100K tanpa capsule` 6.971; Free 68.545. Sample valid untuk manual search: AppID `10` (<100K + capsule), `36130` (Free + no capsule), dan `29560` (>=100K + no capsule).

### Refresh dan Cache

- `DynamicFeedRefreshService` menjadi network+disk owner tunggal untuk Popular/NewFix/FixGames/SteamGames. Semua memakai conditional ETag saat body lokal tersedia, per-feed JSON validation, flush + atomic move, dan known-good preservation. Body lokal yang hilang memaksa body fetch tanpa validator; `304` tak terduga tanpa body diretry sekali tanpa validator. Kegagalan atau body yang masih hilang tidak mengaktifkan same-session suppression, sehingga refresh berikutnya tetap dapat mencoba lagi.
- Startup memakai local SQLite/feed lebih dulu. Manifest + dynamic feed check berjalan setelah startup overlay ditutup; hanya surface terdampak yang reload/invalidate.
- `NewFixChanged` pada background startup memanggil `HomeViewModel.ReloadRecentAsync()` ketika Home aktif. Jalur ini membaca AppID ordered-distinct, membatasi 12, melakukan batch SQLite/Bypass base, lalu hanya kandidat Recent tersebut yang boleh menjalankan enrichment `GetDetailAsync` berbatas concurrency empat.
- `Load Games` memaksa conditional revalidation, bukan unconditional payload download. No-change tidak menaikkan generation atau memaksa Home/Games rebuild.
- Home Popular menampilkan SQLite cards sebelum cover image cache download selesai. Home Recent menampilkan local rows sebelum max-12 detail enrichment selesai.
- `SteamStoreService` memakai per-AppID in-flight dedup, parse-before-publish, durable temp write + atomic move, corrupt-cache repair, valid stale fallback pada transient network failure, dan cache epoch.
- Cover/detail clear memakai detach/delete fallback dan epoch sehingga task yang mulai sebelum Clear Cache tidak dapat menulis balik. Cover cap 250 MB trim ke sekitar 200 MB; detail TTL 7 hari + cap 100 MB trim ke sekitar 90 MB.
- Hero clicked General Detail dan Bypass Detail memakai urutan identik: override katalog `LibraryHero2x` -> metadata `library_hero` -> metadata `library_hero_2x` -> SGDB hero -> catalog `LibraryHero2xUrl` -> metadata `background_raw` -> catalog background raw -> detail background -> metadata header -> catalog header. Perubahan hanya menukar dua field hero metadata; listing artwork tidak berubah.

### Verification Status

- [PASS] Debug x64 clean build: 0 error, 0 warning.
- [PASS] SelfCheck: Browse price/artwork matrix, effective override artwork parity, count/page parity, three-page uniqueness, daily rotation, tier/protection/genre/combined filters, whole-catalog Search including Free/sub-100K/no-capsule/exact AppID, patch/overlay/protection, cache safety, detail fallback, dynamic feeds, dan bundled seed count.
- [PASS] Release self-contained publish dan bundled `data/catalog/catalog_seed.sqlite` presence.
- [PASS] `git diff --check`.
- [PASS] Fixture dynamic feed terisolasi: missing+200, missing+unexpected 304 lalu 200, existing+ETag+304 tanpa rewrite, missing+network failure lalu retry non-force, dan invalid JSON menjaga known-good.
- [PASS MANUAL DEBUG] Dengan app tertutup dan `new_fix_games.json` memang hilang, startup Debug mengambil remote `200`, memvalidasi array 13 AppID, menulis file fisik 155 byte ke runtime source, menandai `recent=True`, dan Home langsung menampilkan `New Bypass Games` tanpa `Load Games`/restart kedua.
- [PASS MANUAL DETAIL] AppID `2840770` memiliki URL `library_hero` dan `library_hero_2x` berbeda. General Detail dan Bypass Detail sama-sama membuka dan merender hero normal. HEAD representatif: `library_hero.jpg` 402.175 byte; `library_hero_2x.jpg` 1.440.907 byte. Penghematan berasal dari resource gambar, bukan ukuran Metadata JSON.
- [PASS RELEASE SMOKE] Publish self-contained terbaru membuka proses responsif, melewati license flow, dan menampilkan section `New Bypass Games` dari runtime feed yang sudah dipulihkan.
- [PASS MANUAL DEBUG] Real Debug GUI melewati license flow. Games Browse page 1 menampilkan 60 card tanpa `NO CONTENT`; Next dan Prev bekerja. Premium, Standard, Denuvo, Non-Denuvo, Action, dan Premium+Denuvo+Action memuat result tanpa blank-artwork browse row. Standard+Non-Denuvo+RPG dan predicate/filter content lengkap diverifikasi lewat SQLite SelfCheck.
- [PASS MANUAL SEARCH] AppID `10`, `36130`, dan `29560` ditemukan. AppID `36130`/`29560` menampilkan placeholder `NO CONTENT`, tetap clickable, dan `36130` membuka General Detail normal. Search AppID `10` + Standard tetap ditemukan; clear Search + Standard kembali ke browse dan row sub-100K tidak eligible menurut query SQLite yang sama.
- [PASS LISTING NETWORK] `metadata_detail/36130.json` tidak ada sebelum maupun sesudah Search result dirender; detail file/network baru diizinkan setelah card diklik. Source search memastikan Games listing tidak memanggil `GetDetailAsync`, tidak membaca `library_capsule.json(.gz)`, dan hanya image cache yang boleh mengambil URL artwork valid.
- [PASS MANUAL RELEASE] Published Release membuka Home/Games, Browse memuat 60 card tanpa placeholder, Search AppID `36130` memuat satu placeholder aman, dan Search AppID `10` + Standard memuat Counter-Strike tanpa crash.
- [PASS] Windows Application log check satu jam terakhir tidak menemukan `.NET Runtime`/`Application Error`/WER entry untuk NexaPlay selama smoke.
- [SKIPPED SAFETY] Clear Data destructive factory reset tidak dijalankan; scope meminta current user state tetap dipreservasi.

## 1. Ringkasan Eksekutif

NexaPlay saat ini memiliki dua lapisan data berbeda, tetapi halaman daftar belum menggunakannya secara konsisten:

1. Lapisan katalog disusun di memori dari sumber JSON besar yang dihosting di GitHub. `MetadataService.BuildIndexAsync()` menggabungkan `steam_data.json.gz` dan `steam_data.json`, menambahkan `new_games_catalog.json`, menimpa data dengan `override_data.json`, memparse ulang tiga file bypass untuk menentukan protection, lalu menerapkan `nexaplay_override.json`. Hasilnya berupa dictionary dengan AppID sebagai key. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:695-790`, metode `BuildIndexAsync()`, dipanggil oleh `WarmupEssentialSourcesAsync()`, `EnsureLoadedAsync()`, dan jalur refresh pada baris 243-345.
2. Lapisan detail diambil secara lazy dari Cloudflare R2 sebagai `Metadata/{appid}.json`, dicache selama tujuh hari di `%LocalAppData%\NexaPlay\metadata_detail`, diparse menjadi `GameDetailEntry`, dan juga dipertahankan utuh sebagai `RawMetadataJson`. Sumber: `NexaPlay/Infrastructure/Services/SteamStoreService.cs:14-67`, metode `GetDetailAsync()` dan `BuildMergedMetadataJsonAsync()`, dipanggil oleh halaman detail dan beberapa jalur pengayaan card daftar.
3. Home dan Games terutama memakai data katalog, tetapi hero terbaru Home, card Library, dan setiap card Bypass dapat meminta JSON detail penuh hanya untuk menentukan artwork. Bypass menjadi jalur N+1 terbesar karena memulai pengayaan untuk setiap item pada kedua daftar Bypass. Sumber: `NexaPlay/Presentation/ViewModels/BypassGamesViewModel.cs:87-113` dan `184-264`, metode `EnrichCoversInBackgroundAsync()`, dipanggil oleh `LoadAsync()`.
4. Startup memproses banyak data. Setelah validasi license, startup menunggu pemanasan/pembangunan katalog, pemanasan/parse indeks cover, pemuatan Home, serta pramuat indeks filter/card Games sebelum menyembunyikan overlay startup. Sumber: `NexaPlay/MainWindow.xaml.cs:205-233`, `248-400`, metode `RunStartupLicenseFlowAsync()` dan `WarmupMetadataStartupAsync()`.
5. Search berjalan lokal, bukan melalui API. Games memuat indeks ringan lengkap kedua ke RAM dan menyimpannya sebagai `games_filter_index_cache_v3.json`; setiap query/filter memindai indeks lalu mengacak hasil melalui pengurutan `OrderBy(random.Next())`. Sumber: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:119-166`, `538-602`, metode `LoadOrBuildFilterIndexAsync()` dan `BuildFilteredAppIdsAsync()`.
6. Penyimpanan Library sudah ringan: keberadaan game ditentukan dari Steam `config\stplug-in\{appid}.lua`; tidak ada database metadata Library duplikat. Inefisiensi berada pada proses hidrasi saat baca, ketika setiap card yang terlihat meminta metadata dasar, data override, dan detail penuh. Sumber: `NexaPlay/Infrastructure/Services/AddGameService.cs:29-79`; `NexaPlay/Presentation/ViewModels/LibraryViewModel.cs:100-128`, `135-194`.
7. Tidak ada SQLite atau database aplikasi lain. State permanen menggunakan JSON, `.dat` terenkripsi, `.cache`, file log, file Steam, dan penyimpanan WebView2. Pencarian repository tidak menemukan referensi `SqliteConnection`, `SQLiteConnection`, `DbContext`, atau paket SQLite.

Ukuran lokal yang diamati pada workstation ini, bukan nilai tetap untuk semua instalasi:

| Area | Jumlah file | Byte | Perkiraan MiB |
| --- | ---: | ---: | ---: |
| `%LocalAppData%\NexaPlay\runtime_catalog_sources` | 645 | 113.082.483 | 107,84 |
| `runtime_catalog_sources\cover_files` | 605 | 44.445.009 | 42,39 |
| `%LocalAppData%\NexaPlay\metadata_detail` | 206 | 5.528.293 | 5,27 |
| `%LocalAppData%\NexaPlay\WebView2` | 318 | 31.991.055 | 30,51 |

Untuk 206 file JSON detail: minimum 1.657 byte, median 24.994 byte, maksimum 108.451 byte. File katalog besar yang diamati adalah `games_filter_index_cache_v3.json` 26.206.301 byte, `steam_data.json.gz` 25.354.414 byte, dan `steam_data.json` 13.271.511 byte.

Rekomendasi arsitektur awal: **hybrid katalog SQLite lokal ditambah detail JSON per AppID yang dimuat secara lazy**. SQLite sebaiknya menjadi satu-satunya penyimpanan ringan untuk daftar/search/filter. JSON detail penuh tetap diminta saat diperlukan. Feed JSON kecil berversi tetap dapat dipakai untuk daftar AppID populer/baru dan manifest update.

## 2. Alur Startup

### Rantai pemanggilan aktual

```text
Constructor App
-> mendaftarkan pencatatan crash
-> mendaftarkan service DI dan membangun IServiceProvider
-> App.OnLaunched membuat MainWindow dan mengaktifkannya
-> MainWindow.OnFirstActivated
-> RunStartupLicenseFlowAsync
-> LicenseService.LoadAsync membaca license lokal terenkripsi
-> LicenseService.ValidateExistingAsync mengirim POST ke Supabase
-> navigasi ke Home
-> memulihkan manifest Steam terkelola secara fire-and-forget
-> WarmupMetadataStartupAsync
   -> MetadataService.WarmupEssentialSourcesAsync
      -> warm: parse katalog lokal, lalu revalidasi di background
      -> cold: download/revalidasi source, materialisasi game baru, bangun indeks lengkap
   -> GameCoverIndexService.WarmupAsync
      -> parse indeks library capsule lokal atau download jika belum ada
   -> HomeViewModel.LoadAsync
   -> GamesViewModel.LoadAsync
      -> baca/bangun indeks filter dan hidrasi halaman card aktif
-> menunggu Home dan overlay siap
-> AppUpdateService.CheckForUpdatesAsync
-> Home siap digunakan
```

### Entry point dan service

- `App.ConfigureServices()` mendaftarkan service data sebagai singleton: override, tutorial, update, metadata, indeks cover, cache gambar cover, katalog bypass, penyimpanan detail, resolver cover daftar, penyimpanan state, serta view model Home/Games. `LibraryViewModel`, `BypassGamesViewModel`, Settings, dan view model detail bersifat transient. Sumber: `NexaPlay/App.xaml.cs:47-85`; dipanggil oleh `App()` pada baris 27-41.
- `App.OnLaunched()` me-resolve dan mengaktifkan `MainWindow`. Sumber: `NexaPlay/App.xaml.cs:88-99`.

### Gerbang license

- Startup lebih dulu memuat `license.dat` melalui cache memori dalam proses, lalu mendekripsi dan memparsenya di thread background. Sumber: `NexaPlay/Infrastructure/Services/LicenseService.cs:23-56`, metode `LoadAsync()`, menggunakan `LicenseStore.Load()` pada `NexaPlay/Infrastructure/Persistence/LicenseStore.cs:41-120`.
- License offline yang valid tetap diperiksa secara online melalui `POST {SupabaseUrl}/rest/v1/rpc/activate_license`, dengan timeout sepuluh detik. Startup menunggu alur ini; error jaringan/offline membuat UI validasi menunggu tindakan pengguna. Sumber: `NexaPlay/MainWindow.xaml.cs:550-648`; `NexaPlay/Infrastructure/Services/LicenseService.cs:71-112`, `115-207`.
- Navigasi Home baru terjadi setelah alur license selesai. Sumber: `NexaPlay/MainWindow.xaml.cs:205-233`, metode `RunStartupLicenseFlowAsync()`.

### Pemanasan metadata

- Startup warm: `MetadataService.WarmupEssentialSourcesAsync()` mendeteksi source lokal yang dapat digunakan, membangun indeks secara sinkron jika belum ada, lalu memulai `PerformBackgroundUpdateAsync()` tanpa menunggunya. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:243-260`.
- Startup cold: metode menunggu sinkronisasi source, memuat ulang cache samping AppID, mematerialisasi metadata game baru yang belum ada, dan membangun katalog lengkap. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:262-289`.
- `SyncSourcesCoreAsync()` lebih dulu mencoba `catalog_manifest.json`, memvalidasi ukuran dan SHA-256, mendownload file berubah dengan concurrency tiga, lalu memakai ETag per file sebagai fallback untuk file yang tidak tercakup. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:354-394`, `396-669`.
- Delapan source katalog yang disinkronkan adalah `steam_data.json`, `steam_data.json.gz`, `override_data.json`, `fix_games.json`, `new_fix_games.json`, `steam_games/steam_games.json`, `appid_populer.json`, dan `new_games.json`. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:384-394`, metode `GetCatalogSources()`.
- `BuildIndexAsync()` memparse kedua file utama ketika keduanya tersedia. Gzip bukan fallback eksklusif. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:695-708`.
- Proses parse memakai `JsonDocument` untuk seluruh source dan dictionary yang dialokasikan awal untuk 160.000 entri. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:842-960`, terutama `MergeSourceAsync()` dan pembuatan dictionary pada baris 700.

### Pramuat cover dan halaman

- Pemanasan indeks cover ditunggu bersama/setelah pemanasan metadata. Proses ini memparse seluruh indeks gzip menjadi dictionary berkapasitas 160.000; JSON biasa hanya menjadi fallback jika gzip tidak dapat digunakan. Sumber: `NexaPlay/Infrastructure/Services/GameCoverIndexService.cs:102-153`, `183-242`.
- Startup kemudian menunggu `HomeViewModel.LoadAsync()` dan `GamesViewModel.LoadAsync()` meskipun pengguna belum membuka Games. Sumber: `NexaPlay/MainWindow.xaml.cs:393-400`.
- `HomePage.OnNavigatedTo()` memanggil `HomeViewModel.LoadAsync()` lagi, tetapi `CatalogLoadCoordinator` mencegah pekerjaan duplikat untuk generasi refresh yang sama. Sumber: `NexaPlay/Presentation/Views/Pages/HomePage.xaml.cs:139-148`; `NexaPlay/Core/Helpers/CatalogLoadCoordinator.cs:3-35`.
- Pemulihan manifest terkelola adalah satu-satunya task startup fire-and-forget yang eksplisit dalam chain ini. Sumber: `NexaPlay/MainWindow.xaml.cs:224-246`.
- Pemeriksaan update baru berjalan setelah Home siap. Proses ini memakai cooldown state disk selama enam jam. Sumber: `NexaPlay/MainWindow.xaml.cs:424-435`; `NexaPlay/Infrastructure/Services/AppUpdateService.cs:62-78`.

### Klasifikasi blocking

| Operasi | Async | Bisa memakai jaringan | Pekerjaan file penuh | Memblokir Home pertama |
| --- | --- | --- | --- | --- |
| Memuat license lokal | Ya | Tidak | File terenkripsi kecil | Ya |
| Validasi license online | Ya | Ya | JSON kecil | Ya |
| Sinkronisasi katalog | Ya | Ya | Hingga delapan source | Ya saat cold start; tidak untuk revalidasi warm |
| Membangun indeks katalog | Ya | Tidak | Source utama/override/protection penuh | Ya |
| Membangun indeks cover | Ya | Ya jika belum tersedia | Gzip/JSON penuh | Ya |
| Memuat Home | Ya | Mungkin meminta detail/cover | Salinan katalog dan feed | Ya |
| Memuat indeks/halaman Games | Ya | Mungkin memakai fallback cover/detail | Indeks filter penuh dan satu halaman | Ya |
| Memeriksa update | Ya | Ya setelah cooldown enam jam | Manifest kecil | Setelah pemeriksaan kesiapan Home, tetapi tetap ditunggu sebelum urutan startup selesai |

## 3. Alur Data Home

### Rantai pemanggilan aktif

```text
HomePage.OnNavigatedTo
-> HomeViewModel.LoadAsync
-> CatalogLoadCoordinator.RunAsync
-> HomeViewModel.LoadCoreAsync
   -> BypassGamesDataService.GetAllFixesAsync
   -> AddGameService.ListLibraryGames memindai file .lua Steam
   -> MetadataService.GetNewFixAppIdsAsync
   -> bangun lookup bypass dari katalog new/fix/steam
   -> resolve maksimal 12 hero terbaru
   -> MetadataService.GetPopularAppIdsAsync
   -> salin snapshot katalog ke dictionary Home
   -> hidrasi batch populer pertama
   -> cache gambar cover dan prefetch batch berikutnya
```

- Pemuatan inti membaca seluruh daftar fix, menghitung file `.lua` library, membaca AppID new-fix, dan me-resolve maksimal 12 item terbaru. Sumber: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs:80-137`, metode `LoadCoreAsync()`.
- Pembangunan lookup bypass terbaru membaca ketiga koleksi bypass: fix baru, fix, dan game Steam. Sumber: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs:154-172`, metode `BuildBypassLookupAsync()`.
- Data populer dimulai sebagai daftar AppID dari `appid_populer.json`; card di-resolve terhadap salinan penuh snapshot katalog dalam memori. Sumber: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs:176-205`, `337-406`, `441-450`.
- Ukuran halaman populer adalah `columns * 8`; aksi muat lebih banyak menambah `columns * 4`. Batch muat-lebih-banyak berikutnya di-prefetch. Sumber: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs:33-43`, `212-268`.
- Pengayaan hero terbaru dapat memanggil detail penuh R2 untuk setiap hero saat override hero tidak tersedia. Sumber: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs:703-748`, metode yang digunakan oleh pengayaan cover terbaru.
- Resolusi cover populer lebih dahulu memakai katalog/override/indeks cover dan menyimpan byte gambar aktual ke cache disk. Detail penuh hanya menjadi fallback. Sumber: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs:518-548`; `NexaPlay/Infrastructure/Services/ListingCoverResolver.cs:35-57`.

### Field yang benar-benar di-bind oleh Home

Recent hero `FixEntry`:

- `PosterUrl`
- `IsPremium`
- `Title`
- `Category`
- `AppId` untuk navigasi/tindakan

Bukti: `NexaPlay/Presentation/Views/Pages/HomePage.xaml:69-135`.

Card populer `GameEntry`:

- `AppId`
- `PopularCoverImageUrl`
- `HasPopularCover`
- `IsPremium`
- `HasDenuvo`
- `Name`

Bukti: `NexaPlay/Presentation/Views/Pages/HomePage.xaml:208-292`. Harga tidak dirender di Home.

### Jawaban

- Home tidak sengaja mengurai detail penuh untuk semua game populer. Home memakai entri katalog ringan, tetapi fallback hero terbaru dapat mengambil detail penuh hingga 12 item.
- Semua AppID populer dipertahankan, tetapi hanya batch yang terlihat yang dihidrasi. Batch berikutnya dihidrasi secara proaktif.
- Gambar aktual diunduh melalui `CoverImageCacheService`; file yang berhasil akan dirender secara lokal pada kunjungan berikutnya.
- Kembali ke Home biasanya menggunakan ulang state yang dikoordinasikan berdasarkan generation. Kemajuan catalog generation atau reload eksplisit akan membersihkan dictionary dan koleksi turunan.

## 4. Alur Data Games

```text
GamesPage.OnNavigatedTo / pramuat startup
-> GamesViewModel.LoadAsync
-> muat games_filter_index_cache_v3.json jika revision cocok
   ATAU MetadataService.GetCatalogSnapshotAsync
      -> petakan setiap GameEntry ke GameFilterIndex
      -> simpan indeks filter JSON duplikat
-> filter/cari seluruh baris indeks
-> urutkan baris yang cocok secara acak
-> ambil halaman saat ini (3-6 kolom * 10 baris)
-> petakan AppID ke card FixEntry
-> resolve URL cover
-> cache byte gambar
-> render
-> prefetch dua halaman berikutnya
```

- Entri halaman dimuat hanya saat navigasi bukan Back dan view model belum memiliki card, tetapi startup sudah memanggil singleton view model yang sama. Sumber: `NexaPlay/Presentation/Views/Pages/GamesPage.xaml.cs:34-46`; `NexaPlay/MainWindow.xaml.cs:393-397`.
- Path cache indeks filter adalah `%LocalAppData%\NexaPlay\runtime_catalog_sources\games_filter_index_cache_v3.json`. Sumber: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:65-84`.
- Saat cache miss, setiap baris katalog disalin ke `GameFilterIndex(AppId, NameLower, PriceNormalized, IsPremium, HasDenuvo, GenreTokens)` dan ditulis kembali sebagai JSON. Sumber: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:141-166`, `695-709`, `744-801`.
- Ukuran halaman adalah 30-60 item, tergantung 3-6 kolom. Hanya card halaman saat ini yang dipublikasikan; dua halaman berikutnya di-prefetch. Sumber: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:19-20`, `56-63`, `183-244`, `420-445`.
- Card me-resolve metadata berdasarkan AppID, lalu prioritas cover, lalu cache gambar disk. Sumber: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:450-534`, metode `GetOrBuildCardFastAsync()` dan `GetOrBuildListingCardAsync()`.
- UI card memakai poster, judul, AppID, premium/standard, dan Denuvo. Harga, penerbit, deskripsi, dan genre tidak dirender pada card. Bukti: `NexaPlay/Presentation/Views/Pages/GamesPage.xaml:219-319`.
- Metadata detail penuh biasanya tidak diperlukan untuk card Games. Metadata ini hanya diambil saat override, indeks cover, dan capsule runtime semuanya tidak tersedia lalu `ListingCoverResolver` memakai fallback `SteamStoreService.GetDetailAsync()`. Sumber: `NexaPlay/Infrastructure/Services/ListingCoverResolver.cs:35-57`, `142-150`.

## 5. Alur Data Pencarian

### Rantai pemanggilan pencarian Games

```text
Pengguna mengubah SearchQuery
-> GamesViewModel.OnSearchQueryChanged
-> DebounceSearch menunggu 220 ms
-> tolak query nonkosong yang lebih pendek dari 3 karakter
-> ApplyFiltersAndPaginationAsync
-> BuildFilteredAppIdsAsync pada thread pekerja
   -> kecocokan tepat AppID integer, selain itu substring nama ternormalisasi
   -> filter premium/standard, Denuvo, genre
   -> saat tidak ada teks pencarian, kecualikan harga di bawah 100000
   -> OrderBy(random.Next())
-> hidrasi card halaman
-> perbarui UI dan prefetch dua halaman berikutnya
```

Bukti: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:183-244`, `332-363`, `538-602`, `667-693`.

Jawaban:

1. Ya. Daftar penuh `GameFilterIndex` berada di RAM sebelum pencarian.
2. Pencarian tidak membaca ulang JSON besar pada setiap ketikan. Startup/pemuatan membaca JSON indeks filter tersimpan satu kali atau membangunnya dari snapshot katalog.
3. Pemfilteran pencarian sendiri tidak membaca file per AppID. Hidrasi halaman hasil dapat meminta/menyimpan cover ke cache dan dapat mencapai fallback detail.
4. Pencarian sepenuhnya lokal.
5. Ada indeks datar kustom dalam memori; ini bukan inverted index atau indeks database.
6. Field yang dapat dicari adalah AppID tepat atau `NameLower` ternormalisasi. Genre memengaruhi filter terpisah, bukan pencarian teks. Publisher tidak dapat dicari di Games.
7. Pemfilteran adalah O(n). Pengacakan menambah pengurutan O(m log m) untuk `m` kecocokan pada setiap eksekusi filter/pencarian.
8. Data berasal dari katalog yang sama, tetapi membuat struktur RAM lain dan JSON disk besar lain.
9. Objek card `GameEntry`, `GameFilterIndex`, `GameFilterIndexCacheItem`, dan `FixEntry` menduplikasi field listing yang tumpang tindih.

### Pencarian lain

- Pencarian Library menunggu 300 ms, lalu mencocokkan AppID tepat atau nama ternormalisasi terhadap daftar library kecil. Sumber: `NexaPlay/Presentation/ViewModels/LibraryViewModel.cs:135-176`, `346-359`.
- Pencarian Bypass berjalan sinkron pada setiap perubahan properti, tanpa debounce, dengan mencocokkan judul, penerbit, atau substring AppID. Sumber: `NexaPlay/Presentation/ViewModels/BypassGamesViewModel.cs:155-177`, `281-283`.
- `MetadataService.SearchAsync()` tidak digunakan oleh UI. Referensi repository hanya ada di `IMetadataService` dan `MetadataService`; `SteamGuardService.SearchAsync()` adalah API mail yang tidak terkait. Sumber: `NexaPlay/Contracts/Services/IMetadataService.cs:10`; `NexaPlay/Infrastructure/Services/MetadataService.cs:118-127`.

## 6. Alur Data Detail Game

```text
Klik card Games/Home yang membawa AppID
-> NavigationService membuka GameDetailPage(AppID)
-> GameDetailViewModel.LoadAsync(AppID)
   -> MetadataService.GetMetadataAsync: lookup katalog O(1)
   -> BypassGamesDataService.GetAllFixesAsync: cari entri fix
   -> AppliedStateStore / AddGameService: state diterapkan dan terinstal
   -> SteamStoreService.GetDetailAsync
      -> JSON disk masih berlaku: baca dan uraikan file penuh
      -> jika tidak, GET R2 Metadata/{appid}.json
      -> tulis JSON mentah penuh ke metadata_detail/{appid}.json
      -> uraikan GameDetailEntry dan pertahankan RawMetadataJson
      -> terapkan override detail NexaPlay
-> pilih URL media dan render detail/WebView2
```

- Navigasi Games dan Home hanya meneruskan AppID. Sumber: `NexaPlay/Presentation/Views/Pages/GamesPage.xaml.cs:143-153`; `NexaPlay/Presentation/Views/Pages/HomePage.xaml.cs:273-276`.
- `GameDetailViewModel.LoadAsync()` menjalankan tiga tahap di atas. Sumber: `NexaPlay/Presentation/ViewModels/GameDetailViewModel.cs:245-326`.
- TTL cache detail adalah tujuh hari. Kunjungan ulang dengan cache yang masih berlaku menghindari jaringan, tetapi tetap membaca dan mengurai ulang JSON penuh dari disk; tidak ada cache memori untuk `GameDetailEntry`. Sumber: `NexaPlay/Infrastructure/Services/SteamStoreService.cs:14-67`.
- Respons penuh diurai dari `store_data` dan `assets` untuk deskripsi, media, persyaratan, kategori, dukungan, pemberitahuan, dan harga, lalu string JSON mentah yang sama dipertahankan dalam `RawMetadataJson`. Sumber: `NexaPlay/Infrastructure/Services/SteamStoreService.cs:102-145`.
- Override detail NexaPlay diterapkan setelah parsing dasar dan mempertahankan payload mentah. Sumber: `NexaPlay/Infrastructure/Services/SteamStoreService.cs:254-317`.
- Protection dan premium yang ditampilkan pada detail berasal dari `GameEntry`, bukan `GameDetailEntry`. Sumber: `NexaPlay/Presentation/ViewModels/GameDetailViewModel.cs:155-166`.
- Tangkapan layar, hero, video, dan media detail lain tetap berupa URL remote. Data tersebut tidak melewati `CoverImageCacheService`; WebView2 dan kontrol gambar XAML mengelola perilaku fetch/cache sendiri.

## 7. Alur Data Library

### Sumber kebenaran penyimpanan

- Keanggotaan Library disimpulkan dari nama file `Steam\config\stplug-in\{appid}.lua`. File disabled dihitung oleh `IsGameInstalled()`, sedangkan `ListLibraryGames()` hanya mengenumerasi `*.lua`. Sumber: `NexaPlay/Infrastructure/Services/AddGameService.cs:29-79`.
- Karena itu, Library secara implisit hanya menyimpan AppID. Library tidak menduplikasi nama, cover, penerbit, deskripsi, atau metadata lain dalam JSON/database library.
- `Clear Data` tidak menghapus file `.lua` Steam karena hanya menghapus `%LocalAppData%\NexaPlay`. Keanggotaan Library tetap bertahan, meskipun cache metadata lokal aplikasi hilang. Sumber: `NexaPlay/Presentation/ViewModels/SettingsViewModel.cs:339-393`.

### Rantai pemuatan

```text
LibraryViewModel.LoadAsync
-> AddGameService.ListLibraryGames memindai stplug-in/*.lua
-> untuk setiap AppID: metadata katalog + manifest/nama/path Steam + applied state
-> simpan daftar InstalledGame di RAM
-> pencarian/paginasi lokal, 10 item/halaman
-> untuk setiap item yang terlihat:
   MetadataService.GetMetadataAsync
   NexaPlayOverrideService.GetCatalogOverrideAsync
   SteamStoreService.GetDetailAsync
-> bangun LibraryGameCard
-> render
```

- Pemindaian awal library dan hidrasi dasar berjalan berurutan per AppID. Sumber: `NexaPlay/Presentation/ViewModels/LibraryViewModel.cs:100-128`.
- Hidrasi card halaman terlihat juga berjalan berurutan dan selalu memanggil detail penuh untuk masing-masing hingga sepuluh card. Sumber: `NexaPlay/Presentation/ViewModels/LibraryViewModel.cs:135-194`.
- Field UI adalah AppID, judul, poster, genre, premium/standard, dan Denuvo. Sumber: `NexaPlay/Presentation/Views/Pages/LibraryPage.xaml:171-243`.

Kesimpulan: persistensi Library sudah dinormalisasi dengan tepat. Masalah utama adalah hidrasi detail N+1 untuk artwork dan lookup katalog berulang, bukan duplikasi metadata library tersimpan.

## 8. Alur Data Bypass

### Source daftar dan rantai pemanggilan

```text
BypassGamesPage
-> BypassGamesViewModel.LoadAsync
-> BypassGamesDataService.GetAllFixesAsync: fix_games.json
-> BypassGamesDataService.GetSteamGamesAsync: steam_games.json
-> simpan kedua daftar lengkap di RAM
-> filter/urutkan daftar terpilih, tanpa paginasi
-> publikasikan setiap card yang cocok
-> mulai pengayaan cover background untuk kedua daftar lengkap
   -> lookup metadata per item
   -> lookup override NexaPlay per item
   -> lookup detail penuh/request R2 per item
   -> fallback URL CDN Steam
```

- `new_fix_games.json` bukan bagian dari daftar utama Bypass. File ini digunakan oleh recent fixes Home dan sebagai fallback resolusi detail. Sumber: `NexaPlay/Presentation/ViewModels/BypassGamesViewModel.cs:87-113`; `NexaPlay/Presentation/ViewModels/BypassGameDetailViewModel.cs:1154-1166`.
- Kedua daftar aktif Bypass dimuat penuh dan disimpan dalam memori. Pemfilteran mengurutkan seluruh kumpulan yang cocok dan tidak memiliki paginasi. Sumber: `NexaPlay/Presentation/ViewModels/BypassGamesViewModel.cs:87-177`.
- Pengayaan cover berjalan pada kedua daftar source lengkap dengan concurrency empat dan memanggil `GetDetailAsync()` tanpa syarat untuk setiap item, bahkan saat kandidat cover sebelumnya tersedia. Sumber: `NexaPlay/Presentation/ViewModels/BypassGamesViewModel.cs:184-264`.
- Pencarian Bypass tidak memiliki debounce dan mencocokkan judul, penerbit, dan AppID. Sumber: `NexaPlay/Presentation/ViewModels/BypassGamesViewModel.cs:155-177`, `281-283`.
- UI daftar Bypass merender AppID/tag, poster, premium/standard, badge offline/sharing, dan judul. UI tidak merender badge Denuvo. Sumber: `NexaPlay/Presentation/Views/Pages/BypassGamesPage.xaml:163-251`.

### Detail Bypass

- Halaman detail me-resolve data katalog dasar, entri Bypass, detail penuh R2/cache, hero override katalog, dan JSON tutorial. Sumber: `NexaPlay/Presentation/ViewModels/BypassGameDetailViewModel.cs:126-215`.
- Prioritas resolusi entri Bypass adalah kecocokan `steam_games`, lalu `fix_games`, lalu `new_fix_games`. Sumber: `NexaPlay/Presentation/ViewModels/BypassGameDetailViewModel.cs:1154-1166`.
- Metadata tutorial memakai `youtube.json` remote, ETag, cache disk, dan interval refresh minimum dua menit. Sumber: `NexaPlay/Infrastructure/Services/BypassTutorialVideoService.cs:27-40`, `43-112`.
- Unduhan arsip bypass pihak ketiga memakai URL Google Drive yang disimpan dalam `FixEntry.Files`; data sementara ditempatkan di `%TEMP%\NexaPlayFix\{appid}`. Sumber: `NexaPlay/Presentation/ViewModels/BypassGameDetailViewModel.cs:325-363`, `688-753`.

## 9. Protection / Premium / Standard

### Aturan status final aktual

```text
steam_data.json.gz + steam_data.json + new_games_catalog.json + override_data.json
                                      |
fix_games.json + new_fix_games.json + steam_games.json -> protected AppID set
                                      |
                                      v
                         GameEntry.Protection
                                      |
                 override final nexaplay_override.json
                                      |
                 HasDenuvo = Protection
                 IsPremium = PriceNormalized >= 130000
                 Standard = !IsPremium
```

- Premium katalog dihitung, bukan disimpan terpisah: `PriceNormalized >= 130_000`. Sumber: `NexaPlay/Core/Models/GameEntry.cs:20-25`.
- Protection katalog dimulai dari sumber `protection`, lalu menjadi true untuk setiap AppID yang ditemukan dalam `fix_games.json`, `new_fix_games.json`, atau `steam_games.json`. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:743-768`, metode `LoadProtectionAppIdsAsync()` dan `BuildIndexAsync()`.
- Override katalog NexaPlay berjalan terakhir dan dapat mengganti harga maupun protection. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:786-838`.
- Premium Bypass bersifat independen: nilainya adalah boolean literal `premium` dari `fix_games.json`/`steam_games.json`. Sumber: `NexaPlay/Infrastructure/Services/BypassGamesDataService.cs:242-309`, `319-379`.
- `FixEntry.HasDenuvo` tersedia, tetapi `BypassGamesDataService` tidak menetapkannya dalam kedua parser. UI daftar Bypass juga tidak melakukan binding terhadapnya. Sumber: `NexaPlay/Core/Models/FixEntry.cs:17-27`; penetapan parser di `NexaPlay/Infrastructure/Services/BypassGamesDataService.cs:304-309`, `365-370`.
- Card Games membangun `FixEntry.HasDenuvo` dari katalog `GameEntry.HasDenuvo`, sehingga badge Denuvo aktif. Sumber: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:450-485`.

Implikasi: AppID yang sama dapat memiliki tier katalog yang berasal dari harga, sementara tier Bypass berasal dari boolean source eksplisit. Keduanya dapat berbeda. Protection juga lebih luas daripada field metadata literal karena keanggotaan dalam source bypass mana pun memaksanya menjadi true sebelum override final.

## 10. Pengaturan

### Muat Games

```text
SettingsViewModel.LoadGamesAsync
-> paralel: NexaPlayOverrideService.RefreshAsync + GameCoverIndexService.RefreshAsync
-> MetadataService.RefreshDynamicSourcesAsync
   -> sinkronkan manifest/ETag delapan source
   -> muat ulang cache samping AppID
   -> bangun ulang katalog lengkap
   -> ambil detail new-game yang hilang dari R2, concurrency maksimal 4
   -> tulis new_games_catalog.json
-> BypassGamesDataService.RefreshAsync
   -> refresh fix/new-fix/steam-games lagi melalui jalur HTTP/TTL sendiri
-> majukan catalog generation
-> bersihkan cache memori listing-cover
-> paksa reload Home
-> paksa reload Games dan penulisan ulang indeks filter
```

Bukti: `NexaPlay/Presentation/ViewModels/SettingsViewModel.cs:279-337`; `NexaPlay/Infrastructure/Services/MetadataService.cs:221-240`, `354-394`, `970-1024`; `NexaPlay/Infrastructure/Services/BypassGamesDataService.cs:79-102`.

Efek:

- File source yang ada diganti secara atomik hanya saat berubah/divalidasi oleh jalur metadata.
- Katalog penuh dan indeks cover dibangun ulang.
- AppID `new_games.json` yang tidak ada dalam katalog utama dapat memicu satu request detail R2 per AppID, dengan concurrency empat.
- File fix/new-fix/steam dapat diunduh untuk kedua kalinya karena `BypassGamesDataService` memiliki logika freshness terpisah atas path lokal yang sama.
- State turunan Home dan Games dibuang lalu langsung diisi ulang.

### Bersihkan Cache

`SettingsViewModel.ClearMetadataCacheAsync()` melakukan:

| Cache | Tindakan | Sumber |
| --- | --- | --- |
| Indeks RAM metadata dan cache memori AppID | Hanya di-reset; JSON source dipertahankan | `NexaPlay/Infrastructure/Services/MetadataService.cs:292-304` |
| RAM indeks cover dan `library_capsule.json(.gz)` | Dihapus | `NexaPlay/Infrastructure/Services/GameCoverIndexService.cs:92-99` |
| File gambar card yang diunduh | Direktori dilepas lalu dihapus di background | `NexaPlay/Infrastructure/Services/CoverImageCacheService.cs:142-162` |
| Cache JSON detail | Direktori dilepas lalu dihapus di background | `NexaPlay/Infrastructure/Services/SteamStoreService.cs:323-341` |
| Cache indeks filter Games | Dihapus oleh invalidasi cache turunan | `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:103-111` |
| Cache RAM Home/Games/resolver | Dibersihkan; generation dimajukan | `NexaPlay/Presentation/ViewModels/SettingsViewModel.cs:255-270` |

Tidak dibersihkan:

- Katalog source utama (`steam_data*`, `override_data`, daftar fix, feed popular/new, manifest).
- License, file pengaturan/state, applied state, managed manifest, log.
- File `.lua` Library.
- Cache/data pengguna WebView2.
- Unduhan/state update.

Setelah Clear Cache, indeks cover, gambar cover, JSON detail, dan indeks filter Games harus dibuat ulang atau diunduh saat digunakan. File source katalog tetap ada, sehingga unduhan source penuh tidak otomatis diperlukan.

### Bersihkan Data

- `ClearAllDataAndRestartAsync()` menulis `.cmd` tertunda, keluar, menghapus secara rekursif tepat `%LocalAppData%\NexaPlay`, memulai ulang executable, lalu menghapus script helper. Sumber: `NexaPlay/Presentation/ViewModels/SettingsViewModel.cs:339-393`.
- Tindakan ini menghapus source katalog, file cover, file detail, WebView2, license, cache device ID, log aplikasi, applied state, batas laporan, daftar managed manifest, cache tutorial, state update, dan unduhan update karena semuanya berada di bawah root tersebut.
- Tindakan ini tidak menghapus `config\stplug-in\*.lua` Steam, log fix dalam direktori game, `%LocalAppData%\GameHub\downloads`, atau `%LocalAppData%\GameHub\license.dat` legacy.
- Karena `LicenseStore.Load()` memakai fallback license GameHub dan memigrasikannya, Clear Data dapat diikuti pemulihan license dari file legacy. Sumber: `NexaPlay/Infrastructure/Persistence/LicenseStore.cs:23-30`, `41-110`.

## 11. Inventaris Source Data

| Source data | Lokasi | Isi | Dibuat/diunduh oleh | Pembaca/fitur aktif | Jaringan/lokal | Redundansi |
| --- | --- | --- | --- | --- | --- | --- |
| `catalog_manifest.json` + ETag | `runtime_catalog_sources` | Revision, ukuran file, SHA-256, URL | `MetadataService.TrySyncFromManifestAsync()` | Refresh katalog | GitHub -> disk | Metadata kontrol, bukan duplikasi game |
| `steam_data.json.gz` | sama | Katalog game utama besar | `MetadataService` | Pembangunan katalog | GitHub -> disk | Katalog logis yang sama dengan JSON biasa |
| `steam_data.json` | sama | Katalog game utama biasa | `MetadataService` | Pembangunan katalog | GitHub -> disk | Keduanya diurai saat sama-sama tersedia |
| `override_data.json` | sama | Override arsip/katalog | `MetadataService` | Pembangunan katalog | GitHub -> disk | Tumpang tindih dengan field listing utama |
| `nexaplay_override.json` + ETag | sama | Override katalog dan detail sparse | `NexaPlayOverrideService` | Katalog, detail, resolver cover | GitHub -> disk/RAM | Sengaja tumpang tindih dengan field listing dan detail |
| `fix_games.json` | `runtime_catalog_sources`; salinan root stale/legacy teramati secara lokal | Game Bypass, data akun/file, premium, poster | Service metadata dan Bypass | Home, Bypass, Detail Game, protection | GitHub -> disk/RAM | Service saat ini berbagi path runtime tetapi mengurai/refresh secara independen |
| `new_fix_games.json` | `runtime_catalog_sources`; salinan root stale/legacy teramati secara lokal | Game bypass terbaru/baru | Service metadata dan Bypass | Recent Home, protection, fallback detail | GitHub -> disk/RAM | Service saat ini berbagi path runtime tetapi mengurai/refresh secara independen |
| `steam_games.json` | `runtime_catalog_sources`; salinan root stale/legacy teramati secara lokal | Entri akun/sharing Steam | Service metadata dan Bypass | Tab Steam Bypass, protection | GitHub -> disk/RAM | Service saat ini berbagi path runtime tetapi mengurai/refresh secara independen |
| `appid_populer.json` | sama | Daftar AppID populer | `MetadataService` | Home | GitHub -> disk/RAM | Feed kecil; tepat sebagai daftar terpisah |
| `new_games.json` | sama | Daftar AppID baru | `MetadataService` | Materialisasi | GitHub -> disk/RAM | Feed kecil |
| `new_games_catalog.json` | sama | Baris listing termaterialisasi untuk ID baru yang hilang | `MetadataService.MaterializeNewGamesAsync()` | Pembangunan katalog | Lokal turunan R2 | Menduplikasi subset field detail dan katalog |
| `library_capsule.json.gz/json` | sama | AppID -> URL cover | `GameCoverIndexService` | Resolver cover Home/Games | GitHub -> disk/RAM | Menduplikasi cover yang tersedia dalam data utama/detail |
| `games_filter_index_cache_v3.json` | sama | AppID, nama ternormalisasi, harga, premium, Denuvo, token genre | `GamesViewModel` | Games/Pencarian | Turunan lokal | Duplikat besar field listing katalog |
| `cover_files/*` | sama | Byte gambar yang diunduh | `CoverImageCacheService` | Card Home/Games | Gambar remote -> disk | Cache biner yang disengaja; tanpa batas |
| `{appid}.json` | `%LocalAppData%\NexaPlay\metadata_detail` | Payload detail penuh R2 | `SteamStoreService` | Detail, Library, fallback cover, Bypass | R2 -> disk | AppID dan sebagian field listing yang sama diulang |
| `youtube_tutorial.json/.etag` | root app-data | Pemetaan tutorial | `BypassTutorialVideoService` | Detail Bypass | GitHub -> disk/RAM | Domain terpisah |
| `license.dat` | root app-data | JSON license terenkripsi AES | `LicenseStore` | Gerbang startup/license | Lokal + validasi Supabase | Duplikat legacy mungkin ada di GameHub |
| `device_id.cache` | root app-data | Identifier perangkat | `DeviceIdHelper` | License | Lokal | State terpisah |
| `applied_state.json` | root app-data | AppID -> boolean applied | `AppliedStateStore` | Online fix/detail/library | Lokal | State kecil |
| `managed_appmanifests.json` | root app-data | AppID Steam terkelola | `ManagedManifestStore` | Pemulihan Steam saat startup | Lokal | State kecil |
| `account_report_limits.json` | root app-data | Tanggal/jumlah UTC per AppID | `AccountReportLimitStore` | Pelaporan akun | Lokal | State kecil |
| `app_update_state.json` | root app-data | Hasil/waktu pemeriksaan update terakhir | `AppUpdateService` | Startup/Pengaturan | Lokal turunan GitHub | Cache kecil |
| `nexaplay.log` | root app-data | Log aplikasi hanya-tambah | `AppLogService` | Pengaturan/dukungan | Lokal | Tidak ada batas ukuran file dalam service |
| `WebView2/*` | root app-data | Profil/cache/storage browser | Runtime WebView2 | Deskripsi game/tutorial | Jaringan/lokal | Tidak dibersihkan oleh Clear Cache |
| `data/api.json` | direktori aplikasi terinstal atau override pada root app-data | Provider unduhan Add Game berurutan | Konten paket/override pengguna | `AddGameService` | Konfigurasi lokal yang menggerakkan jaringan | Mengandung tumpang tindih provider secara sengaja |
| `{appid}.lua` | `config\stplug-in` Steam | Script plugin/library terinstal | `AddGameService` | Library dan state terinstal | Lokal di luar app-data | AppID adalah sumber kebenaran Library |
| `nexaplay-fix-log-{appid}.log` | direktori instalasi game | Log file Online-Fix yang diterapkan | `OnlineFixService` | Penghapusan/pemulihan | Lokal di luar app-data | State operasional per game |
| Arsip sementara | `%TEMP%\NexaPlay`, `%TEMP%\NexaPlayFix`, `%TEMP%\nexaplay-onlinefix` | Unduhan/ekstraksi bertahap | Service metadata/bypass/fix | Alur refresh/instalasi | Lokal sementara | Salinan transien yang diharapkan |

Definisi path: `NexaPlay/Core/Constants/AppConstants.cs:5-67`; konstruksi path katalog: `NexaPlay/Infrastructure/Services/MetadataService.cs:69-92`; konstruksi path Bypass: `NexaPlay/Infrastructure/Services/BypassGamesDataService.cs:27-37`; penyimpanan state: `NexaPlay/Infrastructure/Persistence/*.cs`. Kode Bypass saat ini tidak membaca tiga salinan tingkat root yang teramati pada workstation ini.

Dua konstanta tampak tidak aktif: `SteamDataCacheFileName` (`github_raw_full.json`) dan `UserOverrideDataCacheFileName` (`user_override_data.json`) hanya direferensikan pada deklarasinya. Sumber: `NexaPlay/Core/Constants/AppConstants.cs:46`, `56`.

## 12. Inventaris Request Jaringan

| Host/request | Pemicu | Respons/penggunaan | Frekuensi/cache | Blocking |
| --- | --- | --- | --- | --- |
| Manifest katalog dan delapan source `raw.githubusercontent.com/adii83/Nexaplay-Metadata-Override/...` | Cold startup, refresh background, Muat Games | Feed katalog/daftar | Manifest + SHA-256 + ETag; TTL keamanan background 24 jam | Ya untuk cold startup dan Muat Games |
| Repository GitHub yang sama, `nexaplay_override.json` | Penggunaan override pertama/startup, Muat Games | Override katalog/detail sparse | ETag + dictionary RAM | Dapat memblokir lookup/refresh |
| Repository GitHub yang sama, `library_capsule.json.gz` dengan fallback `.json` | Pemanasan startup, Muat Games | Indeks AppID-cover | ETag + disk + RAM | Ya saat startup |
| Repository GitHub yang sama, `youtube.json` | Membuka detail Bypass | Pemetaan tutorial | ETag, disk, RAM, refresh minimum 2 menit | Ya saat pemuatan detail Bypass |
| `meta.nexaplaymetadata.online/Metadata/{appid}.json` | Detail game; card Library; cover Bypass; fallback cover; materialisasi new-game | Metadata game penuh | TTL disk detail 7 hari; tanpa cache RAM detail | Pemanggil menunggu kecuali pengayaan background |
| Host CDN Steam dari metadata/URL fallback | Gambar card/detail yang terlihat | Byte gambar | Home/Games memakai cache cover disk; Bypass dan media detail mungkin bergantung pada cache HTTP platform | Pemuatan visual; sebagian persiapan card menunggu unduhan |
| RPC Supabase `activate_license` | Startup dan aktivasi | Status/paket license | File/RAM license tersedia, tetapi validasi online saat startup tetap terjadi | Ya saat startup |
| Manifest update GitHub | Setelah Home siap, Pengaturan | Versi dan URL installer | `app_update_state.json`, cooldown 6 jam | Ditunggu pemanggil |
| URL installer dari manifest update | Pengguna menerima update | Executable installer | File di bawah `updates`, SHA-256 diperiksa jika tersedia | Alur update eksplisit |
| `files.luatools.work/OnlineFix1/{appid}.zip` | Ketersediaan/penerapan Online-Fix | Arsip fix | Tanpa cache unduhan persisten; arsip sementara | Ya, tindakan pengguna |
| Release fallback Online-Fix GitHub | Source utama tidak tersedia | Arsip fallback generik | Tanpa cache persisten | Ya, tindakan pengguna |
| Provider dalam paket `data/api.json` | Tindakan Add Game | ZIP AppID | Fallback berurutan; sementara di `%LocalAppData%\GameHub\downloads` | Ya, tindakan pengguna |
| URL Google Drive dalam entri fix | Tindakan Bypass pihak ketiga | Arsip multipart | Hanya sementara | Ya, tindakan pengguna |
| Host YouTube/embed/thumbnail | Tutorial Bypass | Player dan thumbnail | Cache WebView2/browser | Interaksi/render pengguna |
| Telegram Bot API | Melaporkan akun Steam | Pesan laporan | State rate harian lokal, tanpa cache respons | Ya, tindakan pengguna |

Konstanta URL: `NexaPlay/Core/Constants/AppConstants.cs:8-33`. Request metadata: `NexaPlay/Infrastructure/Services/MetadataService.cs:354-669`, `1039-1061`; request detail: `NexaPlay/Infrastructure/Services/SteamStoreService.cs:78-99`; provider Add Game: `NexaPlay/Infrastructure/Services/AddGameService.cs:131-191` dan `NexaPlay/data/api.json:1-39`; Online-Fix: `NexaPlay/Infrastructure/Services/OnlineFixService.cs:36-72`, `75-151`; Google Drive: `NexaPlay/Presentation/ViewModels/BypassGameDetailViewModel.cs:688-753`; Telegram: `NexaPlay/Infrastructure/Services/TelegramAccountReportService.cs:39-70`.

Temuan N+1 dan duplikasi:

- N+1 kritis: request detail penuh per entri untuk kedua daftar Bypass.
- N+1 tinggi: request detail penuh per card pada halaman Library yang terlihat.
- N+1 bersyarat: fallback cover Games/Home per cover yang hilang.
- Otoritas unduhan duplikat: service metadata dan Bypass dapat me-refresh tiga file bypass yang sama secara independen.
- `data/api.json` mencakup provider HTTP biasa dan satu URL dengan kredensial query autentikasi tertanam. Ini adalah risiko keamanan/transportasi yang terpisah dari performa. Laporan sengaja tidak mereproduksi kredensial tersebut.

## 13. Inventaris Cache

| Cache | Key -> nilai | Lokasi | TTL | Invalidasi | Batas |
| --- | --- | --- | --- | --- | --- |
| RAM katalog | AppID -> `GameEntry` | Proses | Hingga refresh/clear | Update source, Clear Cache, generation | Tanpa batas baris eksplisit |
| Disk source katalog | nama file source -> JSON/gzip | `runtime_catalog_sources` | Manifest/ETag; keamanan background 24 jam untuk refresh new-game | Revision berubah/force | Tanpa batas ukuran total |
| RAM popular/new-fix | identitas daftar -> AppID | `MetadataService` | Throttle request 2 menit untuk refresh samping | Clear/reload | Kecil, bergantung source |
| RAM override | AppID -> override sparse | `NexaPlayOverrideService` | Pemeriksaan ETag saat startup | Refresh/clear | Tanpa batas eksplisit |
| RAM indeks cover | AppID -> URL | `GameCoverIndexService` | Umur proses | Refresh/clear | ~ukuran katalog |
| Disk indeks cover | gzip/JSON + ETag | `runtime_catalog_sources` | ETag | Refresh/Clear Cache | Tanpa batas ukuran total |
| RAM resolver listing | AppID -> URL cover R2 yang berhasil | Proses | Hingga clear/eviction | Antrean mirip FIFO di atas 512, generation clear | 512 keberhasilan |
| Listing in-flight | AppID -> task bersama | Proses | Umur request | Selesai/clear | Gate concurrency alami 4 |
| RAM filter Games | AppID/nama/harga/tier/protection/genre | `GamesViewModel` | Catalog generation | Reload/Clear Cache | Katalog penuh |
| Disk filter Games | Baris turunan yang sama | `games_filter_index_cache_v3.json` | Stamp revision source | Revision tidak cocok/Clear Cache | Tanpa batas byte |
| RAM card Games | AppID -> `FixEntry` | `GamesViewModel` | Catalog generation | Reload/Clear Cache | Bertambah bersama card yang dilihat/di-prefetch |
| Salinan katalog Home | AppID -> `GameEntry` hasil clone | `HomeViewModel` | Catalog generation | Reload/Clear Cache | Duplikat katalog penuh |
| Cache popular/card Home | card berurutan/AppID -> card | `HomeViewModel` | Catalog generation/siklus halaman | Reload/Clear Cache | Bertambah bersama item populer yang dimuat |
| Disk gambar cover | `appid_SHA1(url).ext` -> byte | `cover_files` | Tidak ada | Hanya Clear Cache | Tanpa batas ukuran/jumlah |
| Unduhan cover in-flight | path lokal -> task bersama | Proses | Umur request | Selesai/clear | Gate unduhan 6 |
| Disk detail | AppID -> JSON penuh | `metadata_detail` | 7 hari | Invalidasi per AppID/Clear Cache | Tanpa batas ukuran/jumlah |
| RAM detail | Tidak ada | N/A | N/A | N/A | Setiap akses mengurai ulang JSON disk |
| RAM Bypass | Jenis source -> daftar `FixEntry` | `BypassGamesDataService` | 24 jam | Refresh/restart proses | Tiga daftar lengkap |
| Disk Bypass | Tiga file JSON | Path app-data/katalog | Umur file 24 jam dalam service Bypass | Refresh/Clear Data | Tanpa batas ukuran total |
| RAM/disk tutorial | Kategori/AppID -> video | JSON root + ETag | Pemeriksaan ulang minimum 2 menit | ETag/Clear Data | Kecil |
| State disk update | Versi saat ini -> hasil/waktu | `app_update_state.json` | 6 jam | Force/perubahan versi/Clear Data | Kecil |
| RAM/disk license | Singleton + file terenkripsi | Proses/root | Proses; server diperiksa saat startup | Deactivate/Clear Data/respons server | Satu license |
| WebView2 | Resource/storage yang dikelola browser | `WebView2` | Kebijakan runtime | Hanya Clear Data | Dikelola runtime |
| RAM/file log aplikasi | 500 terakhir di RAM + file append | `nexaplay.log` | Tidak ada | Clear log eksplisit/Clear Data | RAM 500; disk tanpa batas |

Implementasi utama: `NexaPlay/Infrastructure/Services/CoverImageCacheService.cs:13-174`; `NexaPlay/Infrastructure/Services/ListingCoverResolver.cs:10-150`; `NexaPlay/Infrastructure/Services/SteamStoreService.cs:14-67`; `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:53-84`, `744-801`; `NexaPlay/Infrastructure/Logging/AppLogService.cs:7-49`.

## 14. Inventaris Model / DTO

### Model utama

| Model | Field penting | Dibuat oleh | Digunakan oleh | Penilaian |
| --- | --- | --- | --- | --- |
| `RuntimeCatalogEntry` | AppID, judul, harga, protection, capsule, header, genre | Parser metadata | Hanya pembangunan katalog | Bentuk internal ringan yang baik |
| `GameEntry` | Field listing ditambah pengembang/penerbit/deskripsi/rilis/helper media/placeholder mentah | MetadataService | Home, Games, dasar Detail, Library | Lebih lebar daripada card; objek listing runtime kanonis saat ini |
| `GameDetailEntry` | Deskripsi, bahasa, pengembang, penerbit, tangkapan layar, video, persyaratan, kategori, dukungan, pemberitahuan, harga, JSON mentah | SteamStoreService | Detail Game dan Bypass; fallback Library/cover | Tier detail tepat, tetapi JSON mentah menduplikasi data terurai di RAM |
| `FixEntry` | AppID/judul/publisher/kategori/poster/kredensial akun/premium/status/file instalasi | Parser Bypass dan pemetaan card | Recent Home, card Games, Bypass, Detail | Menggabungkan data domain Bypass dan DTO card listing generik |
| `NewGamesCatalogEntry` | Field listing ditambah deskripsi/release/beberapa artwork | Materializer R2 | Pembangunan metadata | Menduplikasi subset detail dan `GameEntry` |
| `GameFilterIndex` | AppID, nama ternormalisasi, harga, premium, Denuvo, kumpulan genre | GamesViewModel | Filter/pencarian Games | Duplikat RAM katalog penuh |
| `GameFilterIndexCacheItem` | Sama, array genre yang dapat diserialisasi | GamesViewModel | Cache disk indeks filter | DTO duplikat hanya untuk serialisasi |
| `InstalledGame` | AppID, nama, path, flag/tanggal applied | Pemuatan Library | Pemfilteran Library | State runtime Library yang tepat |
| `LibraryGameCard` | AppID, judul, poster, diterapkan, genre, premium, Denuvo | Mapper halaman terlihat Library | UI Library | DTO tampilan kecil; jalur hidrasinya mahal |

Definisi: `NexaPlay/Infrastructure/Services/MetadataService.cs:1647-1659`; `NexaPlay/Core/Models/GameEntry.cs:6-61`; `NexaPlay/Core/Models/GameDetailEntry.cs:8-68`; `NexaPlay/Core/Models/FixEntry.cs:6-39`; `NexaPlay/Core/Models/NewGamesCatalogModels.cs:3-24`; `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:695-709`; `NexaPlay/Core/Models/BypassProgressState.cs:26-32`; `NexaPlay/Presentation/ViewModels/LibraryViewModel.cs:18-30`.

### Matriks penggunaan field

Legenda: `R` dirender, `F` filter/pencarian/state, `D` hanya detail, `-` tidak diperlukan oleh surface tersebut.

| Field | Home | Games | Pencarian | Detail | Library | Bypass |
| --- | --- | --- | --- | --- | --- | --- |
| AppID | R/F | R/F | F | R/F | R/F | R/F |
| Nama/judul | R | R | F | R | R/F | R/F |
| Capsule/poster | R | R | - | R | R | R |
| Harga | F hanya untuk tier | F untuk tier/threshold default | F | R | F untuk tier | Flag tier source terpisah |
| Protection/Denuvo | R | R/F | F | R/aturan tindakan | R | Tidak dirender |
| Premium/Standard | R | R/F | F | R/aturan tindakan | R | R/F dari boolean source |
| Genre/kategori | Kategori terbaru | Hanya filter, bukan card | Hanya filter | R | R | R/badge/filter |
| Penerbit | Tidak dirender | Tidak dirender | Tidak dapat dicari | R | Tidak dirender | Hanya pencarian |
| Deskripsi | Placeholder untuk recent Home | - | - | R/WebView2 | - | Hanya detail |
| Tangkapan layar/video | - | - | - | R | - | Hanya detail |
| Persyaratan/bahasa/dukungan | - | - | - | R | - | Hanya detail |
| Kredensial/file | - | - | - | Entri fix bergantung tindakan | - | Detail/tindakan Bypass |

Temuan duplikat/terlalu besar:

- `FixEntry` digunakan ulang sebagai DTO card Games, membawa field akun/unduhan yang tidak relevan bagi Games.
- `GameFilterIndexCacheItem` menduplikasi `GameFilterIndex` hanya untuk menyerialisasi `HashSet` sebagai array.
- Home meng-clone seluruh katalog `GameEntry` ke dictionary terpisah.
- `GameDetailEntry` menyimpan nilai terurai dan JSON mentah lengkap secara bersamaan karena kode UI berikutnya membaca ulang path aset dari JSON mentah.
- `GameEntry.RawMetadataJson` dan `RawFieldPathCount` disalin oleh Home tetapi tidak pernah ditetapkan oleh pembangun `MetadataService` saat ini. Keduanya praktis tidak aktif dalam konstruksi katalog aktif. Sumber: deklarasi di `NexaPlay/Core/Models/GameEntry.cs:60-61`; satu-satunya penetapan ditemukan di `NexaPlay/Presentation/ViewModels/HomeViewModel.cs:480-481`, `619-620`.

## 15. Data Duplikat

Untuk AppID yang tersedia di semua domain, field identitas/listing yang sama dapat berada dalam:

1. `steam_data.json.gz`.
2. `steam_data.json`.
3. `override_data.json` jika di-override.
4. `nexaplay_override.json` jika di-override.
5. `new_games_catalog.json` untuk entri baru termaterialisasi.
6. `library_capsule.json.gz` untuk URL cover.
7. `fix_games.json`, `new_fix_games.json`, dan/atau `steam_games.json` untuk keanggotaan Bypass/protection.
8. `games_filter_index_cache_v3.json` untuk field pencarian/filter.
9. `metadata_detail/{appid}.json` untuk detail penuh dan aset.
10. Nama file gambar cover di bawah `cover_files`.
11. Dictionary katalog MetadataService di RAM.
12. Daftar filter Games di RAM.
13. Cache card Games di RAM setelah dilihat/di-prefetch.
14. Clone katalog penuh Home ditambah cache card populer di RAM.
15. Daftar source Bypass di RAM.
16. Model/card transien Library di RAM jika terinstal.

Tidak setiap AppID muncul di setiap lokasi, tetapi arsitektur mengizinkan representasi yang tumpang tindih ini.

Ringkasan duplikasi field:

| Field | Salinan persisten | Salinan RAM |
| --- | --- | --- |
| AppID | Semua JSON source relevan, indeks filter, nama file/isi detail, nama file `.lua` Steam | Katalog, Home, indeks/card Games, daftar Bypass, model Library |
| Nama | Bentuk ternormalisasi utama/override/new-game/detail/bypass/indeks-filter | Katalog, Home, indeks/card Games, Bypass, Library |
| URL capsule | Poster utama/override/indeks-cover/new-game/detail/bypass | Katalog, dictionary cover, cache resolver/card, card Home/Library/Bypass |
| Harga/tier | Indeks utama/override/new-game/detail/filter; premium eksplisit Bypass | Katalog, indeks Games, card |
| Protection | Utama/override ditambah keanggotaan turunan dalam tiga file bypass; indeks filter | Katalog dan indeks/card Games |

Duplikasi termahal yang teramati secara lokal adalah JSON indeks filter Games 26,2 MB di samping salinan gzip 25,4 MB dan salinan biasa 13,3 MB dari katalog utama. Byte gambar aktual menambah 44,4 MB tanpa eviction.

## 16. Diagram Arsitektur Saat Ini

```text
                            Repository raw GitHub
          +----------------------+----------------------+------------------+
          |                      |                      |                  |
  manifest katalog       8 source katalog      gzip indeks cover    override/tutorial
          |                      |                      |                  |
          +----------> MetadataService <-----+  GameCoverIndex      OverrideService
                                |             |          |                  |
                                | uraikan kedua|          +--------+---------+
                                | file steam   |                   |
                                v             |                   v
                       RAM Dictionary<AppID, GameEntry>     ListingCoverResolver
                         |          |          |                   |
             +-----------+          |          +---------+---------+
             |                      |                    |
             v                      v                    v
      Clone penuh Home      Indeks filter Games     resolusi cover card
      populer/terbaru       RAM + JSON 26 MB               |
             |                      |                     |
             +----------+-----------+                     |
                        |                                 |
                        v                                 v
                    card daftar                   CoverImageCacheService
                                                  cover_files pada disk

 Cloudflare R2 Metadata/{appid}.json
                    |
                    v
             SteamStoreService
        cache disk metadata_detail 7 hari
     uraikan GameDetailEntry + pertahankan JSON mentah
          |             |              |
          v             v              v
      Detail Game    Card Library    pengayaan cover daftar Bypass
                                     (request per item daftar)

 fix_games/new_fix/steam_games ----------------------+
          |                                          |
          v                                          v
 Daftar RAM BypassGamesDataService            Kumpulan protection MetadataService
          |
          +--> recent Home / halaman Bypass / kredensial dan file tindakan

 Steam config/stplug-in/{appid}.lua
          |
          +--> Keanggotaan Library (hanya AppID)
```

## 17. Masalah Performa

### KRITIS

1. **Daftar Bypass menyebabkan detail N+1 berskala seluruh katalog.** `LoadAsync()` memulai pengayaan untuk kedua daftar lengkap Bypass, dan `EnrichCoversInBackgroundAsync()` memanggil `SteamStoreService.GetDetailAsync()` tanpa syarat untuk setiap item. Cold cache dapat mengirim satu request R2 dan menulis satu JSON penuh per item. Sumber: `NexaPlay/Presentation/ViewModels/BypassGamesViewModel.cs:87-113`, `184-264`.

### TINGGI

2. **Startup terblokir oleh pekerjaan yang tidak terkait dengan render pertama Home.** Pembangunan katalog, parsing indeks cover penuh, pemuatan Home, serta pemuatan/persiapan card Games ditunggu sebelum overlay startup selesai. Sumber: `NexaPlay/MainWindow.xaml.cs:248-400`.
3. **Katalog utama terkompresi dan biasa sama-sama diurai dan digabungkan.** Saat kedua file lokal tersedia, klien membaca/dekompresi/mengurai keduanya alih-alih memperlakukan satu sebagai fallback. Total salinan lokal sekitar 36,8 MiB sebelum dekompresi/alokasi objek. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:695-708`.
4. **File bypass yang sama memiliki dua pemilik sinkronisasi/parsing.** `MetadataService` mengunduh/mengurainya untuk menurunkan protection, lalu `BypassGamesDataService` dapat mengunduh/mengurainya lagi memakai logika freshness 24 jam yang independen; Muat Games memanggil kedua jalur. Sumber: `NexaPlay/Infrastructure/Services/MetadataService.cs:354-394`, `743-753`; `NexaPlay/Infrastructure/Services/BypassGamesDataService.cs:107-238`; `NexaPlay/Presentation/ViewModels/SettingsViewModel.cs:279-307`.
5. **Games menduplikasi seluruh data pencarian/filter dan mengurutkan acak setiap kumpulan hasil.** Katalog penuh menjadi indeks RAM kedua dan JSON disk 26,2 MB. Setiap query/filter memindai seluruh baris dan `OrderBy(random.Next())` mengurutkan kecocokan. Sumber: `NexaPlay/Presentation/ViewModels/GamesViewModel.cs:141-166`, `538-602`, `695-709`, `744-801`.

### SEDANG

6. **Library menjalankan lookup detail penuh untuk setiap card yang terlihat.** Hingga sepuluh pembacaan/request detail berurutan terjadi setelah library sudah memperoleh metadata dasar. Sumber: `NexaPlay/Presentation/ViewModels/LibraryViewModel.cs:135-194`.
7. **Home menduplikasi dictionary katalog lengkap.** `GetCatalogSnapshotAsync()` dikonversi menjadi entri hasil clone dalam dictionary kedua. Sumber: `NexaPlay/Presentation/ViewModels/HomeViewModel.cs:441-450`.
8. **Detail penuh secara konseptual disimpan dan ditahan dua kali.** Field `GameDetailEntry` terurai berada bersama `RawMetadataJson`, dan UI mengurai ulang aset mentah. Sumber: `NexaPlay/Infrastructure/Services/SteamStoreService.cs:102-145`; `NexaPlay/Presentation/ViewModels/GameDetailViewModel.cs:311-326`.
9. **Cache gambar cover dan cache detail tidak memiliki batas.** Cache cover tidak memiliki TTL, batas byte, atau LRU. Detail memiliki TTL tetapi tidak memiliki eviction ukuran/jumlah. Sumber: `NexaPlay/Infrastructure/Services/CoverImageCacheService.cs:38-174`; `NexaPlay/Infrastructure/Services/SteamStoreService.cs:14-67`.
10. **Bypass merender/memfilter-mengurutkan daftar lengkap tanpa paging dan mencari pada setiap ketikan.** Sumber: `NexaPlay/Presentation/ViewModels/BypassGamesViewModel.cs:115-177`, `281-283`.

### RENDAH

11. **Surface API katalog mati.** `MetadataService.SearchAsync()` dan `GetAllCatalogAppIdsAsync()` tidak memiliki pemanggil aplikasi. Sumber: `NexaPlay/Contracts/Services/IMetadataService.cs:10-12`; `NexaPlay/Infrastructure/Services/MetadataService.cs:118-133`.
12. **Konstanta/field legacy tidak aktif menambah ambiguitas.** `github_raw_full.json`, `user_override_data.json`, dan katalog `RawMetadataJson`/`RawFieldPathCount` tidak berpartisipasi dalam alur builder aktif saat ini. Sumber: `NexaPlay/Core/Constants/AppConstants.cs:46`, `56`; `NexaPlay/Core/Models/GameEntry.cs:60-61`.
13. **Pertumbuhan WebView2 dan log berada di luar Clear Cache.** WebView2 yang teramati berukuran 30,5 MiB; file log terus ditambahkan tanpa batas disk. Sumber: `NexaPlay/Presentation/Helpers/WebView2EnvironmentHelper.cs:12-19`; `NexaPlay/Infrastructure/Logging/AppLogService.cs:26-48`.

## 18. Arsitektur Target yang Direkomendasikan

### Bentuk yang direkomendasikan

```text
Metadata mentah penuh + override + klasifikasi bypass
                         |
                         v
                Pipeline build/validasi
                         |
          +--------------+----------------+
          |                               |
          v                               v
 SQLite ringan berversi             JSON detail penuh per AppID
 AppID/nama/capsule/harga/         deskripsi/media/persyaratan/
 tier/protection/genre/status      dukungan/field mentah besar
          |                               |
   +------+------+------+                 |
   |      |      |      |                 |
 Home   Games  Pencarian  Library/Bypass   |
   |      |      |      |                 |
   +------+------+-klik AppID--------------+
```

Tanggung jawab yang direkomendasikan:

1. **Home:** feed AppID popular/new kecil, lalu satu query SQLite memakai `WHERE appid IN (...)`. Tanpa clone katalog penuh. Pertahankan prefetch gambar terbatas.
2. **Games:** paging SQLite dengan urutan deterministik. Pilih hanya kolom halaman yang terlihat.
3. **Pencarian:** query SQLite terindeks atas nama ternormalisasi dan AppID. Tambahkan FTS hanya jika kebutuhan substring/fuzzy membenarkannya; pencarian prefix/tepat terindeks dasar mungkin cukup sebagai langkah awal.
4. **Library:** pertahankan hanya kebenaran AppID dalam `.lua` Steam; resolve AppID yang terlihat secara batch dari SQLite. Jangan mengambil detail untuk artwork card.
5. **Bypass:** normalisasi source Bypass saat ini selama build menjadi satu tabel listing/status atau tabel SQLite sidecar dengan key AppID. Pisahkan payload sensitif/tindakan dari field listing generik.
6. **Detail:** GET `metadata/{appid}.json` penuh hanya saat masuk ke Detail Game atau Detail Bypass yang membutuhkan konten kaya. Fallback cover tidak boleh mengambil detail penuh.
7. **`steam_data.json.gz`:** proses selama pipeline build/release, bukan diurai sebagai source runtime klien kedua. Distribusikan satu artefak katalog siap-klien.
8. **Status:** prakomputasikan `protection`, `tier`, dan flag source/kategori final setelah override. Klien harus menampilkan field final tersimpan, bukan mereproduksi kebijakan merge di berbagai fitur.
9. **Cache:** pertahankan TTL JSON detail, tambahkan penulisan atomik dan kebijakan LRU/ukuran terbatas untuk gambar/detail. Simpan validator HTTP dalam state manifest/versi katalog.
10. **Update:** publikasikan artefak katalog berversi serta manifest yang memuat schema, revision, ukuran, SHA-256, dan URL delta opsional. Unduh ke temp, validasi, lalu tukar secara atomik.

Ini mempertahankan perilaku desktop offline, menghapus ketergantungan server dari pencarian, dan mencegah metadata penuh bocor ke jalur card daftar.

## 19. Keputusan JSON vs SQLite

| Opsi | Kekuatan | Kelemahan pada NexaPlay saat ini | Putusan |
| --- | --- | --- | --- |
| A. Katalog JSON ringan | Mudah dipublikasikan/debug; gzip terkompresi baik | Tetap membutuhkan parsing/object graph penuh untuk ~150 ribu baris; paging/filtering tetap di RAM | Format build/ekspor baik, lemah sebagai satu-satunya penyimpanan runtime |
| B. JSON terpaginasi | Unduhan halaman dan RAM lebih kecil | Multi-filter/pencarian lokal membutuhkan banyak file atau koordinasi server; kompleksitas cache/versi meningkat | Kurang cocok untuk pencarian seluruh katalog secara offline |
| C. Katalog SQLite lokal | Pencarian/filter/halaman terindeks; kolom selektif; satu penyimpanan kanonis; penggantian atomik | Membutuhkan pipeline build dan migrasi schema | Penyimpanan runtime listing/pencarian terbaik |
| D. Pencarian server/API | Katalog klien minimal, data live | Menambah latensi, ketergantungan availability, biaya operasi, dan perilaku offline lemah | Kemampuan masa depan opsional, bukan utama |
| E. Hybrid SQLite + JSON detail | Daftar/pencarian efisien ditambah detail kaya lazy yang fleksibel; cocok dengan konsep dua-tier saat ini | Dua siklus hidup cache harus dikelola | Kecocokan keseluruhan terbaik |

Keputusan: **E. Hybrid SQLite + metadata JSON per AppID**.

Alasan:

- ~150.000 game membutuhkan pemfilteran terindeks, paging deterministik, dan pembacaan selektif.
- Aplikasi desktop Windows diuntungkan oleh katalog/pencarian offline.
- Pemisahan detail R2 per AppID yang tersedia sudah berguna dan harus tetap lazy.
- Home, Games, Pencarian, Library, dan Bypass berbagi kolom listing/status kecil yang sama.
- Deskripsi/media/persyaratan penuh lebih sering berubah bentuk dan tidak seharusnya masuk ke setiap query card.

Skema listing minimal yang disarankan, tunduk pada peninjauan implementasi berikutnya:

```text
games(
  appid INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  normalized_name TEXT NOT NULL,
  library_capsule TEXT,
  header TEXT,
  price_normalized INTEGER NOT NULL,
  currency TEXT,
  tier INTEGER NOT NULL,
  protection INTEGER NOT NULL,
  genre TEXT,
  bypass_category INTEGER,
  bypass_available INTEGER NOT NULL,
  catalog_revision TEXT NOT NULL
)
```

Jauhkan deskripsi besar, screenshot, movie, persyaratan, field dukungan, DLC/package, dan metadata mentah dari tabel ini kecuali kebutuhan daftar/pencarian yang terbukti memerlukannya.

## 20. Rencana Migrasi yang Diusulkan

1. Catat metrik baseline: durasi cold/warm startup, peak working set, waktu parsing katalog, jumlah request R2 Bypass, latensi pencarian, dan pertumbuhan cache.
2. Definisikan kontrak listing/status kanonis dan presedensi merge yang tepat. Sertakan perilaku override-last saat ini dan rekonsiliasikan secara eksplisit tier katalog dengan boolean `premium` Bypass.
3. Bangun artefak katalog secara offline dari data Steam mentah, override, indeks cover, dan klasifikasi Bypass. Validasi AppID unik, jumlah baris, versi schema, hash, dan field wajib.
4. Publikasikan SQLite di samping sumber JSON saat ini melalui manifest yang tersedia. Klien mengunduh, memvalidasi SHA-256, membuka secara baca-saja, dan menukar revisi secara atomik.
5. Alihkan Games dan Pencarian lebih dahulu. Keduanya mendapat manfaat terbesar dari paginasi terindeks dan memungkinkan pemeriksaan parity langsung terhadap hasil filter saat ini.
6. Alihkan Home ke feed AppID ditambah resolusi SQLite batch. Hapus salinan katalog penuh Home hanya setelah parity output terbukti.
7. Alihkan card Library ke pembacaan katalog batch dan hapus pemanggilan detail dari hidrasi card.
8. Alihkan listing Bypass ke baris katalog/status ternormalisasi. Simpan metadata kredensial/file unduhan dalam payload Bypass yang berscope sempit, bukan baris game generik.
9. Jadikan request detail penuh eksklusif untuk halaman detail. Tambahkan endpoint/indeks cover ringan hanya jika data cover SQLite tidak dapat mencakup setiap card.
10. Tambahkan eviction gambar/detail terbatas dan buat semantik Clear Cache eksplisit untuk katalog, gambar, detail, WebView2, dan validator HTTP.
11. Jalankan telemetri/parity berdampingan untuk setidaknya satu revision katalog. Bandingkan jumlah, judul, cover, premium, protection, kategori, dan hasil pencarian.
12. Hapus parsing JSON legacy, cache duplikat, metode API mati, dan konstanta tak terpakai hanya setelah kesetaraan dan jendela rollback selesai.

Aturan rollback: pertahankan jalur JSON saat ini agar dapat dibaca selama satu rilis ketika SQLite diperkenalkan. Pertukaran artefak harus atomik; validasi yang gagal harus mempertahankan katalog terakhir yang diketahui baik.

## Langkah Berikutnya yang Direkomendasikan

Tinjau dan setujui dua keputusan sebelum perubahan kode apa pun:

1. Semantik kanonis final untuk `tier` saat premium turunan harga katalog berbeda dengan `premium` eksplisit Bypass.
2. Apakah payload kredensial/unduhan Bypass tetap berupa JSON berversi terpisah atau dipindahkan ke penyimpanan terenkripsi/terkontrol yang terpisah.

Setelah disetujui, pekerjaan berikutnya harus berupa benchmark baca-saja dan spesifikasi skema/kesetaraan, bukan penghapusan langsung sumber saat ini.

# Audit Phase 2 — Cache, Games, Refresh

Tanggal audit: 2026-09-14 17:06 +07:00. Authority bagian ini adalah source aktual post-SQLite dan snapshot baca-saja workstation. Tidak ada source/runtime/schema yang diubah. Angka cache adalah snapshot sesaat dan dapat bertambah saat aplikasi dipakai.

## 1. Cache Classification

Status klasifikasi target: **PARTIALLY CORRECT**. `catalog.sqlite`, lima dynamic feed, cache gambar/detail, dan state terpisah sesuai. Pengecualian utama: `library_capsule.json.gz` masih aktif sebagai cache listing prioritas di atas capsule SQLite, dan `catalog_runtime_overrides` adalah data turunan dinamis yang disimpan di dalam DB canonical.

| Nama | Status/kelas | Path | Creator/writer | Reader/consumer | TTL/cap | Invalidasi/refill | Clear Cache / Clear Data |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `catalog.sqlite` | [VERIFIED CURRENT] canonical general data | `%LOCALAPPDATA%\NexaPlay\Data\catalog.sqlite` | seed bootstrap + patch transaction | repository, Home, Games, Search, Library, Detail base | Tidak ada TTL/cap | Manifest/patch setiap launch dan Load Games | Dipertahankan / dihapus lalu bootstrap seed |
| `catalog_runtime_overrides` | [VERIFIED CURRENT] derived runtime data di DB | tabel dalam `catalog.sqlite` | `MetadataService.RefreshCatalogOverlaysAsync()` | seluruh effective SQLite query | Dibangun ulang penuh; tanpa TTL | warmup, Load Games, override/feed refresh | Dipertahankan / hilang bersama DB |
| `fix_games.json` | [VERIFIED CURRENT] dynamic snapshot + user-action payload | `runtime_catalog_sources` | `BypassGamesDataService` | Home, Bypass, Detail, protection overlay | 24 jam pada owner Bypass; tanpa cap | full GET jika stale/missing | Dipertahankan / dihapus |
| `new_fix_games.json` | [VERIFIED CURRENT] dynamic snapshot | `runtime_catalog_sources` | `MetadataService` dan `BypassGamesDataService` | Home Recent, fallback Bypass detail, protection | ETag path Metadata; 24 jam path Bypass | conditional GET plus owner kedua | Dipertahankan / dihapus |
| `steam_games.json` | [VERIFIED CURRENT] dynamic snapshot + credentials | `runtime_catalog_sources` | `BypassGamesDataService` | Steam Sharing, Home lookup, Bypass detail, protection | 24 jam; tanpa ETag/cap | full GET jika stale/missing | Dipertahankan / dihapus |
| `appid_populer.json` | [VERIFIED CURRENT] dynamic ordered-ID snapshot | `runtime_catalog_sources` | `MetadataService` | Home Popular | Tidak ada cooldown; ETag | local-first lalu background conditional GET | Dipertahankan / dihapus |
| `nexaplay_override.json` | [VERIFIED CURRENT] dynamic sparse override | `runtime_catalog_sources` | `NexaPlayOverrideService` | SQLite overlay, detail, cover resolver | sekali per startup session; ETag | HEAD lalu conditional/full GET | Dipertahankan / dihapus |
| `library_capsule.json.gz` | [LEGACY STILL ACTIVE] disposable listing index | `runtime_catalog_sources` | `GameCoverIndexService` | `ListingCoverResolver` untuk Home/Games | Tidak ada startup TTL check jika file usable; tanpa cap | missing/Load Games; ETag HEAD/GET | Dihapus / dihapus |
| `cover_files` | [VERIFIED CURRENT] disposable image cache | `runtime_catalog_sources\cover_files` | `CoverImageCacheService` | Home Popular dan Games | Tidak ada TTL/LRU/byte/count cap | visible page + prefetch | Tombstone + async delete / dihapus |
| `metadata_detail` | [VERIFIED CURRENT] disposable detail cache | `%LOCALAPPDATA%\NexaPlay\metadata_detail` | `SteamStoreService` | detail, Home Recent, listing fallback, Bypass listing | freshness 7 hari; tanpa eviction/cap | per AppID | Tombstone + async delete / dihapus |
| Home RAM | [VERIFIED CURRENT] transient derived cache | process | `HomeViewModel` | Home | feed maksimum 256; prefetch batch 12-24 | generation/reload | Dikosongkan / process exit |
| Games RAM | [VERIFIED CURRENT] transient card cache | process | `GamesViewModel` | Games | tumbuh per visited/prefetched AppID | generation/reload | card cache dibersihkan; visible collection tetap / process exit |
| Bypass RAM | [VERIFIED CURRENT] parsed dynamic lists | process | `BypassGamesDataService` | Home/Bypass/Detail | 24 jam | service refresh/restart | Tidak dibersihkan / process exit |
| cover resolver RAM | [VERIFIED CURRENT] URL success + in-flight | process | `ListingCoverResolver` | Home/Games | 512 success; R2 gate 4 | generation/explicit clear | Dikosongkan / process exit |
| tutorial JSON/ETag | [VERIFIED CURRENT] dynamic snapshot/state | app-data root | `BypassTutorialVideoService` | Bypass Detail | minimum refresh 2 menit | conditional GET | Dipertahankan / dihapus |
| app update state/download | [VERIFIED CURRENT] state + transient installer | root + `updates` | `AppUpdateService` | startup/settings | cooldown 6 jam | force/version change | Dipertahankan / dihapus |
| license/device/applied/managed/report | [VERIFIED CURRENT] state/user data | app-data root | persistence services | startup/actions | domain-specific | explicit actions | Dipertahankan / dihapus |
| WebView2 | [VERIFIED CURRENT] browser cache/state | `%LOCALAPPDATA%\NexaPlay\WebView2` | WebView2 runtime | detail/tutorial | runtime-managed | runtime policy | Dipertahankan / dihapus |
| log | [VERIFIED CURRENT] operational state | `nexaplay.log` | `AppLogService` | diagnostics | RAM 500; disk tanpa cap | explicit log clear/Clear Data | Dipertahankan / dihapus |
| `games_filter_index_cache_v3.json` | [RETIRED] legacy cache | `runtime_catalog_sources` | tidak ada writer aktif | tidak ada reader aktif | N/A | bootstrap/Metadata Clear menghapus sisa | Dihapus / dihapus |
| `steam_data*`, `override_data`, `new_games*` | [RETIRED] general-listing artifacts | tidak ada pada snapshot runtime | helper/test legacy saja | tidak reachable dari runtime aktif | N/A | tidak direfill | sisa hanya hilang via Clear Data/manual migration cleanup |

## 2. Cache Size Snapshot

Physical code-resolved root: `\\?\C:\Users\ADMIN\AppData\Local\NexaPlay` (`C:\Users\ADMIN\AppData\Local\NexaPlay` pada tool filesystem).

| Area | Files | Bytes | MiB | Min | Average | Median | Max | Oldest UTC | Newest UTC |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |
| Seluruh root | 710 | 232,216,120 | 221.459 | 0 | 327,065 | 28,897 | 170,987,520 | 2001-01-01* | 2026-09-14 09:12 |
| `Data` | 3 | 171,020,288 | 163.098 | 0 | 57,006,763 | 32,768 | 170,987,520 | 2026-09-14 09:09 | 2026-09-14 09:16 |
| `catalog.sqlite` | 1 | 170,987,520 | 163.066 | 170,987,520 | 170,987,520 | 170,987,520 | 170,987,520 | 2026-09-14 09:16 | sama |
| `runtime_catalog_sources` | 242 | 24,811,892 | 23.662 | 66 | 102,529 | 61,971 | 3,175,970 | 2026-09-14 09:09 | 2026-09-14 09:09 |
| `cover_files` | 232 | 21,509,936 | 20.514 | 17,392 | 92,715 | 62,468 | 1,330,004 | 2026-09-14 09:09:02 | 2026-09-14 09:09:53 |
| `metadata_detail` | 170 | 4,539,964 | 4.330 | 1,657 | 26,706 | 24,475 | 108,451 | 2026-09-14 09:09:12 | 2026-09-14 09:12:18 |
| `WebView2` | 289 | 31,824,962 | 30.351 | 0 | 110,121 | 706 | 4,202,496 | 2001-01-01* | 2026-09-14 09:12 |
| `updates` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | - | - |

`*` Timestamp 2001 berasal dari file runtime WebView2, bukan waktu pembuatan root. Semua 170 detail berumur kurang dari atau sama dengan 7 hari pada snapshot. Dynamic files: `library_capsule.json.gz` 3,175,970 B; `fix_games.json` 112,858 B; `steam_games.json` 6,473 B; `appid_populer.json` 3,289 B; `nexaplay_override.json` 2,947 B; `new_fix_games.json` 155 B. Legacy filter/source besar tidak ditemukan. Dua sidecar sisa `catalog.sqlite.bootstrap-shm` 32,768 B dan `catalog.sqlite.bootstrap-wal` 0 B masih ada.

## 3. Clear Cache Current Flow

[VERIFIED CURRENT]

```text
SettingsPage.OnClearCacheClicked
-> SettingsViewModel.ClearMetadataCacheAsync
   -> MetadataService.ClearCacheAsync
      -> reload popular/new-fix RAM dari disk
      -> delete legacy games_filter_index_cache_v3.json
   -> GameCoverIndexService.ClearCacheAsync
      -> _coverIndex = null
      -> delete library_capsule.json.gz dan library_capsule.json
      -> ETag files tetap ada
   -> CoverImageCacheService.ClearCacheAsync
      -> clear in-flight dictionary
      -> move cover_files ke tombstone
      -> async recursive delete tombstone
      -> recreate cover_files
   -> SteamStoreService.ClearCacheAsync
      -> move metadata_detail ke tombstone
      -> async recursive delete tombstone
      -> recreate metadata_detail
   -> HomeViewModel.InvalidateDerivedCache
   -> GamesViewModel.InvalidateDerivedCache
   -> CatalogRefreshState.Advance
   -> ListingCoverResolver.ClearCache
```

## 4. Clear Cache Rebuild/Network Effects

| Pertanyaan | Current answer |
| --- | --- |
| `catalog.sqlite` usable | [VERIFIED CURRENT] Ya; file dan patch/version state tidak disentuh. |
| Games/Search/Library perlu catalog rebuild | [VERIFIED CURRENT] Tidak; query SQLite langsung usable. |
| Home perlu reload | [VERIFIED CURRENT] Ya pada load Home berikut karena collection/ID/cache Home dikosongkan dan generation berubah. |
| Games reload langsung | [PROBLEM] Tidak. Card cache dibersihkan tetapi `Games` collection tidak; handler Clear Cache tidak memanggil reload, dan `GamesPage.OnNavigatedTo` dapat melewati `LoadAsync()` saat collection masih berisi item. Poster path lama dapat menunjuk file yang sudah dihapus. |
| Network langsung di handler | [VERIFIED CURRENT] Tidak. |
| Network tidak langsung | [VERIFIED CURRENT] Ya: next cover-index consumer melakukan full GET gzip karena file hilang; Home cached feed memicu conditional GET; Home Recent dapat refill detail; visible/prefetched Home/Games refill image bytes; Bypass open dapat refill detail massal. |
| Metadata detail refill massal otomatis | [PARTIAL] Bukan seluruh katalog saat clear. Startup/Home maksimal 12 recent; Bypass listing berikutnya berpotensi 178 calls. |
| ETag/WebView2/dynamic feeds | [VERIFIED CURRENT] Dipertahankan. |

App tetap terbuka: tidak ada reload/network sinkron dari handler. Restart setelah clear: SQLite bootstrap hanya memvalidasi DB, tetapi startup menunggu full download/parse `library_capsule.json.gz`, Home Recent detail, Home/Games visible cover refill, dan page prefetch background.

[PROBLEM] Clear memakai tombstone async tanpa cancellation token untuk request aktif. Download/detail yang sudah berjalan dapat selesai setelah clear dan mengisi direktori baru. Kegagalan delete tombstone ditelan.

## 5. Clear Data Current Flow

[VERIFIED CURRENT]

```text
SettingsPage.OnClearDataClicked + confirmation
-> SettingsViewModel.ClearAllDataAndRestartAsync
-> write %TEMP%\nexaplay-clear-data-{guid}.cmd
-> launch hidden script
-> Application.Current.Exit()
script: wait 2s -> retry rmdir /s /q %LOCALAPPDATA%\NexaPlay until gone
-> restart same EXE -> self-delete script
```

Semua app-data terhapus: DB, feeds, ETags, covers, detail, WebView2, license/device, applied/managed/report state, tutorial, update state/downloads, logs. Steam `config\stplug-in\*.lua`, Steam manifests, game install files, dan `%LOCALAPPDATA%\GameHub` tidak berada di target dan tetap ada. Restart bootstrap `catalog.sqlite` dari bundled seed secara offline. Network bukan syarat catalog base, tetapi Home dynamic feeds/cover index mencoba network bila cache kosong. `LicenseStore` dapat memulihkan dan memigrasikan `%LOCALAPPDATA%\GameHub\license.dat`; tanpa legacy license, user kembali ke activation flow.

## 6. catalog.sqlite Persistence

[VERIFIED CURRENT] DB canonical dipertahankan oleh Clear Cache/Load Games. Runtime version `2`, schema `1`, `163,262` games, premium threshold metadata `135000`. Manifest tidak disimpan; patch version berada di `catalog_meta`. Patch diterapkan transactionally. Clear Data menghapus DB, lalu seed package men-bootstrap ulang tanpa full catalog download.

## 7. cover_files

[VERIFIED CURRENT] Key/path: `{appid}_{sha1(absolute URL).lower()}{extension}`, extension fallback `.img`. File nonzero dianggap usable; tidak ada decode/content validation. Download gate global 6, in-flight dedup by local path, timeout 60 detik, satu GET tanpa retry, temp file + flush + atomic move. Error/timeout mengembalikan URL remote. Tidak ada TTL/LRU/size/count cap. Direct consumers hanya Home Popular dan Games; Home Recent, Library, Bypass, dan Detail tidak memakai service ini. Prefetch Games/Home dapat mengunduh actual bytes di background.

## 8. metadata_detail

[VERIFIED CURRENT] Path `{appid}.json`, TTL freshness 7 hari dari `LastWriteTimeUtc`, HTTP timeout 30 detik. Fresh file dibaca dan diparse setiap call; tidak ada parsed-memory cache, concurrency gate, atau in-flight dedup. Network write memakai `File.WriteAllTextAsync` langsung, bukan temp/atomic move. Fresh corrupt nonblank JSON menghasilkan `null` tanpa network repair. `BuildMergedMetadataJsonAsync` mengubah HTTP failure menjadi `(empty, transient=true)`; caller lalu mengembalikan `null`, sehingga stale cache tidak dipakai pada jalur transient tersebut kecuali exception terjadi di luar helper.

| Caller | Klasifikasi | Reachability |
| --- | --- | --- |
| `GameDetailViewModel.LoadAsync` | ALLOWED REAL DETAIL | click Games/Home/Library general detail |
| `BypassGameDetailViewModel.LoadAsync` | ALLOWED REAL DETAIL | click Bypass detail |
| `HomeViewModel.EnrichRecentFixesHeroCoverAsync` | LISTING/FALLBACK | maksimal 12, skip hanya jika hero override ada |
| `ListingCoverResolver.GetR2CapsuleAsync` | LISTING/FALLBACK | Home Popular/Games saat override, index, dan SQLite capsule kosong |
| `BypassGamesViewModel.EnrichCoversInBackgroundAsync` | LISTING/N+1 | unconditional untuk seluruh fix + steam lists |
| Library | VERIFIED NO CALLER | card memakai batch SQLite saja |

## 9. library_capsule Runtime Dependencies

[LEGACY STILL ACTIVE] Runtime chain: DI singleton -> `MainWindow.WarmupMetadataStartupAsync` -> `GameCoverIndexService`; Settings Load Games refresh; Clear Cache delete; `ListingCoverResolver` reads index before SQLite runtime capsule. Home Popular dan Games adalah downstream consumers. Tidak ada Library consumer.

Snapshot parity: index 141,309 AppIDs; SQLite has capsule for 155,892. Semua index AppID ada di SQLite; 141,307 URL identical, 2 differ (`3159330`, `3405690`). SQLite also has 14,583 capsule rows not in index. Retirement later: **PARTIAL/near-YES**, blocked by acceptance/testing of two URL changes plus removal of startup, Settings, resolver, DI, constants, validation helpers, and self-check references. Jangan retire pada audit ini.

## 10. Home Popular Current Flow

[VERIFIED CURRENT]

```text
appid_populer.json (256 IDs snapshot)
-> MetadataService local RAM/disk; cached path starts background ETag GET
-> GetMetadataBatchAsync -> SQLite effective rows, preserving requested order
-> cached NexaPlay non-SQL fields
-> ListingCoverResolver: catalog override -> library_capsule index -> SQLite capsule -> Metadata detail fallback -> header
-> CoverImageCacheService actual image bytes
-> UI
```

Tidak ada full-catalog clone. Initial page `columns * 8` = 24-48 cards. Load More `columns * 4` = 12-24. Prefetch tepat next Load More batch. Feed parser memakai `HashSet<int>` lalu `ToList`; urutan observed mengikuti traversal, tetapi preservation bukan kontrak eksplisit collection.

## 11. Home Recent Current Flow

[VERIFIED CURRENT] `new_fix_games.json` menghasilkan AppID, `Take(12)`. Setiap AppID mendapat SQLite base lookup dan optional Bypass row. Bypass lookup precedence saat build Home: `new_fix` lalu ditimpa `fix`, lalu ditimpa `steam`. Hero enrichment concurrency 4. Jika catalog `LibraryHero2x` override ada, detail dilewati; selain itu `Metadata/{appid}.json` selalu dipanggil, termasuk saat capsule override sudah ada. Detail cache 7 hari dipakai. Home startup menunggu seluruh recent enrichment. `CoverImageCacheService` tidak dipakai untuk hero recent.

## 12. Games SQLite Current Flow

[VERIFIED CURRENT] Count dan page memakai CTE `effective`: `games LEFT JOIN catalog_runtime_overrides`, lalu `COALESCE` per field. Count dan data query memakai `WHERE` identik dan parameterized. Data SQL:

```sql
WITH effective AS (...)
SELECT e.appid, e.name, e.normalized_name, e.library_capsule, e.genre,
       e.price_normalized, e.price_display, e.protection, e.tier, e.pc_requirements
FROM effective e
WHERE ...
ORDER BY ((e.appid * @orderSeed) % 2147483647), e.appid
LIMIT @limit OFFSET @offset;
```

No-search selalu menambah `e.price_normalized >= 100000`. Search exact integer memakai `e.appid = @searchAppId`; name memakai normalized substring. Tier/protection/genre digabung SQL-side. C# hanya membangun cards/covers setelah page rows kembali.

## 13. Games Default Ordering

[VERIFIED CURRENT] Bukan random per query/session. Urutan pseudo-random deterministic dari AppID dan seed:

```text
orderSeed = floor((UTC days since Unix epoch) / 2) + 1
ORDER BY ((appid * orderSeed) % 2147483647), appid
```

Seed snapshot `10356`. Semua page dalam bucket dua hari konsisten. Back -> Games mempertahankan collection/order singleton. Restart pada bucket sama menghasilkan order sama. Pergantian hari belum tentu mengubah urutan; hanya boundary dua hari UTC. Jika app tetap terbuka melewati boundary, visible collection tetap sampai query berikutnya.

## 14. Daily Rotation

[MISSING] **DAILY ROTATION: NOT IMPLEMENTED.** Yang ada adalah rotation dua-hari berbasis UTC, tanpa persisted seed dan tanpa generation event saat boundary. Perubahan terjadi pada query berikutnya, bukan timer/reload otomatis.

## 15. Default Price Floor

[VERIFIED CURRENT] Floor exact `100000` untuk semua query tanpa text search, termasuk query tier/protection/genre. Search menonaktifkan floor, sehingga exact AppID/name dapat menemukan item di bawah Rp100.000. Semua Standard di bawah Rp100.000 tersembunyi dari default/filter browsing; Standard Rp100.000-Rp134.999 tetap eligible.

## 16. Price Distribution/Mix

[VERIFIED CURRENT] Total 163,262; eligible default 19,327; below floor 143,935; free 68,545. Eligible: Premium 10,429 (53.96%), Standard 8,898 (46.04%); Denuvo 564 (2.92%), Non-Denuvo 18,763. Range eligible Rp100.000-Rp3.499.825, average Rp184.672. Tidak ada quota, band sampling, weighting, atau premium prioritization. Mix hanya akibat deterministic order. Page 1 size 60: 29 Premium/31 Standard, average Rp199.627. Page 2: 32/28, average Rp177.722.

## 17. Games Paging

[VERIFIED CURRENT] 10 rows visual per page; responsive 3-6 columns; page size 30, 40, 50, atau 60. Breakpoints `<680=3`, `680-879=4`, `880-1099=5`, `>=1100=6`. Startup default sebelum page layout adalah 5 columns/50. Resize debounce 220 ms, mempertahankan current page index dan vertical offset; tidak reset page ke 1. Offset berubah sesuai page size. Tidak ada SQLite page-result cache. Games card cache dipertahankan pada resize.

## 18. Games Prefetch

[VERIFIED CURRENT] Dua page berikut diprefetch dengan satu query `Limit = PageSize * 2`, maksimum 60-120 items. Prefetch membangun card model, resolve override/index/SQLite cover, download actual image bytes, dan dapat mengambil metadata detail hanya sebagai last fallback. Concurrency card prefetch 4.

[PARTIAL] Search/filter/page/resize akhirnya mengganti prefetch dan stale result tidak pernah masuk `Games` collection; stale work hanya dapat mengisi AppID card cache/disk image. Namun old prefetch baru dibatalkan setelah current page query/build selesai. Navigation away tidak membatalkan prefetch. Invalidation/Clear Cache membatalkan langsung.

## 19. Search

[VERIFIED CURRENT] Debounce 220 ms, minimum 3 karakter, cancellation token untuk apply terakhir, dan `ct.ThrowIfCancellationRequested()` sebelum UI sync. Exact numeric search: `e.appid = @searchAppId`. Name: `e.normalized_name LIKE '%' || @searchName || '%'`; normalization lowercases, removes diacritics/apostrophes, and collapses punctuation/space. Tidak ada FTS.

[PROBLEM] Query 1-2 karakter tidak mengeksekusi apply dan dapat meninggalkan hasil query sebelumnya di layar sampai input kosong atau mencapai 3 karakter. Leading wildcard memaksa scan normalized-name index.

## 20. Premium/Standard Filter

[VERIFIED CURRENT] UI mutually exclusive. Premium: `e.tier = 'premium'`; Standard: `e.tier = 'standard'`; neither/both: no tier predicate. Default price floor tetap aktif. Count/page parity exact. Stored SQLite tier authority; runtime price override dapat menghasilkan runtime tier threshold 135,000. Badge memakai `GameEntry.IsPremium` dari effective tier yang sama.

## 21. Denuvo/Non-Denuvo Filter

[VERIFIED CURRENT] UI menyediakan keduanya dan mutually exclusive. Denuvo: `e.protection = 1`; Non-Denuvo: `e.protection = 0`. Effective protection = SQLite base, forced true bagi membership union fix/new-fix/steam, lalu final `nexaplay_override` jika tersedia. Filter dan badge memakai effective value yang sama.

## 22. Genre Filter

[VERIFIED CURRENT] SQLite stores JSON string array. Predicate per selected/expanded alias: `e.genre LIKE @genreN`, value `%\"Genre\"%`. Multiple genre selections/aliases digabung `OR`; group digabung `AND` dengan search/tier/protection. UI list statis. Tidak ada genre index/JSON1 token table; cost adalah scan candidates.

## 23. Combined Filter

[VERIFIED CURRENT] Semua kombinasi diminta digabung SQL-side. Sample effective counts: Premium+Denuvo+Action 342; Premium+Non-Denuvo+Action 3,741; Standard+Denuvo+RPG 11; Standard+Non-Denuvo+RPG 2,112. Search + tier/protection/genre memakai AND yang sama; no post-query filtering.

## 24. SQLite Indexes

| Table | Key/index | Columns | Unique |
| --- | --- | --- | --- |
| `games` | INTEGER PRIMARY KEY | `appid` | Ya |
| `games` | `idx_games_normalized_name` | `normalized_name` | Tidak |
| `games` | `idx_games_price` | `price_normalized` | Tidak |
| `games` | `idx_games_protection` | `protection` | Tidak |
| `games` | `idx_games_tier` | `tier` | Tidak |
| `catalog_meta` | autoindex | `key` | Ya |
| `catalog_runtime_overrides` | INTEGER PRIMARY KEY | `appid` | Ya |

Tidak ada index genre atau composite filter/order index.

## 25. Query Plan

[VERIFIED CURRENT]

| Query | Representative plan |
| --- | --- |
| count eligible | scan covering `idx_games_price`; rowid lookup override per row |
| default page | scan covering price index + override PK lookup + temp B-tree ORDER BY |
| Premium/Standard/Denuvo/Genre | scan `idx_games_price` + override PK lookup + temp B-tree; tier/protection indexes tidak dipilih karena effective fields memakai `COALESCE` |
| exact AppID | INTEGER PRIMARY KEY lookup; O(log n)/rowid direct |
| normalized substring | full scan covering `idx_games_normalized_name` + override PK lookup + temp B-tree karena leading wildcard |

## 26. Query Benchmark

Read-only URI connection, runtime DB, warm runs, limit 60, repository-equivalent count + full selected columns. Timing workstation ini:

| Scenario | Count | Rows | Median ms | Observed min-max ms |
| --- | ---: | ---: | ---: | ---: |
| count all eligible only | 19,327 | 19,327 | 11.824 | 11.299-13.801 |
| default page 1 | 19,327 | 60 | 35.073 | 33.954-37.845 |
| default page 2 | 19,327 | 60 | 31.791 | 30.094-32.843 |
| Premium | 10,429 | 60 | 198.956 | 181.849-271.997 |
| Standard | 8,898 | 60 | 178.483 | 170.949-195.696 |
| Denuvo | 564 | 60 | 176.750 | 174.564-181.202 |
| Non-Denuvo | 18,763 | 60 | 177.882 | 174.169-183.314 |
| Genre Action | 7,359 | 60 | 180.171 | 176.246-207.597 |
| Premium + Denuvo + Action | 342 | 60 | 182.087 | 172.380-184.810 |
| Standard + Non-Denuvo + Action | 3,249 | 60 | 180.272 | 175.792-188.385 |
| exact AppID 570 | 1 | 1 | 0.041 | 0.041-0.053 |
| normalized name `counter strike` | 5 | 5 | 53.680 | 53.122-54.737 |

## 27. Library Current Flow

[VERIFIED CURRENT]

```text
Steam config\stplug-in\*.lua membership
-> one GetMetadataBatchAsync(all AppIDs)
-> SQLite effective rows dictionary
-> local search/paging 10 cards
-> title/capsule/header/genre/tier/protection
```

Tidak ada `GetDetailAsync`, per-card override request, `ListingCoverResolver`, `GameCoverIndexService`, atau `CoverImageCacheService`. Override listing sudah masuk effective SQLite/batch dan cached non-SQL fields. Library cover image tetap URL langsung, bukan `cover_files`.

## 28. Bypass Listing N+1

[PROBLEM] `LoadAsync()` memuat `fix_games` 160 dan `steam_games` 18, lalu selalu memulai enrichment kedua list penuh, terlepas tab aktif. Setiap item unconditional memanggil SQLite base, override lookup, dan `GetDetailAsync` sebelum memilih cover; existing source poster tidak mencegah detail call. Cold cache: 178 detail tasks, 172 unique AppIDs, 6 overlap antar-list yang dapat menjadi duplicate concurrent HTTP/file writes karena `SteamStoreService` tidak punya in-flight dedup. Masing-masing list punya gate 4, tetapi kedua enrichment berjalan bersamaan sehingga potential concurrency 8.

Poster priority setelah fetch: source `poster` -> NexaPlay capsule override -> detail capsule -> SGDB grid -> SQLite capsule -> SQLite header -> hardcoded Steam CDN. Premium/category/status/credentials/files tetap authority JSON Bypass. Listing tidak memakai `CoverImageCacheService`.

## 29. Bypass Detail

[VERIFIED CURRENT] Clicked `preferredBypassEntry` menang jika AppID sama. Tanpa preferred row, priority `steam_games` -> `fix_games` -> `new_fix_games`. Base metadata berasal SQLite. Full detail R2/cache selalu diminta untuk descriptions/media/requirements/support/price/assets. NexaPlay detail/catalog overrides diterapkan. Tutorial JSON/ETag diminta setelah detail, dengan 2-minute minimum refresh. Credentials/files/actions tetap dari Bypass entry.

## 30. Dynamic Feed Owners

| Source | Remote/local | Downloader/parser | Validation/atomicity | TTL/ETag | Startup/Load Games/consumer |
| --- | --- | --- | --- | --- | --- |
| `fix_games` | GitHub -> runtime source | Bypass service; Metadata parses protection only | raw write non-atomic; parser swallows malformed rows/file | 24h, no ETag | Home startup may GET if stale; Load Games owner refresh; Home/Bypass/Detail |
| `new_fix_games` | GitHub -> same file | Metadata + Bypass | Metadata validates nonempty and atomic; Bypass raw write non-atomic before parse | Metadata ETag/no cooldown; Bypass 24h | Home startup ETag; Load Games Metadata then Bypass; Recent/protection/detail fallback |
| `steam_games` | GitHub -> runtime source | Bypass service; Metadata parses protection only | raw write non-atomic; permissive parser | 24h, no ETag | Home lookup/BYPASS if stale; Load Games; Steam tab/protection/detail |
| `appid_populer` | GitHub -> runtime source | Metadata | nonempty validation + atomic replace | ETag, no cooldown | Home local-first/background every load; Load Games; Home Popular |
| `nexaplay_override` | GitHub -> runtime source | Override service | parse before atomic replace | startup-session check; HEAD + conditional GET | background each launch; Load Games; all listing/detail overrides |

## 31. Startup Background Refresh

| Source/operation | Timing | Blocking first usable Home | Rule/fallback |
| --- | --- | --- | --- |
| license local + remote validation | before navigation | BLOCKING | local encrypted load, then Supabase validation; fallback UI on failure |
| SQLite bootstrap/open + runtime overlay write | warmup | BLOCKING | local seed/runtime; network not required |
| cover index | warmup | BLOCKING | existing usable file parsed without revalidation; missing triggers network |
| `fix_games` | Home load | BLOCKING if stale/missing | 24h full GET; disk fallback |
| `new_fix_games` | Home load | local result immediate if cached; background network | conditional GET; missing path blocks fetch |
| `steam_games` | Home recent lookup | BLOCKING if stale/missing | 24h full GET; may be skipped if recent list empty |
| `appid_populer` | Home load | local immediate if cached; background network | conditional GET; missing path blocks fetch |
| Home Recent detail | Home load | BLOCKING | up to 12, concurrency 4 |
| Home Popular current batch covers | Home load | BLOCKING | SQLite/index plus actual image download |
| Games current page covers | preload | BLOCKING | count/page/card/actual image download |
| Home/Games prefetch | after current pages | FIRE-AND-FORGET | bounded batches |
| override refresh | after local warmup begins | FIRE-AND-FORGET | once each launch; event reload if changed |
| catalog manifest/patch | after startup overlay hidden | FIRE-AND-FORGET | full manifest GET every launch; patches only if newer |
| app update manifest | after Home ready | NON-BLOCKING to first usable Home | 6h state cooldown |
| managed Steam manifest restore | after navigation | FIRE-AND-FORGET | local only |

Latest five dynamic feeds dapat diterima tanpa Load Games: override/popular/new-fix each launch/Home; fix/steam saat consumer melihat file stale/missing (maksimal stale 24 jam). Cover index adalah exception: existing usable index tidak dicek saat startup dan memerlukan Load Games untuk revalidation.

## 32. Duplicate Network Requests

[PARTIAL] `new_fix_games` punya dua owner. Pada Load Games urutan Metadata lebih dulu menulis file baru, sehingga Bypass owner biasanya membaca disk fresh dan tidak melakukan GET kedua. Pada startup dengan cache stale, Metadata background ETag dan Bypass direct GET dapat overlap. `fix_games`/`steam_games` hanya didownload Bypass sekarang; Metadata tetap parser/overlay owner kedua, bukan downloader kedua.

[PROBLEM] Home reload di akhir Load Games memanggil cached `GetNewFixAppIdsAsync()` dan dapat memulai conditional GET tambahan setelah feed baru saja diperiksa. `CatalogUpdated` dari perubahan popular/new-fix juga memanggil refresh seluruh Bypass service sebelum Home/Games reload, walau popular tidak mengubah Bypass payload.

## 33. Load Games Current Flow

[VERIFIED CURRENT]

```text
SettingsPage.OnLoadGamesClicked
-> parallel:
   NexaPlayOverrideService.RefreshAsync
   GameCoverIndexService.RefreshAsync
-> MetadataService.RefreshDynamicSourcesAsync
   -> conditional GET appid_populer
   -> conditional GET new_fix_games
   -> parse fix/new-fix/steam protection
   -> replace runtime overrides transaction
-> BypassGamesDataService.RefreshAsync
   -> reset RAM; parse fresh files or full GET stale fix/new-fix/steam
-> MetadataService.RefreshAsync(false)
   -> reload small feed RAM from disk
   -> parse protection + replace runtime overrides again
-> CatalogUpdateService.CheckAndApplyPatchesAsync(true)
   -> full GET catalog manifest; GET/apply each missing patch
-> generation++ and listing resolver clear
-> forced Home reload
-> forced Games reload
```

Request classification:

| Request | Status/reachability |
| --- | --- |
| catalog manifest + patches | CURRENTLY REQUIRED; manifest always GET, patches conditional by version |
| `nexaplay_override` | CURRENTLY REQUIRED; HEAD/conditional GET |
| `appid_populer`, `new_fix_games` | CURRENTLY REQUIRED; conditional GET |
| `fix_games`, `steam_games` | CURRENTLY REQUIRED; GET only if 24h stale/missing |
| `library_capsule.json.gz` | LEGACY STILL ACTIVE; HEAD/conditional GET then full parse |
| `Metadata/{appid}` | CURRENTLY REACHABLE indirectly from forced Home Recent and cover fallback |
| actual cover image URLs | CURRENTLY REACHABLE for visible Home/Games; cache hits avoid GET |
| `steam_data*`, `override_data`, `new_games*`, full catalog SQLite | RETIRED/unreachable |
| app update manifest, YouTube tutorial | not called by Load Games |

`force` parameter pada `CatalogUpdateService` tidak mengubah behavior. Bypass `RefreshAsync` juga bukan true force download karena tetap menerima file <24 jam.

## 34. Load Games No-Change Cost

[PROBLEM] **WASTEFUL NO-CHANGE WORK** ketika semua remote unchanged:

1. Override HEAD; cover-index HEAD; popular/new-fix conditional GET; catalog manifest GET; optional Home-triggered second conditional feed GET.
2. Decompress/parse full 3.18 MB cover index menjadi 141,309-entry dictionary walau ETag sama.
3. Parse popular/new-fix disk after 304.
4. Parse fix/new-fix/steam and delete/reinsert runtime overrides twice.
5. Reset/reparse tiga Bypass RAM lists.
6. Advance generation walau catalog/feeds tidak berubah.
7. Clear Home/Games derived caches; scan Steam `.lua`; rerun SQLite counts/pages; rebuild visible card models; recheck disk covers/detail; start prefetch again.
8. Tidak ada full catalog materialization, catalog delete/bootstrap, cover/detail deletion, Library reload, atau Bypass ViewModel reload langsung.

## 35. Generation/Invalidation Map

| Event | Generation/state | Consequence |
| --- | --- | --- |
| catalog patch changed at startup | generation +1 | resolver clear; forced Home/Games reload |
| override changed background | `CatalogUpdated`; generation +1 | Bypass refresh; resolver clear; Home/Games reload |
| popular/new-fix changed background | `CatalogUpdated`; generation +1 | same broad reload, including Bypass refresh |
| fix/steam change during initial Home load | no generation needed before first render | later `_metadata.Refresh(false)` rebuilds overlays |
| standalone `RefreshFixDataAsync` | no generation/overlay refresh | [PROBLEM] Bypass updates, Games/Home protection may remain old |
| Clear Cache | generation +1 unconditionally | Home invalidated; Games cache invalidated but no immediate reload |
| Load Games | generation +1 unconditionally | forced Home/Games reload |
| Clear Data | process state reset to 0 | full app-data recreation/bootstrap |
| navigation | no generation | singleton Home/Games may reuse state |
| two-day UTC order boundary | no generation/timer | order changes only on next Games query |

## 36. Cache Eviction Findings

[DECISION REQUIRED] Analysis only:

- Covers sekarang 20.5 MiB/232 files, average 92.7 KiB. 100 MB memberi sekitar 5x snapshot dan reasonable sebagai conservative cap; 250 MB memberi long-browse headroom dan juga reasonable; 500 MB belum didukung measurements.
- Cover policy terbaik kemudian: hybrid LRU/last-access approximation + byte/count cap, optional long TTL. TTL-only tidak membatasi burst size.
- Detail sekarang 4.33 MiB/170 files, average 26.7 KiB. TTL 7 hari saja **tidak cukup** karena expired file yang tidak pernah dibuka tidak dihapus.
- Expired detail sebaiknya lazy-replaced saat access, ditambah threshold/interval cleanup untuk orphan expired files. Jangan block startup dengan eager full cleanup.
- Cleanup sebaiknya background hanya saat byte/count threshold atau maintenance interval terlampaui; bukan setiap startup tanpa guard.

## 37. Current vs Proposed Target Gap

### Clear Cache target

| Target | Status current | Gap |
| --- | --- | --- |
| clear `cover_files`, `metadata_detail`, RAM card/prefetch, legacy cache | CURRENT MATCH/PARTIAL | Home/Games/resolver cleared; Bypass/Library/static WebView height RAM not centrally cleared |
| preserve DB, patches/version, feeds, ETags, license/device/state, Steam lua, update, WebView2 | CURRENT MATCH | semua dipertahankan |
| no catalog rebuild/bootstrap/download feeds | CURRENT MATCH direct | indirect Home load checks feeds and missing cover index downloads |
| no generation change without data change | CURRENT MISMATCH | generation selalu naik |
| no forced reload | CURRENT MATCH direct but inconsistent | Home next-load forced by invalidation; Games may not reload and can retain dead paths |
| no Bypass metadata flood | CURRENT MISMATCH conditional | next Bypass open after detail clear dapat membuat 178 calls |

### Architecture target

| Area | Current | Target gap/risk |
| --- | --- | --- |
| General listing | SQLite -> runtime override -> UI | MATCH |
| Home Popular | feed -> SQLite batch -> override/index -> image cache; detail last fallback | PARTIAL: legacy index priority and detail fallback remain |
| Home Recent | new-fix -> SQLite/Bypass -> bounded detail | MATCH, tetapi blocks startup |
| Games | SQLite page/filter/search | MATCH; effective filter plans remain costly |
| Library | `.lua` -> SQLite batch | MATCH |
| Bypass listing | JSON -> unconditional detail all items | MAJOR MISMATCH/N+1 |
| Bypass detail | clicked/preferred or steam/fix/new -> SQLite + detail + override/tutorial | MATCH |
| library capsule index | active startup/Load Games dependency | RETIRE CANDIDATE; 2 URL parity decisions |
| dynamic feeds | mixed local-first, ETag, and 24h full GET | PARTIAL; owner/cooldown/atomicity inconsistent |
| Load Games | broad revalidate + rebuild/reload | PARTIAL; not clean force-only and wastes no-change work |

## 38. Recommended Next Implementation Steps

PROPOSED order only; no implementation in this audit:

1. Stop Bypass listing unconditional detail fetch; use source poster -> override -> SQLite cover -> CDN, keep detail on click.
2. Define lightweight Clear Cache semantics, cancel/guard in-flight writes, and guarantee visible Games/Home reload consistency without false catalog generation.
3. Choose one dynamic-feed download owner; make atomic validation and explicit force/revalidate semantics; remove duplicate overlay rebuild/no-change generation.
4. Retire `GameCoverIndexService` only after parity decision for AppIDs `3159330` and `3405690` and self-check update.
5. Optimize effective SQLite filter/search/order plan based on measured 176-199 ms filtered pages; preserve query parity before schema/index changes.
6. Add measured cover/detail eviction policy.
7. Re-evaluate startup Games preload, Home Recent blocking, and two-page prefetch after request/timing telemetry.

## 39. Risks

- [PROBLEM] Bypass cold listing: 178 detail calls/tasks, 172 unique IDs, possible duplicate writes.
- [PROBLEM] SteamStore detail writes non-atomic, lacks in-flight dedup, fresh corrupt cache is not repaired, transient failures can skip stale fallback.
- [PROBLEM] Bypass JSON writers are non-atomic and write before meaningful validation.
- [PROBLEM] Clear Cache can be repopulated by in-flight work and can leave Games cards pointing at deleted cover files.
- [PROBLEM] Clear Cache and Load Games advance catalog generation without catalog change.
- [PROBLEM] Filtered Games pages are roughly 176-199 ms warm on this workstation because effective `COALESCE` prevents tier/protection index selection and ordering uses temp B-tree.
- [PROBLEM] Load Games parses/replaces overlays twice and rebuilds cover index/reloads UI on no-change.
- [PROBLEM] Standalone fix refresh does not refresh SQLite protection overlays/generation.
- [PROBLEM] 1-2 character Games search can display prior-query results.
- [PROBLEM] Covers/detail/log disk growth is unbounded; expired detail is never globally collected.
- [PARTIAL] Prefetch cancellation is delayed and navigation does not cancel.
- [PARTIAL] Popular/recent order depends on `HashSet` enumeration rather than explicit ordered-list preservation contract.
- [PARTIAL] `catalog.sqlite.bootstrap-shm/-wal` leftovers remain after bootstrap.

## 40. Open Decisions

- [DECISION REQUIRED] Keep current two-day UTC rotation, change to daily, or use stable order.
- [DECISION REQUIRED] Desired default Premium/Standard mix; current natural mix is about 54/46, no quota.
- [DECISION REQUIRED] Keep two-page Games prefetch or reduce after telemetry.
- [DECISION REQUIRED] Accept SQLite cover differences for two AppIDs and retire library capsule index.
- [DECISION REQUIRED] Cover cap: 100 MB conservative versus 250 MB long-browse headroom; 500 MB not justified now.
- [DECISION REQUIRED] Detail byte/count cap and maintenance interval in addition to 7-day freshness.
- [DECISION REQUIRED] Whether Clear Cache should advance any catalog generation and which live ViewModels must reload immediately.
- [DECISION REQUIRED] Whether Load Games means force network download, conditional revalidation, or only catalog/feed check.
- [DECISION REQUIRED] Canonical downloader for shared `new_fix_games` and parser/overlay ownership for all three Bypass feeds.
- [DECISION REQUIRED] Whether GameHub legacy license restoration after Clear Data is intended factory-reset behavior.
- [DECISION REQUIRED] Whether WebView2/log cleanup belongs to separate controls, not Clear Cache.
- [DECISION REQUIRED] Search strategy after measurements: keep substring scan, add prefix path, or later FTS.
