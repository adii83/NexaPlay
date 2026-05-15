@echo off
title NexaPlay Runner
echo ==================================================
echo   NexaPlay Build ^& Run Script
echo ==================================================
echo.

echo [INFO] Menambahkan .NET SDK ke PATH...
set PATH=%PATH%;C:\Program Files\dotnet\

echo [INFO] Menjalankan NexaPlay dengan Auto-Restart (Watch Mode)...
echo [INFO] Biarkan terminal ini terbuka. Setiap file disimpan (Ctrl+S), aplikasi akan tertutup dan terbuka ulang otomatis.
echo [INFO] Log eksekusi disimpan ke "nexaplay_run.log".
echo.

powershell -Command "dotnet watch --project 'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' run -c Debug --property:Platform=x64 --launch-profile 'NexaPlay (Unpackaged)' 2>&1 | Tee-Object -FilePath 'nexaplay_run.log'"

echo.
echo [INFO] Proses selesai. Jika gagal terbuka, periksa isi file nexaplay_run.log.
pause
