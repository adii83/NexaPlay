$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$steam = Get-Content -Raw (Join-Path $root 'Infrastructure\Platform\SteamPlatformService.cs')
$viewModel = Get-Content -Raw (Join-Path $root 'Presentation\ViewModels\BypassGameDetailViewModel.cs')
$mainWindow = Get-Content -Raw (Join-Path $root 'MainWindow.xaml.cs')

$checks = [ordered]@{
    'recovery has bounded retries' = $steam -match 'const int maxRecoveryAttempts = 4;'
    'recovery waits between checks' = $steam -match 'Task\.Delay\(TimeSpan\.FromSeconds\(30\), ct\)'
    'recovery logs the final verified state' = $steam -match 'Restore manifest verified appid='
    'startup does not block metadata warmup' = $mainWindow -match '_ = RestoreManagedManifestsAsync\(\);\s+await WarmupMetadataStartupAsync\(\);'
    'success dialog is generic' = $viewModel -match 'Semua proses bypass berhasil diselesaikan\. Game siap dijalankan\.'
    'user copy omits update-lock wording' = $viewModel -notmatch '(?i)update game|proteksi update Steam'
}

$failed = @($checks.GetEnumerator() | Where-Object { -not $_.Value })
$checks.GetEnumerator() | ForEach-Object { '{0} {1}' -f $(if ($_.Value) { 'PASS' } else { 'FAIL' }), $_.Key }

if ($failed.Count -gt 0) {
    throw "$($failed.Count) manifest recovery contract(s) failed."
}
