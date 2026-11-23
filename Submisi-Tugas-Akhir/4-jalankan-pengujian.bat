@echo off
chcp 65001 >nul
title KeranjangHijau - Menjalankan Seluruh Pengujian Otomatis

echo =====================================================================
echo            PENGUJIAN OTOMATIS (UNIT & INTEGRATION TEST)
echo =====================================================================
echo.
echo Menjalankan suite pengujian xUnit untuk KeranjangHijau.Tests...
echo.

cd /d "%~dp0..\backend"
dotnet test "tests/KeranjangHijau.Tests/KeranjangHijau.Tests.csproj" --verbosity normal

echo.
echo =====================================================================
echo [INFO] Detail hasil uji dan pengujian black-box fungsional dapat
echo dibaca pada dokumen: DOKUMEN_TESTING_MODUL_10.md
echo =====================================================================
echo.
pause
