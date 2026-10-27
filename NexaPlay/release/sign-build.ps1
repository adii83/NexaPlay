# sign-build.ps1
# Script ini HANYA sign file hasil publish NexaPlay
# Tidak melakukan dotnet publish / build ulang

[CmdletBinding()]
param (
    [string[]]$OnlyFiles = @(),
    [ValidateRange(1, 5)]
    [int]$MaxAttempts = 3,
    [switch]$ForceResign
)

$ErrorActionPreference = "Stop"

# =========================
# CONFIG
# =========================
$ProjectDir = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$PublishDir = Join-Path $ProjectDir "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
$PfxPath = Join-Path $PSScriptRoot "nexaplay.pfx"

# Password PFX default
$PlainPassword = 'NexaPlay83geel!'

# =========================
# CEK FILE / FOLDER
# =========================
if (-not (Test-Path -LiteralPath $PublishDir -PathType Container)) {
    Write-Host "Folder publish tidak ditemukan: $PublishDir" -ForegroundColor Red
    Write-Host "Pastikan aplikasi sudah di-build/publish terlebih dahulu." -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path -LiteralPath $PfxPath -PathType Leaf)) {
    Write-Host "File certificate tidak ditemukan: $PfxPath" -ForegroundColor Red
    exit 1
}

$MainExe = Join-Path $PublishDir "NexaPlay.exe"

if (-not (Test-Path -LiteralPath $MainExe -PathType Leaf)) {
    Write-Host "File utama NexaPlay.exe tidak ditemukan: $MainExe" -ForegroundColor Red
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

function Test-FileSignature {
    param ([string]$FilePath)

    # /tw also rejects a signature without a timestamp (SignTool warning exit code).
    & $SignTool verify /pa /tw /q "$FilePath" *> $null
    return ($LASTEXITCODE -eq 0)
}

function Invoke-SignFile {
    param (
        [string]$FilePath
    )

    if (-not $ForceResign -and (Test-FileSignature -FilePath $FilePath)) {
        Write-Host "Skip (signature dan timestamp valid): $FilePath" -ForegroundColor DarkGreen
        return
    }

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        Write-Host "Signing [$attempt/$MaxAttempts]: $FilePath" -ForegroundColor Yellow

        & $SignTool sign `
            /f $PfxPath `
            /p $PlainPassword `
            /fd SHA256 `
            /tr "http://timestamp.digicert.com" `
            /td SHA256 `
            "$FilePath"

        $signExitCode = $LASTEXITCODE
        if ($signExitCode -eq 0 -and (Test-FileSignature -FilePath $FilePath)) {
            Write-Host "Signature dan timestamp terverifikasi: $FilePath" -ForegroundColor Green
            return
        }

        Write-Host "Sign/verify belum berhasil (sign exit code: $signExitCode)." -ForegroundColor Yellow
        if ($attempt -lt $MaxAttempts) {
            Write-Host "Mencoba ulang dalam 2 detik..." -ForegroundColor Yellow
            Start-Sleep -Seconds 2
        }
    }

    throw "Gagal sign/verify setelah $MaxAttempts percobaan: $FilePath. Lihat error SignTool di atas; kegagalan dapat berasal dari timestamp, file, atau certificate, bukan selalu password PFX."
}

function Invoke-VerifyFile {
    param (
        [string]$FilePath
    )

    Write-Host "Verifying: $FilePath" -ForegroundColor Cyan

    & $SignTool verify /pa /tw /v "$FilePath"

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

    $FilesToSign = @(Get-ChildItem -LiteralPath $PublishDir -Recurse -File |
        Where-Object {
            $_.Extension -in @(".exe", ".dll") -and
            $_.Name -notin @(
                "createdump.exe",
                "RestartAgent.exe"
            )
        })

    if ($OnlyFiles.Count -gt 0) {
        foreach ($requested in $OnlyFiles) {
            if ($requested -ne [System.IO.Path]::GetFileName($requested) -or
                $requested -notin $FilesToSign.Name) {
                throw "File yang diminta tidak ditemukan dalam daftar signing publish: $requested"
            }
        }
        $FilesToSign = @($FilesToSign | Where-Object { $_.Name -in $OnlyFiles })
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
