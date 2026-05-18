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
  "$stamp=(Get-Date).ToString('yyyy-MM-dd HH:mm:ss');" ^
  "'' | Out-File -FilePath '%LOG_FILE%' -Encoding utf8;" ^
  "'===== NexaPlay Watch Started: ' + $stamp + ' =====' | Out-File -FilePath '%LOG_FILE%' -Append -Encoding utf8;" ^
  "& '%DOTNET_EXE%' watch --project '%PROJECT_FILE%' run -c Debug -r win-x64 --property:Platform=x64 --launch-profile 'NexaPlay (Unpackaged)' 2>&1 | Tee-Object -FilePath '%LOG_FILE%' -Append"

set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo [INFO] Watch mode berhenti dengan code: %EXIT_CODE%
echo [INFO] Cek log di: %LOG_FILE%
pause
exit /b %EXIT_CODE%
