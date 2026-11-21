# Reset database KeranjangHijau: hapus 11 tabel, lalu jalankan schema.sql dan seed.sql.
# PERINGATAN: semua data di tabel tersebut akan hilang.
#
# Pemakaian (PowerShell):  .\scripts\reset-db.ps1
# Koneksi:
#   1. Jika `psql` terpasang  -> memakai variabel KH_DB (dari environment atau file .env).
#   2. Jika tidak             -> memakai psql di dalam container docker compose (service `postgres`).
$ErrorActionPreference = 'Stop'

$Root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $Root

# Muat .env tanpa menimpa variabel yang sudah diset di environment.
$EnvFile = Join-Path $Root '.env'
if (Test-Path $EnvFile) {
    foreach ($line in Get-Content $EnvFile) {
        if ($line -match '^\s*#' -or $line -notmatch '=') { continue }
        $key, $value = $line -split '=', 2
        $key = $key.Trim()
        if (-not [Environment]::GetEnvironmentVariable($key)) {
            [Environment]::SetEnvironmentVariable($key, $value.Trim())
        }
    }
}

$DropSql = 'SET client_min_messages = warning; DROP TABLE IF EXISTS shared_reports, forest_logs, forests, record_equivalences, equivalences, emission_records, emission_factors, shipments, cities, couriers, users CASCADE;'

$UsePsql = [bool](Get-Command psql -ErrorAction SilentlyContinue)
if ($UsePsql) {
    if (-not $env:KH_DB) {
        throw 'KH_DB belum diisi. Set di environment atau di file .env (lihat .env.example).'
    }
} else {
    Write-Host 'psql tidak ditemukan, memakai container docker compose (service postgres).'
    if (-not $env:POSTGRES_USER) { throw 'POSTGRES_USER belum diisi di file .env.' }
    $Db = if ($env:POSTGRES_DB) { $env:POSTGRES_DB } else { 'keranjanghijau' }
}

function Invoke-Sql([string]$Sql) {
    if ($UsePsql) {
        & psql $env:KH_DB -v ON_ERROR_STOP=1 -q -c $Sql
    } else {
        & docker compose exec -T postgres psql -U $env:POSTGRES_USER -d $Db -v ON_ERROR_STOP=1 -q -c $Sql
    }
    if ($LASTEXITCODE -ne 0) { throw "Perintah SQL gagal (exit $LASTEXITCODE)." }
}

function Invoke-SqlFile([string]$Path) {
    if ($UsePsql) {
        & psql $env:KH_DB -v ON_ERROR_STOP=1 -q -f $Path
    } else {
        Get-Content -Raw $Path | & docker compose exec -T postgres psql -U $env:POSTGRES_USER -d $Db -v ON_ERROR_STOP=1 -q
    }
    if ($LASTEXITCODE -ne 0) { throw "File $Path gagal dijalankan (exit $LASTEXITCODE)." }
}

Write-Host '1/3 Menghapus tabel lama...'
Invoke-Sql $DropSql
Write-Host '2/3 Menjalankan database/schema.sql...'
Invoke-SqlFile 'database/schema.sql'
Write-Host '3/3 Menjalankan database/seed.sql...'
Invoke-SqlFile 'database/seed.sql'
Write-Host 'Selesai. Database sudah di-reset.'
