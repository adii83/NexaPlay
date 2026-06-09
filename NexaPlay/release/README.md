# NexaPlay Release Packaging

Folder ini berisi file bantu untuk flow update NexaPlay berbasis `setup.exe`.

## 1. Publish app

Jalankan dari folder project `NexaPlay`:

```powershell
dotnet publish .\NexaPlay.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true
```

Output publish default akan muncul di:

```text
bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\
```

## 2. Build installer dengan Inno Setup

Install `Inno Setup 6`, lalu compile file:

```text
release\NexaPlaySetup.iss
```

Output installer default:

```text
release\output\NexaPlay-Setup.exe
```

Jika `ISCC.exe` sudah ada di default path, compile bisa lewat:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" ".\release\NexaPlaySetup.iss"
```

## 3. Tentukan skenario rilis

Ada 2 skenario yang berbeda:

- `Rilis pertama / baseline install`
  Gunakan versi app yang sekarang, yaitu `1.0.0`. Ini dipakai agar user punya base install awal.
- `Test auto update`
  Setelah `1.0.0` sudah terpasang, baru naikkan versi app ke `1.0.1` dan buat release kedua untuk menguji dialog update + download installer + relaunch.

## 4. Generate manifest update final

### Jika Anda sedang membuat rilis pertama `v1.0.0`

Setelah `setup.exe` jadi, jalankan:

```powershell
.\release\Generate-UpdateManifest.ps1 `
  -Version 1.0.0 `
  -InstallerPath ".\release\output\NexaPlay-Setup.exe" `
  -InstallerUrl "https://github.com/adii83/NexaPlay/releases/download/v1.0.0/NexaPlay-Setup.exe"
```

### Jika Anda sedang membuat rilis test update `v1.0.1`

Setelah `setup.exe` jadi, jalankan:

```powershell
.\release\Generate-UpdateManifest.ps1 `
  -Version 1.0.1 `
  -InstallerPath ".\release\output\NexaPlay-Setup.exe" `
  -InstallerUrl "https://github.com/adii83/NexaPlay/releases/download/v1.0.1/NexaPlay-Setup.exe"
```

Script ini akan:

- hitung SHA-256 installer
- generate file `release\update-stable.generated.json`

## 5. Upload ke GitHub

Upload 2 file berikut ke release GitHub:

- `NexaPlay-Setup.exe`
- commit `release\update-stable.generated.json` sebagai `NexaPlay/release/update-stable.json` ke branch `main` repo ini:
  `https://raw.githubusercontent.com/adii83/NexaPlay/main/NexaPlay/release/update-stable.json`

Alur repo final sekarang:

- Asset installer diambil dari GitHub Releases repo `adii83/NexaPlay`
- Manifest update dibaca dari file `NexaPlay/release/update-stable.json` di branch `main` repo yang sama

## Catatan penting

- `AppConstants.AppVersion` saat ini masih `1.0.0`.
- Supaya update terdeteksi, field `version` di manifest harus lebih besar dari versi yang terpasang.
- Untuk baseline pertama, manifest `1.0.0` itu normal. Update baru akan benar-benar terdeteksi saat app `1.0.0` membaca manifest `1.0.1` atau versi yang lebih tinggi.
- Silent arguments update yang dipakai app saat ini:
  `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`
