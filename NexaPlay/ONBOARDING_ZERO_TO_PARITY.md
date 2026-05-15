# NexaPlay Onboarding - Zero to Feature Parity

Dokumen ini ditujukan untuk AI/engineer baru yang masuk dari nol.
Target akhirnya: semua fitur penting `gamehub` tersedia di `NexaPlay` dengan UI native WinUI 3.

## 0. Konteks Besar (Wajib)

- Sumber lama: `D:\My Project\gamehub\desktop\GameHubDesktop`
- Target baru: `D:\My Project\NexaPlay\NexaPlay`
- `gamehub` lama: WPF + WebView2 + UI web (`public/*.html/js`)
- `NexaPlay`: WinUI 3 native + service C#

Kenapa migrasi:
- menghilangkan bottleneck WebView + JS bridge + startup load tinggi.
- menjaga logic backend yang sudah matang, sambil rewrite UI native.

## 1. Aturan Main Keras

1. Jangan bawa kembali WebView/HTML/JS bridge ke `NexaPlay`.
2. Jangan edit banyak page sekaligus tanpa build di tengah.
3. Build gate adalah wajib setiap batch kecil.
4. Jika XAML rusak berantai, pulihkan ke valid minimal dulu.
5. Jaga boundary arsitektur:
   - `Contracts` hanya interface
   - `Infrastructure` implementasi teknis
   - `Presentation` UI + ViewModel

## 2. Langkah Pertama Saat Masuk Project

1. Baca:
   - [README.md](</D:/My Project/NexaPlay/NexaPlay/README.md>)
   - [AGENTS.md](</D:/My Project/NexaPlay/NexaPlay/AGENTS.md>)
   - [MIGRATION_PARITY_MATRIX.md](</D:/My Project/NexaPlay/NexaPlay/MIGRATION_PARITY_MATRIX.md>)
2. Validasi build baseline:
   ```powershell
   & 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
     'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64
   ```
3. Jalankan app (`F5`) dan cek:
   - window muncul
   - sidebar navigasi jalan
   - pindah page tidak crash

## 3. Kondisi Terkini yang Sudah Ada

- Shell native `MainWindow` sudah ada (sidebar + frame navigation).
- DI composition root sudah ada di `App.xaml.cs`.
- ViewModel utama sudah ada:
  - `HomeViewModel`, `GamesViewModel`, `LibraryViewModel`, `FixGamesViewModel`, `SettingsViewModel`.
- Service yang sudah termigrasi di `Infrastructure\Services`:
  - `AddGameService`, `FixGamesDataService`, `LicenseService`, `MetadataService`, `OnlineFixService`.
- Kontrak service sudah tersedia di `Contracts\Services`.

## 4. Sumber Kebenaran Fitur Lama (GameHub)

Fitur lama dominan di:
- `MainWindow.xaml.cs` (routing action JS <-> desktop)
- `Services/*`:
  - `HomeDataService`, `FixGamesDataService`, `OnlineFixService`, `AddGameService`, `SteamService`, `UpdateService`, `LicenseService`, `SteamGuardService`, dll.

Catatan:
- Di lama, banyak flow tergantung message `action` dari JS.
- Di baru, flow harus dipindah ke command/event UI native + ViewModel.

## 5. Strategi Migrasi Fitur (Wajib Ikuti Urutan)

### Fase A - Stabilitas Dasar
- jaga build hijau.
- rapikan warning kritis yang berpotensi jadi error.
- verifikasi semua page dapat dinavigasi.

### Fase B - Data Wiring Native
- Home: metrik + list dari `HomeViewModel`.
- Games: virtualized list + search/filter dari `GamesViewModel`.
- Library: scan Steam + status fix applied.
- Fix Games: apply/unfix + progress states.
- Settings: license + update + system checks.

### Fase C - Feature Parity
- cocokkan semua item dari matrix parity.
- setiap fitur dinilai:
  - `Done` jika behavior setara
  - `Partial` jika hanya UI/logic separuh
  - `Missing` jika belum ada

### Fase D - Hardening
- kurangi warning `MVVMTK0045` (AOT-safe pattern).
- update dependency rentan (`SharpCompress`).
- profiling startup dan long-running task.

## 6. Workflow Harian yang Aman

1. Pilih 1 subfitur kecil.
2. Implementasi.
3. Build.
4. Run quick smoke test.
5. Catat status di matrix parity.
6. Lanjut subfitur berikutnya.

Jangan:
- gabung redesign UI + refactor service besar sekaligus.
- push perubahan besar tanpa checkpoint build.

## 7. Error Recovery Playbook

Jika muncul error XAML:
- `WMC0011`: properti tidak valid untuk WinUI control.
- `WMC9997`: XML/tag rusak.
- `WMC0035`/`WMC0055`: tag kacau atau duplikasi assignment.

Aksi:
1. buka line error pertama.
2. perbaiki struktur tag itu dulu.
3. build ulang.
4. jangan lanjut feature lain sebelum compile hijau.

Jika muncul error C# lintas layer:
1. pastikan namespace benar.
2. pastikan kontrak interface sesuai implementasi.
3. cek mismatch model (record vs class, nullability, dll).

## 8. Definisi Selesai (Project Goal)

`NexaPlay` dianggap selesai parity ketika:
- seluruh fitur utama `gamehub` di matrix berstatus `Done`.
- build + run stabil.
- no blocker error runtime.
- UI native final tidak lagi bergantung ke asset/page web lama.

