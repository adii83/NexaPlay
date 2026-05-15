# NexaPlay (WinUI 3 Native Desktop)

## Start Here
- [AGENTS.md](</D:/My Project/NexaPlay/NexaPlay/AGENTS.md>) - aturan operasional ketat untuk AI/agent.
- [ONBOARDING_ZERO_TO_PARITY.md](</D:/My Project/NexaPlay/NexaPlay/ONBOARDING_ZERO_TO_PARITY.md>) - panduan dari nol sampai parity.
- [MIGRATION_PARITY_MATRIX.md](</D:/My Project/NexaPlay/NexaPlay/MIGRATION_PARITY_MATRIX.md>) - tracker fitur `gamehub` vs `NexaPlay`.

## Gambaran Singkat
NexaPlay adalah remake desktop native (WinUI 3 + C#) dari konsep GameHub lama yang sebelumnya berbasis WebView/UI web.

Tujuan utama project ini:
- UI modern native Windows yang lebih ringan.
- Arsitektur yang mudah dikembangkan (service + viewmodel).
- Menghindari startup berat dan kerusakan layout saat iterasi UI.

Project ini **fokus Windows** dan target framework:
- `net8.0-windows10.0.19041.0`

---

## Status Saat Ini
Kondisi terkini:
- Build `Debug x64` sudah berhasil.
- Shell utama menggunakan sidebar custom di `MainWindow`.
- Halaman utama (`Home`, `Games`, `Library`, `Fix Games`, `Settings`) sudah ada.
- Sebagian halaman sudah distabilkan ulang agar valid di WinUI 3.

Catatan penting:
- Masih ada warning `MVVMTK0045` (AOT compatibility pada `[ObservableProperty]`).
- Ada warning dependency `SharpCompress` (`NU1902`) yang perlu upgrade versi di fase hardening.

---

## Struktur Folder
Struktur kerja yang dipakai:

- `Contracts/`
  - Interface service/navigation.
- `Core/`
  - Model, enum, constant inti domain.
- `Infrastructure/`
  - Implementasi service teknis (store, platform, logging, data service).
- `Presentation/`
  - `Views` (XAML pages/dialogs), `ViewModels`, converter, navigation.
- `Application/`
  - Ruang untuk orchestration/use-case (dipakai bertahap).
- `Assets/`
  - Resource UI (logo, icon, visual assets).

---

## Aturan Aman Saat Edit UI (Penting)
Untuk mencegah project rusak saat AI/kontributor mengubah UI:

1. Gunakan sintaks WinUI 3, **bukan WPF**.
2. Jangan pakai properti yang tidak didukung WinUI 3 (contoh: `ClipToBounds` pada `Border`).
3. Jika memecah atribut XAML ke multi-line, pastikan tag tetap valid:
   - Contoh benar:
   ```xml
   <Border
       CornerRadius="12"
       BorderThickness="1">
   ```
4. Jangan kosongkan `MainWindow.xaml` jika `MainWindow.xaml.cs` masih memakai elemen:
   - `ContentFrame`, `NavHome`, `NavGames`, `NavLibrary`, `NavFixGames`, `NavSettings`, `PageTitleText`.
5. Jika melakukan eksperimen layout besar:
   - simpan backup dulu (misal `*.bak`) sebelum edit agresif.

---

## Alur Navigasi
Navigasi utama dikendalikan oleh:
- `MainWindow.xaml.cs`
- `Frame` bernama `ContentFrame`

Mapping halaman:
- `Home` -> `HomePage`
- `Games` -> `GamesPage`
- `Library` -> `LibraryPage`
- `Fix Games` -> `FixGamesPage`
- `Settings` -> `SettingsPage`

---

## Cara Build yang Disarankan
Gunakan MSBuild Visual Studio (lebih jelas untuk error XAML WinUI):

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64
```

Kenapa `x64`:
- Project packaged WinUI biasanya paling stabil di `x64`.
- Menghindari konflik default `AnyCPU`.

---

## Checklist Sebelum Commit
Wajib cek:

1. Build `Debug x64` sukses (`0 error`).
2. Aplikasi bisa run dan pindah halaman dari sidebar.
3. Tidak ada XAML parse error:
   - `WMC9997`, `WMC0035`, `WMC0055`, `WMC0011`.
4. Jika ada perubahan UI besar, verifikasi:
   - semua tag `Border`, `Grid`, `StackPanel` tertutup benar.
5. Jangan campur refactor service besar bersamaan dengan redesign visual besar dalam satu commit.

---

## Prioritas Pengembangan Berikutnya
Urutan aman agar tidak mudah rusak:

1. Stabilkan page UI satu per satu (`Home` lalu `Games`).
2. Hubungkan binding ke ViewModel secara bertahap.
3. Terapkan virtualized list untuk katalog game.
4. Kurangi warning toolkit/AOT (`MVVMTK0045`).
5. Upgrade dependency yang kena advisory (`SharpCompress`).

---

## Catatan untuk AI Agent Lain
Jika Anda AI yang melanjutkan project ini:

- Prioritaskan **stabilitas compile** sebelum menambah kompleksitas UI.
- Jika XAML error berantai muncul, pulihkan dulu ke layout valid minimal, baru polish ulang.
- Jangan memodifikasi banyak halaman sekaligus tanpa build check di tengah.
- Jalankan build setelah setiap batch perubahan UI utama.
