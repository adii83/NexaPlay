# NexaPlay Release Packaging

Folder ini berisi file bantu untuk flow release dan auto update NexaPlay berbasis `setup.exe`.

## Ringkasan Alur

App NexaPlay melakukan update check seperti ini:

1. App membaca versi saat ini dari `AppConstants.AppVersion`.
2. App mengambil manifest update dari:
   `https://raw.githubusercontent.com/adii83/NexaPlay/main/NexaPlay/release/update-stable.json`
3. App membandingkan:
   - versi terpasang di app
   - field `version` di `update-stable.json`
4. Jika versi manifest lebih tinggi, dialog update akan muncul.
5. Jika user setuju, app mengunduh file installer dari `installerUrl` di manifest.
6. Setelah download selesai, app menutup dirinya dan membuka `NexaPlay-Setup.exe` biasa.
7. User melanjutkan proses install/update dari wizard installer.

Karena itu, setiap rilis harus menjaga sinkronisasi 4 hal ini:

- versi di `AppConstants.AppVersion`
- versi di `release/NexaPlaySetup.iss`
- tag GitHub Release, mis. `v1.0.1`
- field `version` di `update-stable.json`

## Rule Paling Penting

- Jangan update manifest ke versi baru sebelum asset installer versi itu benar-benar sudah ter-upload ke GitHub Release.
- Kalau manifest lebih dulu menunjuk ke versi baru, app user akan langsung melihat dialog update meskipun asset release belum siap.
- Untuk rilis pertama, manifest boleh tetap menunjuk ke versi yang sama dengan app yang sedang dirilis.
- Untuk mengetes auto update sungguhan, harus ada 2 versi:
  - versi lama sudah terpasang
  - versi baru sudah ada di GitHub Release dan manifest sudah diperbarui

## Lokasi File Penting

- Versi app runtime:
  `NexaPlay/Core/Constants/AppConstants.cs`
- Versi installer:
  `NexaPlay/release/NexaPlaySetup.iss`
- Template manifest:
  `NexaPlay/release/update-stable.json`
- Generator manifest:
  `NexaPlay/release/Generate-UpdateManifest.ps1`
- Generator icon app:
  `NexaPlay/release/Generate-AppIcon.ps1`

## Urutan Rilis Yang Benar

Setiap kali mau merilis versi baru, ikuti urutan ini:

1. Samakan versi app dan installer.
2. Publish aplikasi dalam konfigurasi `Release`.
3. Verifikasi file wajib di folder publish.
4. Sign file hasil publish dengan `sign-build.ps1`.
5. Compile `NexaPlaySetup.iss` memakai Inno Setup.
6. Sign `NexaPlay-Setup.exe` dengan `sign-installer.ps1`.
7. Verifikasi signature aplikasi dan installer.
8. Upload installer final yang sudah ditandatangani ke GitHub Release.
9. Generate manifest dan SHA-256 dari installer final tersebut.
10. Commit manifest baru ke `main`.
11. Setelah manifest masuk `main`, barulah app user akan mulai mendeteksi versi baru.

Signing harus dilakukan pada posisi tersebut. Menandatangani file mengubah isi binary, sehingga SHA-256 manifest yang dihitung sebelum `sign-installer.ps1` tidak lagi valid.

## Checklist Rilis Pertama

Contoh: Anda ingin rilis publik pertama sebagai `1.0.1`.

### 1. Ubah versi app

Ubah:

```csharp
public const string AppVersion = "1.0.1";
```

di:

`NexaPlay/Core/Constants/AppConstants.cs`

### 2. Ubah versi installer

Ubah:

```iss
#define MyAppVersion "1.0.1"
```

di:

`NexaPlay/release/NexaPlaySetup.iss`

### 3. Clean dan publish app

Jalankan dari folder `NexaPlay`. Clean `bin` dan `obj` memastikan apphost dibentuk ulang dengan icon terbaru:

```powershell
Remove-Item ".\bin", ".\obj" -Recurse -Force
dotnet publish .\NexaPlay.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true
```

Output publish default:

```text
bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\
```

### 4. Verifikasi output publish

Cek minimal file penting ini ada:

```powershell
Get-ChildItem ".\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
Get-ChildItem ".\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\Assets"
Get-ChildItem ".\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\data"
```

Minimal harus ada:

- `NexaPlay.exe`
- `Assets\Icons\app.ico`
- `Assets\logo.svg`
- `Assets\logo_text.svg`
- `Assets\Web\youtube-player.html`
- `data\api.json`

Verifikasi juga icon yang benar-benar tertanam di `NexaPlay.exe`, karena taskbar memakai embedded icon tersebut, bukan icon shortcut:

