@echo off
chcp 65001 >nul
title KeranjangHijau - Layanan REST Web API

echo =====================================================================
echo            MENJALANKAN LAYANAN WEB API KERANJANG HIJAU
echo =====================================================================
echo.
echo URL API         : http://localhost:5000
echo Swagger UI Docs : http://localhost:5000/swagger
echo.
echo Membuka browser ke Swagger UI...
start "" "http://localhost:5000/swagger"
echo.
echo [INFO] Menjalankan server Web API (Tekan Ctrl+C untuk menghentikan)...
echo.

cd /d "%~dp0Layanan-WebAPI"
"KeranjangHijau.WebApi.exe" --urls "http://localhost:5000"

pause
