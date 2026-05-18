# NexaPlay (WinUI 3 Native Desktop)

## Start Here (Wajib Baca Berurutan)
1. [README.md](</D:/My Project/NexaPlay/NexaPlay/README.md>)
2. [AGENTS.md](</D:/My Project/NexaPlay/NexaPlay/AGENTS.md>)
3. [ONBOARDING_ZERO_TO_PARITY.md](</D:/My Project/NexaPlay/NexaPlay/ONBOARDING_ZERO_TO_PARITY.md>)
4. [MIGRATION_PARITY_MATRIX.md](</D:/My Project/NexaPlay/NexaPlay/MIGRATION_PARITY_MATRIX.md>)
5. [AI_HANDOFF_PROMPT.md](</D:/My Project/NexaPlay/NexaPlay/AI_HANDOFF_PROMPT.md>)

## Tujuan Project
NexaPlay adalah remake native WinUI 3 dari GameHub lama.

Target utama:
- Tetap mempertahankan fitur inti GameHub (feature parity).
- Fokus ke UI native yang ringan dan modern.
- Tidak kembali ke pola WebView/JS bridge.

Target framework:
- `net8.0-windows10.0.19041.0`

## Lokasi Workspace yang Benar
- Project utama NexaPlay: `D:\My Project\NexaPlay\NexaPlay`
- Referensi GameHub (lokal, dalam workspace yang sama): `D:\My Project\NexaPlay\gamehub`

Catatan:
- Semua analisis fitur lama lakukan dari salinan lokal `D:\My Project\NexaPlay\gamehub`.
- Jangan refer ke path luar workspace ini untuk menghindari kebingungan agent lain.

## Fokus UI Saat Ini
- Halaman utama menggunakan istilah `Home` (bukan Dashboard).
- Fitur yang dulu dikenal sebagai `Fix Games` sedang diarahkan menjadi `Bypass Games`.
- Desain yang diinginkan: modern, bersih, konsisten, bukan gaya visual yang terlalu ramai/acak.

## Prinsip Arsitektur
- `Contracts/`: interface.
- `Core/`: domain model/enum/constants.
- `Infrastructure/`: implementasi teknis service.
- `Presentation/`: `Views` + `ViewModels`.
- `Application/`: orchestration/use-case (bertahap).

Aturan inti:
- ViewModel tidak boleh akses IO/network langsung.
- Semua akses teknis lewat interface di `Contracts`.

## Build Gate (Wajib)
```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64
```

Standar lolos:
- Build sukses (0 error).
- App bisa run.
- Navigasi utama berpindah halaman tanpa crash.

## UI Safety Rules (Ringkas)
1. Gunakan sintaks WinUI 3, bukan WPF.
2. Jangan gunakan properti control yang tidak valid di WinUI.
3. Hindari perubahan besar banyak halaman sekaligus.
4. Setelah setiap batch UI kecil, langsung build.
5. Jika error XAML berantai, stabilkan struktur tag dulu sebelum lanjut fitur.

## Istilah Resmi (Untuk Konsistensi)
- `Home` adalah nama halaman utama.
- `Bypass Games` adalah istilah UI baru untuk area yang dulunya `Fix Games`.
- Jika ada nama lama di kode/dokumen (`Fix Games`), anggap itu technical debt migrasi naming dan selesaikan bertahap.

## Prioritas Kerja Selanjutnya
1. Konsistensikan istilah UI (`Bypass Games`) di dokumen dan surface UI.
2. Poles UI halaman per halaman mulai dari `Home`.
3. Jaga parity logic terhadap referensi GameHub.
4. Perbarui status fitur di `MIGRATION_PARITY_MATRIX.md` setiap selesai checkpoint.
5. Perbarui `AI_HANDOFF_PROMPT.md` setiap ada perubahan konteks/fokus penting agar agent berikutnya tidak mulai dari nol.