```powershell
Add-Type -AssemblyName System.Drawing
$publishDir = ".\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
$embeddedIcon = [System.Drawing.Icon]::ExtractAssociatedIcon((Resolve-Path "$publishDir\NexaPlay.exe"))
$embeddedIcon.ToBitmap().Save((Join-Path $env:TEMP "NexaPlay-embedded-icon.png"))
Invoke-Item (Join-Path $env:TEMP "NexaPlay-embedded-icon.png")
```

Pastikan preview menampilkan logo terbaru. Jika masih logo lama, jangan lanjut signing; clean `bin`/`obj` lalu publish ulang.

### 5. Sign hasil publish

Jalankan setelah publish dan verifikasi file, tetapi sebelum compile Inno Setup:

```powershell
& ".\release\sign-build.ps1"
```

Script ini menandatangani file hasil publish dan memverifikasi `NexaPlay.exe`. Script tidak menjalankan publish ulang. Jika publish dijalankan lagi setelah tahap ini, ulangi signing karena file hasil publish telah diganti.

Prasyarat signing:

- Windows SDK dengan `signtool.exe` x64;
- `release\nexaplay.pfx` tersedia secara lokal;
- sertifikat masih valid untuk code signing;
- koneksi ke timestamp server tersedia.

`nexaplay.pfx` dan password-nya tidak boleh di-commit ke repository.

### 6. Build installer Inno Setup

Compile file:

`release\NexaPlaySetup.iss`

Jika `ISCC.exe` sudah ada:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" ".\release\NexaPlaySetup.iss"
```

Output installer default:

```text
release\output\NexaPlay-Setup.exe
```

### 7. Sign installer final

Jalankan hanya setelah `NexaPlay-Setup.exe` selesai dibuat:

```powershell
& ".\release\sign-installer.ps1"
```

Jangan compile ulang Inno Setup setelah tahap ini. Compile ulang akan mengganti installer yang sudah ditandatangani dan signing harus diulang.

### 8. Verifikasi signature

Kedua script signing sudah menjalankan `signtool verify`. Pemeriksaan tambahan dapat dilakukan dengan:

```powershell
Get-AuthenticodeSignature `
  ".\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\NexaPlay.exe" |
  Format-List Status, StatusMessage, SignerCertificate

Get-AuthenticodeSignature `
  ".\release\output\NexaPlay-Setup.exe" |
  Format-List Status, StatusMessage, SignerCertificate
```

Pastikan status signature valid sebelum upload.

### 9. Upload installer ke GitHub Release

Buat release/tag:

```text
v1.0.1
```

Upload asset:

```text
NexaPlay-Setup.exe
```

Contoh URL asset final:

```text
https://github.com/adii83/NexaPlay/releases/download/v1.0.1/NexaPlay-Setup.exe
```

### 10. Generate manifest final

Setelah installer final yang sudah ditandatangani di-upload, baru generate manifest:

```powershell
.\release\Generate-UpdateManifest.ps1 `
  -Version 1.0.3 `
  -InstallerPath ".\release\output\NexaPlay-Setup.exe" `
  -InstallerUrl "https://github.com/adii83/NexaPlay/releases/download/v1.0.3/NexaPlay-Setup.exe"
```

Script ini akan:

- hitung SHA-256 installer
- generate file:
  `release\update-stable.generated.json`

### 11. Commit manifest ke repo

Salin isi `update-stable.generated.json` ke:

`NexaPlay/release/update-stable.json`

Lalu commit ke branch `main`.

Manifest yang aktif dibaca app adalah:

```text
https://raw.githubusercontent.com/adii83/NexaPlay/main/NexaPlay/release/update-stable.json
```

### 12. Efek ke user

Karena ini rilis publik pertama, app yang baru di-install pada versi `1.0.1` tidak akan melihat update selama manifest juga masih `1.0.1`.

## Checklist Rilis Update Berikutnya

Contoh: user sudah punya `1.0.1`, lalu Anda ingin merilis `1.0.2`.

### 1. Naikkan versi app ke `1.0.2`

- `AppConstants.AppVersion` -> `1.0.2`
- `NexaPlaySetup.iss` -> `1.0.2`

### 2. Publish dan verifikasi app

```powershell
dotnet publish .\NexaPlay.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true
```

Verifikasi file wajib di folder publish seperti pada checklist rilis pertama.

### 3. Sign hasil publish

```powershell
& ".\release\sign-build.ps1"
```

### 4. Build installer

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" ".\release\NexaPlaySetup.iss"
```

### 5. Sign dan verifikasi installer

```powershell
& ".\release\sign-installer.ps1"

