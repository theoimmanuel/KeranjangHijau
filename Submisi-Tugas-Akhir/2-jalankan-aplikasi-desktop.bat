@echo off
chcp 65001 >nul
title KeranjangHijau - Aplikasi Desktop Windows

echo =====================================================================
echo           MEMBUKA APLIKASI DESKTOP KERANJANG HIJAU
echo =====================================================================
echo.
echo Membuka aplikasi Windows Forms + WebView2...
echo.

cd /d "%~dp0Aplikasi-Desktop"
start "" "KeranjangHijau.exe"

exit /b 0
