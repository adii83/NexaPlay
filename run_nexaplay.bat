@echo off
title NexaPlay Runner
echo ==================================================
echo   NexaPlay Build ^& Run Script
echo ==================================================
echo.

echo [INFO] Menambahkan .NET SDK ke PATH...
set PATH=%PATH%;C:\Program Files\dotnet\

echo [INFO] Menjalankan NexaPlay (Unpackaged x64)...
echo [INFO] Log eksekusi akan ditampilkan di sini dan disimpan ke "nexaplay_run.log".
echo.

powershell -Command "dotnet run --project 'D:\My Project\NexaPlay\NexaPlay\NexaPlay.csproj' -c Debug -p:Platform=x64 --launch-profile 'NexaPlay (Unpackaged)' 2>&1 | Tee-Object -FilePath 'nexaplay_run.log'"

echo.
echo [INFO] Proses selesai. Jika gagal terbuka, periksa isi file nexaplay_run.log.
pause