Get-AuthenticodeSignature ".\release\output\NexaPlay-Setup.exe" |
  Format-List Status, StatusMessage, SignerCertificate
```

### 6. Upload installer ke GitHub Release `v1.0.2`

Upload asset final yang sudah ditandatangani:

```text
NexaPlay-Setup.exe
```

### 7. Generate manifest `1.0.2`

Hitung manifest hanya dari installer final yang sudah ditandatangani:

```powershell
.\release\Generate-UpdateManifest.ps1 `
  -Version 1.0.2 `
  -InstallerPath ".\release\output\NexaPlay-Setup.exe" `
  -InstallerUrl "https://github.com/adii83/NexaPlay/releases/download/v1.0.2/NexaPlay-Setup.exe"
```

### 8. Commit manifest baru ke `main`

Setelah `update-stable.json` di branch `main` berubah ke `1.0.2`, maka:

- app user yang masih `1.0.1` akan mendeteksi update
- dialog update akan muncul
- app akan download installer `1.0.2`

## Kapan Manifest Diupdate?

Jawaban singkatnya:

- **Upload installer ke GitHub Release dulu**
- **baru update manifest**

Jangan dibalik.

Urutan yang aman:

1. publish app
2. sign hasil publish
3. build `setup.exe`
4. sign dan verifikasi `setup.exe`
5. upload `setup.exe` final ke GitHub Release
6. pastikan URL asset benar-benar hidup
7. generate manifest final dari installer yang sudah ditandatangani
8. commit `update-stable.json`

## Cara Menahan Dialog Update Sementara

Kalau Anda belum siap menampilkan update ke user:

- jangan naikkan field `version` di `update-stable.json`
- biarkan sama dengan versi app yang sedang live

Contoh:

- app live = `1.0.1`
- manifest = `1.0.1`

Maka dialog update tidak akan muncul.

## Sertifikat Self-Signed dan Unknown Publisher

File `nexaplay.cer` yang berasal dari sertifikat self-signed hanya dipercaya pada komputer yang memasang sertifikat tersebut ke certificate store yang sesuai. Dampaknya:

- komputer yang sudah memercayai sertifikat dapat menampilkan publisher dari signature;
- komputer lain yang belum memasangnya masih dapat menampilkan `Unknown Publisher` atau peringatan certificate chain;
- Microsoft SmartScreen tetap dapat menampilkan `Windows protected your PC` karena reputasi file/certificate terpisah dari validitas signature;
- distribusi publik tanpa instalasi `.cer` manual memerlukan sertifikat code-signing dari CA tepercaya.

Jangan membuat installer yang otomatis memasang sertifikat self-signed ke `Trusted Root Certification Authorities`. Perubahan trust root harus dilakukan secara sadar oleh pemilik komputer atau administrator. Jangan commit `nexaplay.pfx`, file private key, atau password certificate.

## Troubleshooting

### Dialog update muncul padahal belum siap

Penyebab paling umum:

- `update-stable.json` sudah berisi versi lebih tinggi
- atau cache lokal update masih menyimpan hasil check lama

File cache lokal:

```text
%LOCALAPPDATA%\NexaPlay\app_update_state.json
```

Kalau perlu, hapus file itu lalu restart app.

### Setelah install muncul error jaringan

Pastikan Anda memakai installer yang dibangun dari publish release terbaru.

Jangan hanya compile ulang `.iss` kalau folder publish lama belum diperbarui.

Urutan aman:

1. `dotnet publish`
2. compile `Inno Setup`
3. uninstall build lama
4. install build baru

### Logo SVG tidak ikut

Sekarang `Assets/logo.svg` dan `Assets/logo_text.svg` sudah dipaksa ikut ke output publish.

Kalau logo tetap hilang:

- cek folder publish
- cek installer dibangun dari publish terbaru
- reinstall dari installer terbaru

### Icon desktop/taskbar tidak berubah

Sekarang icon exe, shortcut desktop, Start Menu, dan wizard installer memakai:

```text
Assets\Icons\app.ico
```

Kalau source icon PNG berubah, urutan refresh yang aman:

1. ganti `Assets\Icons\logo.png`
2. jalankan:

```powershell
.\release\Generate-AppIcon.ps1
```

3. publish ulang app
4. compile ulang `NexaPlaySetup.iss`
5. uninstall build lama
6. install ulang build baru

## Catatan Tambahan

- Runtime manifest update dibaca dari repo `adii83/NexaPlay`, bukan repo lain.
- Silent arguments update saat ini:
  kosong, karena updater sekarang membuka installer biasa agar user melanjutkan pemasangan manual
- File `release\update-stable.generated.json` hanya artefak bantu, tidak perlu disimpan permanen di repo.
