# KeranjangHijau — Submisi Tugas Akhir Junior Project

**Kelompok 14 | Teknologi Informasi UGM**

| Nama | NIM | Peran |
|---|---|---|
| Amelia Ocha Maharani | 24/534372/TK/59229 | Ketua Kelompok |
| Theo Immanuel Sanyoto | 24/534368/TK/59227 | Anggota 1 |
| Juan Christopher Reinaldo Sipayung | 24/544528/TK/60526 | Anggota 2 |

---

## Tentang Aplikasi

**KeranjangHijau** adalah platform *Climate Action* (SDG 13) yang menghitung estimasi jejak emisi karbon (kg CO₂e) dari pengiriman paket belanja *online* di Indonesia, berdasarkan nomor resi ekspedisi. Aplikasi memvisualisasikan dampak emisi melalui gamifikasi hutan virtual, laporan berkala, dan kartu Instagram Story yang bisa dibagikan.

**Stack Teknologi:**
- **Frontend:** Windows Forms + Microsoft WebView2 (HTML/CSS/JS modern dalam aplikasi desktop Windows)
- **Backend:** C# .NET 10, Clean Architecture (Core → Infrastructure → Presentation)
- **Database:** PostgreSQL 16 (via Dapper + Npgsql)
- **REST API:** ASP.NET Core Minimal API + Swagger/OpenAPI
- **Testing:** xUnit.net v2 (897 test lulus, 0 gagal)

---

## Cara Paling Cepat Mencoba Aplikasi (< 2 Menit)

> **Aplikasi berjalan tanpa database!** Semua fitur utama dapat dicoba langsung dengan data simulasi bawaan.

### 1. Jalankan Aplikasi Desktop
Klik ganda:
```
2-jalankan-aplikasi-desktop.bat
```
Aplikasi Windows Forms terbuka. Indikator navbar akan menampilkan `🟡 Mode Simulasi (DB Offline)` — semua fitur tetap berjalan penuh.

### 2. Jalankan REST Web API + Swagger UI
Klik ganda:
```
3-jalankan-web-api.bat
```
Browser otomatis membuka: **http://localhost:5000/swagger**

---

## Cara Menjalankan Lengkap dengan Database (Opsional)

### Prasyarat
- Docker Desktop aktif, atau PostgreSQL 16 terpasang di port 5432
- .NET 10 Desktop Runtime ([unduh di microsoft.com/net](https://dotnet.microsoft.com/download/dotnet/10.0))

### Langkah
1. **Setup database** → klik `1-setup-database.bat`
2. **Jalankan aplikasi** → klik `2-jalankan-aplikasi-desktop.bat`
3. **Jalankan API** → klik `3-jalankan-web-api.bat`
4. **Jalankan pengujian** → klik `4-jalankan-pengujian.bat`

---

## Akun Demo

| Peran | Email | Password |
|---|---|---|
| Administrator | `admin@keranjanghijau.local` | `AdminKH2026!` |
| Pengguna Biasa | `theo@keranjanghijau.local` | `UserKH2026!` |

---

## Nomor Resi Contoh untuk Uji Coba

| Kurir | Nomor Resi | Rute | Berat |
|---|---|---|---|
| JNE | `JNE1001000001` | Jakarta → Bandung | 1.2 kg |
| J&T | `JP1002000001` | Tangerang → Malang | 2.5 kg |
| SiCepat | `004710030001` | Bandung → Surakarta | 0.5 kg |
| Anteraja | `10000400000001` | Jakarta → Yogyakarta | 0.7 kg |
| Pos Indonesia | `POS2405000001` | Malang → Jayapura | 2.0 kg (pesawat) |

---

## Struktur Folder Submisi

```
Submisi-Tugas-Akhir/
├── 1-setup-database.bat            ← Inisialisasi PostgreSQL (1 klik)
├── 2-jalankan-aplikasi-desktop.bat ← Buka aplikasi desktop
├── 3-jalankan-web-api.bat          ← Jalankan API + buka Swagger UI
├── 4-jalankan-pengujian.bat        ← Jalankan 985 test otomatis
│
├── README.md                       ← Dokumen ini (baca pertama)
├── PANDUAN_PENGGUNAAN.md           ← Panduan lengkap semua fitur
├── DOKUMEN_TESTING_MODUL_10.md     ← Laporan pengujian format Modul 10
│
├── Aplikasi-Desktop/               ← Biner desktop Windows (siap pakai)
│   ├── KeranjangHijau.exe
│   ├── index.html
│   └── ...
│
├── Layanan-WebAPI/                 ← Biner REST Web API (siap pakai)
│   └── KeranjangHijau.WebApi.exe
│
└── BasisData/                      ← Skrip SQL & konfigurasi Docker
    ├── schema.sql
    ├── seed.sql
    └── docker-compose.yml
```

---

## Ringkasan Hasil Pengujian

| Jenis Pengujian | Jumlah | Lulus | Gagal |
|---|---|---|---|
| Unit Test (xUnit) | 897 | **897** | 0 |
| Integration Test DB (skipped, butuh DB aktif) | 88 | — | — |
| Black-Box Fungsional (Modul 10) | 25 | **25** | 0 |
| **Total** | **985** | **922** | **0** |

Lihat detail di `DOKUMEN_TESTING_MODUL_10.md`.
