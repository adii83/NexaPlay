@echo off
setlocal EnableExtensions
title NexaPlay Build ^& Run
chcp 65001 >nul

set "ROOT_DIR=D:\My Project\NexaPlay"
set "PROJECT_FILE=%ROOT_DIR%\NexaPlay\NexaPlay.csproj"
set "MSBUILD=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"
set "EXE_PATH=%ROOT_DIR%\NexaPlay\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\NexaPlay.exe"
set "CRASH_LOG=%ROOT_DIR%\crash.txt"
set "CRASH_CONTEXT=%ROOT_DIR%\nexaplay_crash_context.log"
set "APP_LOG=%LOCALAPPDATA%\NexaPlay\nexaplay.log"

echo.
echo ==================================================
echo   NexaPlay Build ^& Run (MSBuild x64 Debug)
echo ==================================================
echo.

:: --- Validasi ---
if not exist "%MSBUILD%" (
    echo [ERROR] MSBuild tidak ditemukan di: %MSBUILD%
    pause
    exit /b 1
)
if not exist "%PROJECT_FILE%" (
    echo [ERROR] Project file tidak ditemukan: %PROJECT_FILE%
    pause
    exit /b 1
)

:: --- Kill NexaPlay yang sedang jalan ---
taskkill /IM NexaPlay.exe /F >nul 2>&1
timeout /t 1 /nobreak >nul

:: --- Build ---
echo [BUILD] Memulai build...
echo.
"%MSBUILD%" "%PROJECT_FILE%" /restore /p:Configuration=Debug /p:Platform=x64 /v:minimal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build FAILED. Perbaiki error dulu.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [BUILD] Build succeeded!

:: --- Cek exe ---
if not exist "%EXE_PATH%" (
    echo [ERROR] Exe tidak ditemukan: %EXE_PATH%
    pause
    exit /b 1
)

:: --- Catat ukuran crash.txt sebelum run (untuk deteksi crash baru) ---
set "CRASH_SIZE_BEFORE=0"
if exist "%CRASH_LOG%" (
    for %%A in ("%CRASH_LOG%") do set "CRASH_SIZE_BEFORE=%%~zA"
)

:: --- Run dan tunggu sampai exit ---
echo [RUN]   Launching NexaPlay...
echo.
start /wait "" "%EXE_PATH%"
set "EXIT_CODE=%ERRORLEVEL%"

:: --- Cek apakah crash terjadi ---
set "CRASH_SIZE_AFTER=0"
if exist "%CRASH_LOG%" (
    for %%A in ("%CRASH_LOG%") do set "CRASH_SIZE_AFTER=%%~zA"
)

if %EXIT_CODE% NEQ 0 (
    echo.
    echo [CRASH] NexaPlay exited with code: %EXIT_CODE%
    echo.

    :: --- Dump crash context ---
    echo ===== Crash Context %DATE% %TIME% ===== > "%CRASH_CONTEXT%"

    if exist "%CRASH_LOG%" (
        echo --- crash.txt (last 60 lines) --- >> "%CRASH_CONTEXT%"
        powershell -NoProfile -Command "Get-Content '%CRASH_LOG%' -Tail 60" >> "%CRASH_CONTEXT%"
        echo. >> "%CRASH_CONTEXT%"
    )

    if exist "%APP_LOG%" (
        echo --- nexaplay.log (last 120 lines) --- >> "%CRASH_CONTEXT%"
        powershell -NoProfile -Command "Get-Content '%APP_LOG%' -Tail 120" >> "%CRASH_CONTEXT%"
        echo. >> "%CRASH_CONTEXT%"
    )

    echo --- Application/Error Events (last 15m, top 30) --- >> "%CRASH_CONTEXT%"
    powershell -NoProfile -Command "Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddMinutes(-15)} -ErrorAction SilentlyContinue | Where-Object { .ProviderName -in @('Application Error','.NET Runtime','Windows Error Reporting') -or .Message -match 'NexaPlay' } | Select-Object -First 30 TimeCreated, Id, LevelDisplayName, ProviderName, Message | Format-List | Out-String -Width 500" >> "%CRASH_CONTEXT%"

    echo.
    echo [INFO] Crash context saved to: %CRASH_CONTEXT%
    echo [INFO] Crash log at: %CRASH_LOG%
    if exist "%APP_LOG%" echo [INFO] App log at: %APP_LOG%

    if exist "%CRASH_LOG%" (
        echo.
        echo --- Last crash entry ---
        powershell -NoProfile -Command "Get-Content '%CRASH_LOG%' -Tail 15"
    )
) else (
    echo.
    echo [DONE] NexaPlay exited normally (code 0).
)

echo.
pause
exit /b %EXIT_CODE%
