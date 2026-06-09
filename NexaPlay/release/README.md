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

1. Naikkan versi app.
2. Naikkan versi installer `.iss`.
3. Publish `Release`.
4. Build `setup.exe`.
5. Upload `setup.exe` ke GitHub Release dengan tag versi yang sama.
6. Generate manifest final yang menunjuk ke asset release itu.
7. Commit manifest baru ke `main`.
8. Setelah manifest masuk `main`, barulah app user akan mulai mendeteksi versi baru.

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

### 3. Publish app

Jalankan dari folder `NexaPlay`:

```powershell
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

### 5. Build installer Inno Setup

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

### 6. Upload installer ke GitHub Release

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

### 7. Generate manifest final

Setelah asset release sudah ter-upload, baru generate manifest:

```powershell
.\release\Generate-UpdateManifest.ps1 `
  -Version 1.0.2 `
  -InstallerPath ".\release\output\NexaPlay-Setup.exe" `
  -InstallerUrl "https://github.com/adii83/NexaPlay/releases/download/v1.0.2/NexaPlay-Setup.exe"
```

Script ini akan:

- hitung SHA-256 installer
- generate file:
  `release\update-stable.generated.json`

### 8. Commit manifest ke repo

Salin isi `update-stable.generated.json` ke:

`NexaPlay/release/update-stable.json`

Lalu commit ke branch `main`.

Manifest yang aktif dibaca app adalah:

```text
https://raw.githubusercontent.com/adii83/NexaPlay/main/NexaPlay/release/update-stable.json
```

### 9. Efek ke user

Karena ini rilis publik pertama, app yang baru di-install pada versi `1.0.1` tidak akan melihat update selama manifest juga masih `1.0.1`.

## Checklist Rilis Update Berikutnya

Contoh: user sudah punya `1.0.1`, lalu Anda ingin merilis `1.0.2`.

### 1. Naikkan versi app ke `1.0.2`

- `AppConstants.AppVersion` -> `1.0.2`
- `NexaPlaySetup.iss` -> `1.0.2`

### 2. Publish app

```powershell
dotnet publish .\NexaPlay.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true
```

### 3. Build installer

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" ".\release\NexaPlaySetup.iss"
```

### 4. Upload installer ke GitHub Release `v1.0.2`

Upload asset:

```text
NexaPlay-Setup.exe
```

### 5. Generate manifest `1.0.2`

```powershell
.\release\Generate-UpdateManifest.ps1 `
  -Version 1.0.2 `
  -InstallerPath ".\release\output\NexaPlay-Setup.exe" `
  -InstallerUrl "https://github.com/adii83/NexaPlay/releases/download/v1.0.2/NexaPlay-Setup.exe"
```

### 6. Commit manifest baru ke `main`

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

1. build `setup.exe`
2. upload `setup.exe` ke GitHub Release
3. pastikan URL asset benar-benar hidup
4. generate manifest final
5. commit `update-stable.json`

## Cara Menahan Dialog Update Sementara

Kalau Anda belum siap menampilkan update ke user:

- jangan naikkan field `version` di `update-stable.json`
- biarkan sama dengan versi app yang sedang live

Contoh:

- app live = `1.0.1`
- manifest = `1.0.1`

Maka dialog update tidak akan muncul.

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
