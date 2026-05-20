@echo off
setlocal EnableExtensions
title NexaPlay Watch Runner
chcp 65001 >nul

set "ROOT_DIR=D:\My Project\NexaPlay"
set "PROJECT_FILE=%ROOT_DIR%\NexaPlay\NexaPlay.csproj"
set "LOG_FILE=%ROOT_DIR%\nexaplay_run.log"
set "DOTNET_EXE=C:\Program Files\dotnet\dotnet.exe"

powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '==================================================' -ForegroundColor DarkCyan"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '  NexaPlay Watch Mode (x64, Self-Contained Safe)' -ForegroundColor Cyan"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '==================================================' -ForegroundColor DarkCyan"
echo.

if not exist "%DOTNET_EXE%" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '[ERROR] dotnet tidak ditemukan di:' -ForegroundColor Red"
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '        %DOTNET_EXE%' -ForegroundColor DarkRed"
  pause
  exit /b 1
)

if not exist "%PROJECT_FILE%" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '[ERROR] File project tidak ditemukan:' -ForegroundColor Red"
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '        %PROJECT_FILE%' -ForegroundColor DarkRed"
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '[INFO] Project : ' -ForegroundColor Green -NoNewline; Write-Host '%PROJECT_FILE%' -ForegroundColor Cyan"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '[INFO] Log     : ' -ForegroundColor Green -NoNewline; Write-Host '%LOG_FILE%' -ForegroundColor Cyan"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '[INFO] Mode    : ' -ForegroundColor Green -NoNewline; Write-Host 'dotnet watch run' -ForegroundColor Cyan"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '[INFO] Tips:' -ForegroundColor Yellow"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '       - Ctrl+R : restart manual app' -ForegroundColor DarkYellow"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '       - Ctrl+C : stop watch mode' -ForegroundColor DarkYellow"
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Continue';" ^
  "$root='%ROOT_DIR%';" ^
  "$log='%LOG_FILE%';" ^
  "$crashFile=Join-Path $root 'crash.txt';" ^
  "$eventDump=Join-Path $root 'nexaplay_crash_context.log';" ^
  "function Write-Log([string]$text){ Add-Content -Path $log -Value $text -Encoding Unicode }" ^
  "function Write-ColoredLine([string]$line){" ^
  "  if($line -match '\[ERROR\]|error|failed|exception|crash|Exited with error code|0xc000027b|0xC000027B'){ Write-Host $line -ForegroundColor Red }" ^
  "  elseif($line -match 'warning|warn'){ Write-Host $line -ForegroundColor Yellow }" ^
  "  elseif($line -match 'success|succeeded|Started|Now listening|watch : Started|Hot reload enabled'){ Write-Host $line -ForegroundColor Green }" ^
  "  elseif($line -match 'watch|dotnet|Building|Restore|Project|Log|Mode'){ Write-Host $line -ForegroundColor Cyan }" ^
  "  else{ Write-Host $line -ForegroundColor Gray }" ^
  "}" ^
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
  "Write-Host ('===== NexaPlay Watch Started: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' =====') -ForegroundColor Green;" ^
  "& '%DOTNET_EXE%' watch --project '%PROJECT_FILE%' run -c Debug -r win-x64 --property:Platform=x64 --launch-profile 'NexaPlay (Unpackaged)' 2>&1 | ForEach-Object {" ^
  "  $line=$_.ToString();" ^
  "  Write-ColoredLine $line;" ^
  "  Write-Log $line;" ^
  "  if($line -match 'Exited with error code'){ Dump-CrashContext }" ^
  "}"

set "EXIT_CODE=%ERRORLEVEL%"
echo.
powershell -NoProfile -ExecutionPolicy Bypass -Command "if(%EXIT_CODE% -eq 0){ Write-Host '[INFO] Watch mode berhenti dengan code: %EXIT_CODE%' -ForegroundColor Green } else { Write-Host '[ERROR] Watch mode berhenti dengan code: %EXIT_CODE%' -ForegroundColor Red }"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host '[INFO] Cek log di: ' -ForegroundColor Green -NoNewline; Write-Host '%LOG_FILE%' -ForegroundColor Cyan"
pause
exit /b %EXIT_CODE%