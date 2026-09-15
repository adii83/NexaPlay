@echo off
setlocal EnableExtensions
title NexaPlay Build ^& Run
chcp 65001 >nul

set "ROOT_DIR=%~dp0"
set "PROJECT_FILE=%ROOT_DIR%NexaPlay\NexaPlay.csproj"
set "MSBUILD=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"
set "EXE_PATH=%ROOT_DIR%NexaPlay\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\NexaPlay.exe"
set "APP_DATA_DIR=%LOCALAPPDATA%\NexaPlay"
set "CRASH_LOG=%APP_DATA_DIR%\crash.txt"
set "CRASH_CONTEXT=%ROOT_DIR%nexaplay_crash_context.log"
set "APP_LOG=%APP_DATA_DIR%\nexaplay.log"

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
timeout /t 1 /nobreak >nul 2>&1

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

:: --- Run dan tunggu sampai exit ---
echo [RUN]   Launching NexaPlay...
echo.
start /wait "" "%EXE_PATH%"
set "EXIT_CODE=%ERRORLEVEL%"

if "%EXIT_CODE%"=="0" goto normal_exit

echo.
echo [CRASH] NexaPlay exited with code: %EXIT_CODE%
echo.
call :dump_crash_context
goto finish

:normal_exit
echo.
echo [DONE] NexaPlay exited normally ^(code 0^).

:finish
echo.
pause
exit /b %EXIT_CODE%

:dump_crash_context
> "%CRASH_CONTEXT%" echo ===== Crash Context %DATE% %TIME% =====

if not exist "%CRASH_LOG%" goto skip_crash_log
>> "%CRASH_CONTEXT%" echo --- crash.txt ^(last 60 lines^) ---
powershell -NoProfile -Command "Get-Content -LiteralPath '%CRASH_LOG%' -Tail 60" >> "%CRASH_CONTEXT%"
>> "%CRASH_CONTEXT%" echo.

:skip_crash_log
if not exist "%APP_LOG%" goto skip_app_log
>> "%CRASH_CONTEXT%" echo --- nexaplay.log ^(last 120 lines^) ---
powershell -NoProfile -Command "Get-Content -LiteralPath '%APP_LOG%' -Tail 120" >> "%CRASH_CONTEXT%"
>> "%CRASH_CONTEXT%" echo.

:skip_app_log
>> "%CRASH_CONTEXT%" echo --- Application/Error Events ^(last 15m, top 30^) ---
powershell -NoProfile -Command "Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddMinutes(-15)} -ErrorAction SilentlyContinue | Where-Object { $_.ProviderName -in @('Application Error','.NET Runtime','Windows Error Reporting') -and $_.Message -match 'NexaPlay' } | Select-Object -First 30 TimeCreated, Id, LevelDisplayName, ProviderName, Message | Format-List | Out-String -Width 500" >> "%CRASH_CONTEXT%"

echo [INFO] Crash context saved to: %CRASH_CONTEXT%
if exist "%CRASH_LOG%" echo [INFO] Crash log at: %CRASH_LOG%
if exist "%APP_LOG%" echo [INFO] App log at: %APP_LOG%
if not exist "%CRASH_LOG%" exit /b 0

echo.
echo --- Last crash entry ---
powershell -NoProfile -Command "Get-Content -LiteralPath '%CRASH_LOG%' -Tail 15"
exit /b 0
