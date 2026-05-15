# AGENTS.md - NexaPlay Operational Guide

Dokumen ini adalah panduan kerja untuk AI agent yang mengerjakan `NexaPlay`.
Tujuan utamanya: mencegah kerusakan project saat iterasi UI dan menjaga arah migrasi dari `gamehub`.

---

## 1) Project Identity dan Tujuan

### Apa ini?
`NexaPlay` adalah aplikasi desktop native Windows berbasis WinUI 3 (`.NET 8`) yang dibuat sebagai remake dari project `gamehub` lama.

### Kenapa dibuat?
Project lama terlalu berat karena pendekatan UI web-in-desktop (WebView stack + startup load tinggi).
`NexaPlay` dibuat untuk:
- UI native modern dan responsif.
- Struktur kode service/viewmodel yang lebih maintainable.
- Mengurangi risiko regressions saat fitur bertambah.

### Batasan platform
- Windows only.
- Target framework: `net8.0-windows10.0.19041.0`.

---

## 2) Prinsip Arsitektur

Gunakan boundary ini secara ketat:

- `Contracts/`:
  Interface (`INavigationService`, `ILicenseService`, dll).
- `Core/`:
  Model + enum + constant domain.
- `Infrastructure/`:
  Implementasi teknis (file store, steam, metadata, logging, online fix, dll).
- `Presentation/`:
  `Views`, `ViewModels`, converter, navigation UI.
- `App.xaml.cs`:
  Composition root/DI registration.

Aturan:
- ViewModel **tidak** akses filesystem/network langsung.
- ViewModel akses data via interface `Contracts`.
- Code-behind dipakai secukupnya untuk glue UI event.

---

## 3) Konteks Migrasi dari GameHub (Wajib Dipahami)

Asal project:
- `gamehub` lama berisi logic game metadata/fix/license yang sebagian besar masih relevan.

Yang dipindahkan ke `NexaPlay`:
- Service C# backend (license, metadata, fix, steam, persistence, logging).
- Model domain dasar.

Yang **tidak** boleh dibawa kembali:
- Dependensi UI web lama.
- Pola bridge WebView/JS message loop.
- Startup flow yang menunggu terlalu banyak proses sebelum shell tampil.

Strategi migrasi:
- Keep business logic, replace UI layer.
- Migrasi bertahap per page/per service.
- Build harus tetap hijau di setiap batch kecil.

---

## 4) Build dan Run Procedure (Source of Truth)

Selalu validasi via MSBuild Visual Studio (lebih informatif untuk XAML):

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64
```

Poin penting:
- Gunakan `x64`.
- Jangan andalkan `Any CPU` untuk validasi akhir.

Setelah build sukses:
- lanjut validasi runtime lewat Visual Studio (`F5`).

---

## 5) UI/XAML Safety Rules (Hard Rules)

1. Gunakan sintaks WinUI 3, bukan WPF.
2. Jangan pakai properti yang tidak valid di WinUI 3 pada control terkait.
   - contoh gagal sebelumnya: `ClipToBounds` pada `Border`.
3. Jika edit multi-line tag XAML:
   - pastikan `>` tidak hilang.
   - pastikan tidak ada `>` nyasar di baris sendiri.
4. Jangan kosongkan `MainWindow.xaml` jika `MainWindow.xaml.cs` masih referensi nama kontrol tertentu.
5. Setelah ubah page besar:
   - langsung build.
   - jangan menunggu sampai banyak file berubah.

Pattern error yang sering muncul jika melanggar:
- `WMC0011` unknown member
- `WMC9997` XML parsing error
- `WMC0035` duplicate assignment
- `WMC0055` invalid text assigned to UIElement

---

## 6) MainWindow Contract (Jangan Dipecah Sembarangan)

`MainWindow.xaml.cs` mengharapkan elemen penting ini ada di XAML:
- `ContentFrame`
- `NavHome`
- `NavGames`
- `NavLibrary`
- `NavFixGames`
- `NavSettings`
- `PageTitleText`

Jika mau redesign shell:
- ubah XAML + code-behind secara sinkron.
- jangan merge setengah jadi.

Catatan backup:
- file `MainWindow.xaml.bak` boleh dipakai sebagai fallback saat shell rusak.

---

## 7) Prioritas Kerja untuk Agent

Urutan aman:
1. Stabilitas compile.
2. Stabilitas runtime.
3. Struktur data binding.
4. Visual polish.
5. Optimasi.

Artinya:
- Jangan kejar detail visual sebelum build/run stabil.
- Jika XAML rusak parah, pulihkan ke versi valid minimal dulu.

---

## 8) Warning Policy

Warning saat ini yang diketahui:
- `MVVMTK0045`: `[ObservableProperty]` belum AOT-friendly untuk WinRT scenario.
- `NU1902`: `SharpCompress 0.38.0` punya advisory moderat.

Kebijakan:
- warning boleh sementara jika tidak blokir build.
- jangan abaikan permanen; buat task terpisah hardening.

---

## 9) Definition of Done per Perubahan

Sebuah perubahan dianggap selesai jika:
1. Build `Debug x64` sukses (0 error).
2. Aplikasi bisa dibuka.
3. Navigasi utama antar page berjalan.
4. Tidak ada error XAML parser/compiler.
5. Tidak ada edit yang memutus contract `MainWindow` atau DI.

---

## 10) Catatan Operasional untuk AI Lain

- Kerjakan perubahan kecil tapi lengkap.
- Setiap selesai 1 batch, lakukan build.
- Jika menemukan error berantai XAML:
  - berhenti menambah fitur.
  - stabilkan struktur tag dulu.
- Hindari refactor besar lintas layer sekaligus (UI + service + model dalam satu batch besar).
- Saat ragu, pilih opsi paling konservatif yang menjaga project tetap jalan.

