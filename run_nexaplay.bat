@echo off
setlocal EnableExtensions
title NexaPlay Watch Runner
chcp 65001 >nul

set "ROOT_DIR=D:\My Project\NexaPlay"
set "PROJECT_FILE=%ROOT_DIR%\NexaPlay\NexaPlay.csproj"
set "LOG_FILE=%ROOT_DIR%\nexaplay_run.log"
set "DOTNET_EXE=C:\Program Files\dotnet\dotnet.exe"

echo ==================================================
echo   NexaPlay Watch Mode (x64, Self-Contained Safe)
echo ==================================================
echo.

if not exist "%DOTNET_EXE%" (
  echo [ERROR] dotnet tidak ditemukan di:
  echo         %DOTNET_EXE%
  pause
  exit /b 1
)

if not exist "%PROJECT_FILE%" (
  echo [ERROR] File project tidak ditemukan:
  echo         %PROJECT_FILE%
  pause
  exit /b 1
)

echo [INFO] Project : %PROJECT_FILE%
echo [INFO] Log     : %LOG_FILE%
echo [INFO] Mode    : dotnet watch run
echo [INFO] Tips:
echo        - Ctrl+R : restart manual app
echo        - Ctrl+C : stop watch mode
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Continue';" ^
  "$root='%ROOT_DIR%';" ^
  "$log='%LOG_FILE%';" ^
  "$crashFile=Join-Path $root 'crash.txt';" ^
  "$eventDump=Join-Path $root 'nexaplay_crash_context.log';" ^
  "function Write-Log([string]$text){ Add-Content -Path $log -Value $text -Encoding Unicode }" ^
  "function Dump-CrashContext{" ^
  "  $ts=Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff';" ^
  "  Add-Content -Path $eventDump -Value ('===== Crash Context ' + $ts + ' =====') -Encoding UTF8;" ^
  "  if(Test-Path $crashFile){ Add-Content -Path $eventDump -Value '--- crash.txt (tail 120) ---' -Encoding UTF8; Get-Content $crashFile -Tail 120 | Add-Content -Path $eventDump -Encoding UTF8 }" ^
  "  Add-Content -Path $eventDump -Value '--- Application/Error Events (last 15m, top 30) ---' -Encoding UTF8;" ^
  "  Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddMinutes(-15)} -ErrorAction SilentlyContinue |" ^
  "    Where-Object { $_.ProviderName -in @('Application Error','.NET Runtime','Windows Error Reporting') -or $_.Message -match 'NexaPlay|NexaPlay.exe|KERNELBASE|0xc000027b|0xC000027B' } |" ^
  "    Select-Object -First 30 TimeCreated, Id, LevelDisplayName, ProviderName, Message | Format-List | Out-String -Width 500 | Add-Content -Path $eventDump -Encoding UTF8;" ^
  "  Add-Content -Path $eventDump -Value '' -Encoding UTF8;" ^
  "}" ^
  "'' | Out-File -FilePath $log -Encoding Unicode;" ^
  "Write-Log ('===== NexaPlay Watch Started: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' =====');" ^
  "& '%DOTNET_EXE%' watch --project '%PROJECT_FILE%' run -c Debug -r win-x64 --property:Platform=x64 --launch-profile 'NexaPlay (Unpackaged)' 2>&1 | ForEach-Object {" ^
  "  $line=$_.ToString();" ^
  "  Write-Host $line;" ^
  "  Write-Log $line;" ^
  "  if($line -match 'Exited with error code'){ Dump-CrashContext }" ^
  "}"

set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo [INFO] Watch mode berhenti dengan code: %EXIT_CODE%
echo [INFO] Cek log di: %LOG_FILE%
pause
exit /b %EXIT_CODE%
