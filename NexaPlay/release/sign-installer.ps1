# sign-installer.ps1
# Sign installer NexaPlay-Setup.exe

$ErrorActionPreference = "Stop"

# =========================
# CONFIG
# =========================
$PfxPath = Join-Path $PSScriptRoot "nexaplay.pfx"
$Installer = Join-Path $PSScriptRoot "output\NexaPlay-Setup.exe"

# Password PFX default
$PlainPassword = 'NexaPlay83geel!'

# =========================
# CEK FILE
# =========================
if (-not (Test-Path -LiteralPath $PfxPath -PathType Leaf)) {
    Write-Host "File certificate tidak ditemukan: $PfxPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $Installer -PathType Leaf)) {
    Write-Host "Installer tidak ditemukan: $Installer" -ForegroundColor Red
    Write-Host "Pastikan NexaPlay-Setup.exe sudah dibuat terlebih dahulu." -ForegroundColor Yellow
    exit 1
}

# =========================
# CARI SIGNTOOL
# =========================
Write-Host "Mencari signtool.exe..." -ForegroundColor Cyan

$WindowsKitsBin = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
$SignTool = Get-ChildItem $WindowsKitsBin -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
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
        Write-Host "`nGAGAL SIGN INSTALLER:" -ForegroundColor Red
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
        Write-Host "`nGAGAL VERIFY INSTALLER:" -ForegroundColor Red
        Write-Host $FilePath -ForegroundColor Red
        exit 1
    }
}

try {
    # =========================
    # SIGN INSTALLER
    # =========================
    Write-Host "`n[1/2] Signing installer..." -ForegroundColor Cyan
    Write-Host "File: $Installer" -ForegroundColor Yellow

    Invoke-SignFile -FilePath $Installer

    # =========================
    # VERIFY INSTALLER
    # =========================
    Write-Host "`n[2/2] Verifikasi installer..." -ForegroundColor Cyan

    Invoke-VerifyFile -FilePath $Installer

    Write-Host "`nDONE: Installer NexaPlay-Setup.exe berhasil disign." -ForegroundColor Green
}
finally {
    $PlainPassword = $null
}