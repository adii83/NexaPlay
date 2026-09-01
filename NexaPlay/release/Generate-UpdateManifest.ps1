param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$InstallerPath,

    [Parameter(Mandatory = $true)]
    [string]$InstallerUrl,

    [string[]]$ReleaseNotes = @(
        "Perbaikan dan penyempurnaan NexaPlay."
    ),

    [switch]$Mandatory
)

$ErrorActionPreference = "Stop"

$installerCandidate = if ([System.IO.Path]::IsPathRooted($InstallerPath)) {
    $InstallerPath
}
else {
    Join-Path $PSScriptRoot $InstallerPath
}

if (-not (Test-Path -LiteralPath $installerCandidate -PathType Leaf)) {
    throw "Installer tidak ditemukan: $installerCandidate"
}

$resolvedInstaller = (Resolve-Path -LiteralPath $installerCandidate).Path
$hash = (Get-FileHash -LiteralPath $resolvedInstaller -Algorithm SHA256).Hash.ToUpperInvariant()
$publishedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:sszzz")

$manifest = [ordered]@{
    version = $Version
    installerUrl = $InstallerUrl
    installerSha256 = $hash
    publishedAt = $publishedAt
    mandatory = [bool]$Mandatory
    releaseNotes = $ReleaseNotes
}

$json = $manifest | ConvertTo-Json -Depth 4
$outputPath = Join-Path $PSScriptRoot "update-stable.generated.json"
Set-Content -Path $outputPath -Value $json -Encoding UTF8

Write-Host "Manifest berhasil dibuat:"
Write-Host $outputPath
Write-Host ""
Write-Host "SHA256:"
Write-Host $hash
