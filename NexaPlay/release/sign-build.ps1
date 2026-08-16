# sign-build.ps1
# Script ini HANYA sign file hasil publish NexaPlay
# Tidak melakukan dotnet publish / build ulang

$ErrorActionPreference = "Stop"

# =========================
# CONFIG
# =========================
$ProjectDir  = "D:\My Project\NexaPlay\NexaPlay"
$PublishDir = "$ProjectDir\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
$PfxPath    = "$ProjectDir\release\nexaplay.pfx"

# Password PFX default
$PlainPassword = 'NexaPlay83geel!'

# =========================
# CEK FILE / FOLDER
# =========================
if (-not (Test-Path $PublishDir)) {
    Write-Host "Folder publish tidak ditemukan: $PublishDir" -ForegroundColor Red
    Write-Host "Pastikan aplikasi sudah di-build/publish terlebih dahulu." -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $PfxPath)) {
    Write-Host "File certificate tidak ditemukan: $PfxPath" -ForegroundColor Red
    exit 1
}

$MainExe = "$PublishDir\NexaPlay.exe"

if (-not (Test-Path $MainExe)) {
    Write-Host "File utama NexaPlay.exe tidak ditemukan: $MainExe" -ForegroundColor Red
    exit 1
}

# =========================
# CARI SIGNTOOL
# =========================
Write-Host "Mencari signtool.exe..." -ForegroundColor Cyan

$SignTool = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -like "*\x64\signtool.exe" } |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $SignTool) {
    Write-Host "signtool.exe tidak ditemukan." -ForegroundColor Red
    Write-Host "Install Windows SDK terlebih dahulu, lalu pastikan Signing Tools terpasang." -ForegroundColor Yellow
    exit 1
}

Write-Host "SignTool ditemukan: $SignTool" -ForegroundColor Green

function Invoke-SignFile {
    param (
        [string]$FilePath
    )

    Write-Host "Signing: $FilePath" -ForegroundColor Yellow

    & $SignTool sign `
        /f $PfxPath `
        /p $PlainPassword `
        /fd SHA256 `
        /tr "http://timestamp.digicert.com" `
        /td SHA256 `
        "$FilePath"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nGAGAL SIGN FILE:" -ForegroundColor Red
        Write-Host $FilePath -ForegroundColor Red
        Write-Host "Kemungkinan password PFX salah atau certificate tidak valid untuk code signing." -ForegroundColor Yellow
        exit 1
    }
}

function Invoke-VerifyFile {
    param (
        [string]$FilePath
    )

    Write-Host "Verifying: $FilePath" -ForegroundColor Cyan

    & $SignTool verify /pa /v "$FilePath"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nGAGAL VERIFY FILE:" -ForegroundColor Red
        Write-Host $FilePath -ForegroundColor Red
        exit 1
    }
}

try {
    # =========================
    # SIGN FILE HASIL PUBLISH
    # =========================
    Write-Host "`n[1/2] Mencari file EXE dan DLL untuk disign..." -ForegroundColor Cyan

    $FilesToSign = Get-ChildItem $PublishDir -Recurse -Include *.exe, *.dll |
        Where-Object {
            $_.Name -notin @(
                "createdump.exe",
                "RestartAgent.exe"
            )
        }

    if ($FilesToSign.Count -eq 0) {
        Write-Host "Tidak ada file EXE/DLL ditemukan di folder publish." -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Total file yang akan disign: $($FilesToSign.Count)" -ForegroundColor Green

    foreach ($file in $FilesToSign) {
        Invoke-SignFile -FilePath $file.FullName
    }

    # =========================
    # VERIFY FILE UTAMA
    # =========================
    Write-Host "`n[2/2] Verifikasi NexaPlay.exe..." -ForegroundColor Cyan

    Invoke-VerifyFile -FilePath $MainExe

    Write-Host "`nDONE: File hasil publish berhasil disign." -ForegroundColor Green
    Write-Host "File utama: $MainExe" -ForegroundColor Green
}
finally {
    $PlainPassword = $null
}