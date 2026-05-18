# AGENTS.md - NexaPlay Operational Guide

Dokumen ini adalah aturan operasional wajib untuk AI agent yang melanjutkan `NexaPlay`.
Tujuan: mencegah kebingungan migrasi dan menjaga progress UI tetap stabil.

## 1) Identitas Project
- Project utama: `D:\My Project\NexaPlay\NexaPlay`
- Referensi lama (lokal dalam workspace yang sama): `D:\My Project\NexaPlay\gamehub`
- Platform: Windows only
- Stack: WinUI 3 + C# + .NET 8

Konsep:
- `NexaPlay` adalah remake native dari `GameHub`.
- Fitur utama harus tetap parity dengan GameHub.
- UI/arsitektur boleh berubah selama behavior inti tetap setara.

## 2) Prioritas Product Saat Ini
Prioritas aktif adalah UI.

Ekspektasi UI:
- modern, ringan, konsisten.
- bukan gaya visual ramai/acak.
- nomenklatur utama:
  - `Home` (bukan Dashboard)
  - `Bypass Games` (pengganti istilah lama `Fix Games` di surface UI).

## 3) Boundary Arsitektur (Tidak Boleh Dilanggar)
- `Contracts/`: hanya interface.
- `Core/`: model/enum/constant domain.
- `Infrastructure/`: implementasi teknis.
- `Presentation/`: XAML pages, viewmodels, converters.
- `App.xaml.cs`: composition root DI.

Aturan keras:
- ViewModel tidak akses filesystem/network langsung.
- Semua akses teknis lewat service interface di `Contracts`.

## 4) Cara Memahami GameHub Sebelum Migrasi Fitur
Saat mengerjakan fitur baru di NexaPlay:
1. Baca dulu implementasi lama di `D:\My Project\NexaPlay\gamehub`.
2. Ambil intent behavior dan edge case.
3. Implementasikan ulang di arsitektur native NexaPlay.
4. Jangan copy pola WebView/JS bridge ke NexaPlay.

Yang boleh dibawa:
- logic bisnis/service.
- struktur data domain.

Yang tidak boleh dibawa:
- web host UI.
- JS action bridge sebagai pusat flow aplikasi.
- ketergantungan startup yang menunggu web state.

## 5) Build Gate Wajib
Setiap batch perubahan wajib build:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64
```

Ketentuan:
- jangan lanjut batch berikutnya sebelum build hijau.
- setelah build hijau, lakukan smoke test run (`F5`).

## 6) UI/XAML Hard Rules
1. Gunakan sintaks WinUI 3, bukan WPF.
2. Jangan pakai properti control yang tidak didukung WinUI.
3. Jika edit tag multiline, pastikan tag XML valid.
4. Jangan ubah contract `MainWindow` secara separuh (XAML tanpa sinkron code-behind).
5. Jika XAML error berantai, stop feature dan pulihkan struktur dulu.

Error pattern umum:
- `WMC0011`
- `WMC9997`
- `WMC0035`
- `WMC0055`

## 7) MainWindow Contract Check
Sebelum commit, pastikan elemen yang dipakai code-behind masih ada.
Nama elemen wajib mengikuti kondisi aktual file `MainWindow.xaml.cs` terbaru.

Catatan:
- jika istilah lama (`NavFixGames`) dan baru (`NavBypass`) masih campur, selesaikan konsistensi bertahap dengan build checkpoint.
- jangan lakukan rename masif bersamaan dengan redesign besar.

## 8) Workflow Aman (Default)
1. Pilih 1 perubahan kecil.
2. Implementasi.
3. Build gate.
4. Smoke test.
5. Update dokumentasi parity bila behavior berubah.
6. Update `AI_HANDOFF_PROMPT.md` bila ada perubahan konteks, keputusan desain, status fitur, atau build result penting.

Larangan:
- ubah banyak page sekaligus tanpa checkpoint.
- gabung refactor service besar dan redesign visual besar dalam 1 batch.

## 9) Definition of Done per Batch
Batch dianggap selesai jika:
1. Build `Debug x64` sukses (0 error).
2. App bisa dibuka.
3. Navigasi halaman utama normal.
4. Tidak ada parser error XAML.
5. Tidak memutus boundary arsitektur.

## 10) Urutan Baca untuk Agent Baru
Wajib baca:
1. `README.md`
2. `AGENTS.md`
3. `ONBOARDING_ZERO_TO_PARITY.md`
4. `MIGRATION_PARITY_MATRIX.md`
5. `AI_HANDOFF_PROMPT.md`

Jika konflik antara asumsi agent dan dokumen:
- dokumen project menang.

## 11) UI Design Contract
Aturan utama:
1. Fitur NexaPlay harus tetap parity dengan GameHub. Jangan mengurangi alur penting.
2. Ubah hanya tampilan atau struktur UI native. Jangan ubah behavior inti tanpa alasan kuat.
3. Gaya visual harus profesional, clean, netral, dan konsisten.
4. UI tidak boleh terasa AI-generated.

Larangan visual:
- jangan pakai emoji di UI, kecuali memang lebih tepat dari ikon dan tetap satu warna.
- jangan warna campur/random.
- jangan gradient berlebihan.
- jangan style ramai, gimmick, atau dekorasi yang tidak perlu.
- jangan copywriting hiperbolik.
- jangan redesign semua halaman sekaligus.

Design system:
- fokus dark theme hitam dan putih.
- gunakan netral dark surface yang konsisten.
- gunakan satu warna aksen utama hanya bila perlu.
- tombol utama cenderung putih.
- tipografi harus rapi dengan hierarchy jelas.
- spacing harus stabil.
- visual depth secukupnya melalui border/surface level, bukan efek ramai.

Target desain:
- modern tetapi enterprise-clean.
- sidebar, topbar, dan content area rapi.
- teks ringkas, formal, dan tidak dekoratif berlebihan.

## 12) Performance Contract
- startup harus ringan.
- gunakan lazy loading untuk data/detail berat.
- Games list harus virtualized.
- hindari operasi berat di UI thread.
- jangan download atau parse data besar saat startup bila tidak diperlukan.

## 13) Engineering Principles
Terapkan prinsip SOLID untuk pengembangan berikutnya, terutama saat menambah service, ViewModel, metadata flow, dan action game.

Aturan praktis:
- Single Responsibility: satu class punya satu alasan utama untuk berubah. Jangan campur fetch metadata, formatting UI, dan command action dalam satu class.
- Open/Closed: tambah behavior lewat service/helper baru atau interface yang jelas, bukan menumpuk `if` besar di ViewModel.
- Liskov Substitution: implementasi service harus bisa dipakai lewat interface tanpa behavior mengejutkan.
- Interface Segregation: jangan buat interface besar. Pisahkan service metadata, Steam, Online-Fix, Add Game, dan Bypass bila tanggung jawabnya berbeda.
- Dependency Inversion: Presentation bergantung ke `Contracts`, bukan langsung ke implementasi `Infrastructure`.

Untuk AI agent:
- sebelum menambah kode, cari boundary yang paling sesuai.
- jangan membuat ViewModel menjadi tempat semua logic teknis.
- jika perubahan mulai membesar, pecah menjadi service kecil dan build checkpoint.
