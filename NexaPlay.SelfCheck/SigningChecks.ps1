# Exercises signing retry/verification policy without private keys, network, or modifying publish files.
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $projectRoot "NexaPlay\release\sign-build.ps1"
$tokens = $null
$parseErrors = $null
$tree = [System.Management.Automation.Language.Parser]::ParseFile($sourcePath, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw "Signing script has syntax errors." }
foreach ($name in @("Test-FileSignature", "Invoke-SignFile")) {
    $definition = $tree.Find({ param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $true)
    if ($null -eq $definition) { throw "Missing function: $name" }
    Invoke-Expression $definition.Extent.Text
}

function Start-Sleep { param ([int]$Seconds) } # Retry timing is not part of the mocked command policy.
$fixture = Join-Path ([System.IO.Path]::GetTempPath()) ("nexaplay-sign-check-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $fixture | Out-Null
$SignTool = Join-Path $fixture "fake-signtool.ps1"
$PfxPath = "fixture-only.pfx"
$PlainPassword = "fixture-only"
$MaxAttempts = 3
$ForceResign = $false
$target = Join-Path $fixture "fixture.dll"
Set-Content -LiteralPath $target -Value "fixture"
@'
$mode = Get-Content -LiteralPath (Join-Path $PSScriptRoot "mode.txt")
$countPath = Join-Path $PSScriptRoot "counts.json"
$counts = Get-Content -LiteralPath $countPath -Raw | ConvertFrom-Json
if ($args[0] -eq "sign") {
    $counts.Sign++
    $counts | ConvertTo-Json | Set-Content -LiteralPath $countPath
    if ($mode -eq "permanent" -or ($mode -eq "retry" -and $counts.Sign -lt 2)) { exit 9 }
    exit 0
}
if ($args[0] -eq "verify") {
    $counts.Verify++
    $counts | ConvertTo-Json | Set-Content -LiteralPath $countPath
    if ($mode -eq "valid" -or ($mode -eq "retry" -and $counts.Sign -ge 2)) { exit 0 }
    exit 1
}
exit 99
'@ | Set-Content -LiteralPath $SignTool

try {
    foreach ($case in @(
        @{ Mode = "valid"; Sign = 0; Throws = $false; Force = $false },
        @{ Mode = "retry"; Sign = 2; Throws = $false; Force = $false },
        @{ Mode = "permanent"; Sign = 3; Throws = $true; Force = $false },
        @{ Mode = "verify-failure"; Sign = 3; Throws = $true; Force = $false },
        @{ Mode = "valid"; Sign = 1; Throws = $false; Force = $true }
    )) {
        Set-Content -LiteralPath (Join-Path $fixture "mode.txt") -Value $case.Mode
        @{ Sign = 0; Verify = 0 } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $fixture "counts.json")
        $ForceResign = $case.Force
        $didThrow = $false
        try { Invoke-SignFile -FilePath $target } catch { $didThrow = $true }
        $counts = Get-Content -LiteralPath (Join-Path $fixture "counts.json") -Raw | ConvertFrom-Json
        if ($didThrow -ne $case.Throws -or $counts.Sign -ne $case.Sign) {
            throw "Signing policy failed for $($case.Mode): throws=$didThrow, sign count=$($counts.Sign)."
        }
    }
    Write-Output "Signing self-check passed: skip-valid, transient retry, bounded failure, verification failure, and force-resign."
}
finally {
    $PlainPassword = $null
    $absolute = [System.IO.Path]::GetFullPath($fixture)
    $temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($absolute.StartsWith($temporaryRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
        [System.IO.Path]::GetFileName($absolute).StartsWith("nexaplay-sign-check-")) {
        Remove-Item -LiteralPath $absolute -Recurse -Force
    }
}
