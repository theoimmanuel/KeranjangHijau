@echo off
chcp 65001 >nul
title KeranjangHijau - Inisialisasi Basis Data PostgreSQL

echo =====================================================================
echo       KERANJANG HIJAU - SETUP BASIS DATA POSTGRESQL 16
echo =====================================================================
echo.

cd /d "%~dp0BasisData"

if not exist ".env" (
    echo [INFO] Menyalin konfigurasi .env.example menjadi .env...
    copy .env.example .env >nul
)

echo [INFO] Menyalakan container PostgreSQL via Docker Compose...
docker compose up -d

if %errorlevel% neq 0 (
    echo.
    echo [PERINGATAN] Docker tidak aktif atau terjadi kesalahan saat docker compose.
    echo Pastikan Docker Desktop sudah berjalan di komputer Anda.
    echo Jika menggunakan PostgreSQL manual, pastikan port 5432 aktif dan jalankan schema.sql + seed.sql.
    echo.
    pause
    exit /b %errorlevel%
)

echo.
echo [INFO] Menunggu database siap menerima koneksi (5 detik)...
timeout /t 5 /nobreak >nul

echo [INFO] Menjalankan migrasi schema.sql dan data awal seed.sql...
powershell -NoProfile -ExecutionPolicy Bypass -File "reset-db.ps1"

echo.
echo =====================================================================
echo [SUKSES] Basis Data PostgreSQL KeranjangHijau Berhasil Diinisialisasi!
echo.
echo Host      : localhost
echo Port      : 5432 (default)
echo Database  : keranjanghijau
echo Username  : keranjanghijau
echo Password  : password
echo.
echo Akun Admin Demo:
echo Email     : admin@keranjanghijau.local
echo Password  : AdminKH2026!
echo =====================================================================
echo.
pause
