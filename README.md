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

## Build dan test

```bash
dotnet build
dotnet test
```

## Class Diagram
<img width="756" height="752" alt="carbon_calculator" src="https://github.com/user-attachments/assets/f3c29888-aee2-482f-bc79-2099f15d17c7" />
