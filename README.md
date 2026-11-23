# KeranjangHijau

KeranjangHijau adalah aplikasi desktop Windows (C# WPF) bertema Climate Action yang menghitung estimasi emisi karbon (kg CO2e) dari pengiriman paket belanja online berdasarkan nomor resi. Emisi ditampilkan dalam konversi setara (km motor/mobil), memengaruhi kesehatan hutan virtual pengguna, dan dapat dirangkum menjadi laporan yang bisa dibagikan ke Instagram Story.

Kelompok KeranjangHijau

Ketua Kelompok: Amelia Ocha Maharani - 24/534372/TK/59229  

Anggota 1: Theo Immanuel Sanyoto - 24/534368/TK/59227

Anggota 2: Juan Christopher Reinaldo Sipayung - 24/544528/TK/60526  

## Struktur solusi

| Proyek | Isi |
| --- | --- |
| `src/KeranjangHijau.Core` | Model, DTO, interface, service, dan aturan domain (tanpa dependensi DB/HTTP/WPF) |
| `src/KeranjangHijau.Infrastructure` | Akses database (Npgsql + Dapper), tracker resi, keamanan, registrasi DI |
| `tests/KeranjangHijau.Tests` | Unit test xUnit |
| `database/` | `schema.sql` dan `seed.sql` untuk PostgreSQL |

> **Catatan:** proyek WPF `src/KeranjangHijau.App` belum ada di solusi. Proyek ini akan dibuat oleh tim frontend (target `net10.0-windows`, pola MVVM) dan mereferensikan `KeranjangHijau.Infrastructure`.

## Prasyarat

- .NET SDK 10 (cek dengan `dotnet --version`)
- Docker (untuk PostgreSQL 16 lokal) atau database PostgreSQL lain, mis. Supabase
- `psql` (opsional; jika tidak ada, skrip reset memakai `psql` di dalam container)

## Menyiapkan database

```bash
cp .env.example .env            # lalu ganti password dan isi API key; .env tidak ikut di-commit
docker compose up -d            # PostgreSQL 16 di port POSTGRES_PORT (default 5432)
./scripts/reset-db.sh           # hapus tabel, jalankan schema.sql lalu seed.sql
```

Di Windows (PowerShell): `Copy-Item .env.example .env`, `docker compose up -d`, lalu `.\scripts\reset-db.ps1`.

> Jika port 5432 sudah dipakai PostgreSQL lain, ganti `POSTGRES_PORT` (mis. `5433`) dan port di `KH_DB` pada file `.env`.

Setelah menyalin, buka `.env` lalu isi `KH_CourierApi__ApiKey` dengan API key RajaOngkir (Komerce) milik tim, menggantikan teks `ISI_API_KEY_RAJAONGKIR`. Jika dikosongkan, aplikasi selalu memakai data resi mock (`MockData/mock-resi.json`). API key dibagikan lewat jalur pribadi tim, **jangan** lewat repo, issue, atau chat publik.

Selama development, nomor resi yang sama boleh diinput berulang kali. Sebelum rilis, jalankan `database/enable-unique-resi.sql` untuk memasang constraint UNIQUE (kurir, nomor resi).

## Konfigurasi backend

Konfigurasi dibaca berurutan (yang belakang menimpa yang depan):

1. `src/KeranjangHijau.Infrastructure/appsettings.json`: nilai default, **tanpa rahasia**.
2. `src/KeranjangHijau.Infrastructure/appsettings.Local.json`: opsional, tidak di-commit. Salin dari `appsettings.Local.example.json`.
3. File `.env` di root repo: opsional, tidak di-commit. Hanya baris berawalan `KH_` yang dibaca (`POSTGRES_*` untuk docker compose diabaikan), dengan aturan yang sama seperti environment variable. File dicari dari folder kerja lalu naik ke folder induk; lokasinya bisa ditentukan lewat env `KH_ENV_FILE`.
4. Environment variable asli berawalan `KH_`, mis. `KH_Features__AllowDuplicateResi=false` (awalan dibuang, `__` menjadi `:`). Selalu menang atas `.env`.
5. `KH_DB` (dari `.env` atau environment variable): connection string database. Format URI (`postgresql://user:pass@host:5432/db`) dan format Npgsql (`Host=...;Password=...`) sama-sama diterima.

Connection string dan API key asli hanya boleh ada di `.env`, `appsettings.Local.json`, atau environment variable.

API cek resi memakai **RajaOngkir (Komerce)** (`CourierApi:Provider = "RajaOngkir"`, paket gratis 100 request/hari). Kurir yang tidak didukung API (mis. `sicepat`) langsung memakai data mock tanpa memakai kuota. Catatan format respons ada di [docs/api-samples/CATATAN.md](docs/api-samples/CATATAN.md).

## Akun demo

| Peran | Email | Password |
| --- | --- | --- |
| Admin | `admin@keranjanghijau.local` | `AdminKH2026!` |

Akun admin dibuat oleh `database/seed.sql` (hash BCrypt cost 11). Password ini hanya untuk demo lokal; ganti hash-nya jika database dipakai di luar komputer sendiri.

## Build dan test

```bash
dotnet build
dotnet test
```

Integration test database otomatis dilewati jika `KH_DB` tidak di-set. Untuk menjalankannya:

```bash
KH_DB=postgresql://keranjanghijau:password@localhost:5432/keranjanghijau dotnet test
```

`KH_DB` untuk test harus di-set sebagai environment variable asli; `KH_DB` di `.env` tidak membuat integration test berjalan. Test tidak pernah memakai API key asli. Smoke test ke API RajaOngkir asli (memakai 1 kuota) hanya berjalan jika diminta:

```bash
KH_RAJAONGKIR_LIVE=1 dotnet test --filter RajaOngkirLiveTests
```

## Class Diagram
<img width="756" height="752" alt="carbon_calculator" src="https://github.com/user-attachments/assets/f3c29888-aee2-482f-bc79-2099f15d17c7" />
