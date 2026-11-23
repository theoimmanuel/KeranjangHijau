# Panduan Penggunaan & Konfigurasi — KeranjangHijau

Selamat datang di panduan resmi aplikasi **KeranjangHijau**. Dokumen ini dirancang untuk memudahkan dosen penguji, asisten praktikum, dan pengguna umum dalam mengonfigurasi basis data, menjalankan aplikasi desktop, memanfaatkan layanan Web API, serta memahami alur fitur *Climate Action* yang tersedia.

---

## 1. Tentang KeranjangHijau

**KeranjangHijau** adalah platform aksi iklim (*Climate Action* — SDG 13) yang menghitung estimasi jejak emisi karbon ($kg\ CO_2e$) dari pengiriman paket belanja *online* di Indonesia berdasarkan nomor resi pengiriman.

### Fitur Utama:
1. **Pelacakan & Kalkulasi Emisi Resi:** Mengintegrasikan rute kota asal ➔ kota tujuan, jarak geodesik (*Haversine*), bobot paket, dan moda transportasi (darat/udara) untuk menghitung emisi secara presisi.
2. **Konversi Dampak Nyata:** Mengonversi angka emisi teknis menjadi padanan jarak naik motor ($km$), naik mobil ($km$), dan kebutuhan pohon penyerap emisi.
3. **Gamifikasi Hutan Virtual:** Setiap paket belanja menurunkan skor kesehatan hutan virtual pengguna ($0–100\%$), dengan mekanisme pemulihan alami otomatis sebesar **+10 poin setiap hari**.
4. **Laporan Jejak Karbon Berkala:** Menampilkan visualisasi tren emisi (Mingguan, Bulanan, Semua Waktu) beserta tabel riwayat pengiriman.
5. **Ekspor Instagram Story Card:** Membuat kartu infografis vertikal (9:16) bertema *green-modern* yang siap dibagikan ke media sosial.
6. **Layanan REST Web API:** Menyediakan antarmuka OpenAPI / Swagger UI untuk integrasi pihak ketiga, sistem *e-commerce*, atau aplikasi mobile.
7. **Panel Pemantauan Administrator:** Monitoring seluruh statistik emisi pengguna dan distribusi armada kurir.

---

## 2. Struktur Paket Submisi

Di dalam folder `Submisi-Tugas-Akhir/` terdapat struktur file yang sudah dikemas siap pakai:

```
Submisi-Tugas-Akhir/
│
├── 1-setup-database.bat                # Skrip 1-klik inisialisasi basis data PostgreSQL
├── 2-jalankan-aplikasi-desktop.bat     # Skrip 1-klik menjalankan aplikasi desktop
├── 3-jalankan-web-api.bat              # Skrip 1-klik menjalankan REST Web API & Swagger UI
├── 4-jalankan-pengujian.bat            # Skrip 1-klik eksekusi unit test (897 test cases)
│
├── PANDUAN_PENGGUNAAN.md               # Buku panduan lengkap (dokumen ini)
├── DOKUMEN_TESTING_MODUL_10.md         # Laporan pengujian fungsional & modul 10
├── README.md                           # Ringkasan cepat submisi tugas akhir
│
├── Aplikasi-Desktop/                   # Biner rilis aplikasi desktop Windows
│   ├── KeranjangHijau.exe              # Executable utama Windows Forms + WebView2
│   ├── index.html                      # Antarmuka web modern (Poppins font)
│   ├── Form.css                        # Styling Climate Action
│   └── MockData/mock-resi.json         # Data resi bawaan untuk pengujian instan
│
├── Layanan-WebAPI/                     # Biner rilis ASP.NET Core Web API (.NET 10)
│   └── KeranjangHijau.WebApi.exe       # Server REST API
│
└── BasisData/                          # Skrip DDL & DML PostgreSQL
    ├── docker-compose.yml              # Konfigurasi container PostgreSQL 16
    ├── .env.example                    # Template environment variable
    ├── schema.sql                      # Struktur tabel relasional
    ├── seed.sql                        # Data awal kurir, kota, faktor emisi, & admin
    └── reset-db.ps1                    # Skrip migrasi database
```

---

## 3. Prasyarat Sistem

* **Sistem Operasi:** Windows 10 / 11 (64-bit).
* **Runtime:** .NET 10 Desktop Runtime / SDK (tersedia bawaan jika menggunakan Visual Studio 2026/2022).
* **WebView2 Runtime:** Microsoft Edge WebView2 (terpasang secara standar di Windows 10/11).
* **Basis Data:** Docker Desktop (opsional jika ingin PostgreSQL container lokal) atau PostgreSQL 16 terpasang lokal di port 5432.
  > *Catatan:* Aplikasi memiliki fitur **Mode Simulasi Mandiri (Offline Fallback)**, sehingga aplikasi dan Web API tetap dapat dijalankan dan diuji fungsionalitasnya secara penuh meskipun PostgreSQL belum diaktifkan!

---

## 4. Panduan Menjalankan Aplikasi

### Langkah A: Inisialisasi Basis Data (Opsional / Direkomendasikan)
1. Pastikan Docker Desktop sedang aktif di komputer Anda.
2. Klik ganda pada berkas:
   ```
   1-setup-database.bat
   ```
3. Skrip otomatis akan:
   * Menyalakan container PostgreSQL 16 di port `5432`.
   * Menjalankan `schema.sql` (pembuatan tabel).
   * Menjalankan `seed.sql` (pengisian data master kurir, koordinat kota se-Indonesia, faktor emisi, dan akun admin demo).
4. Selesai! Basis data siap digunakan.

---

### Langkah B: Menjalankan Aplikasi Desktop
1. Klik ganda pada berkas:
   ```
   2-jalankan-aplikasi-desktop.bat
   ```
   *(Atau buka langsung file `Aplikasi-Desktop/KeranjangHijau.exe`).*
2. Jendela aplikasi akan terbuka dengan tampilan modern beresolusi 1200×780 piksel.
3. Perhatikan indikator di kanan atas:
   * `🟢 PostgreSQL 16 Terhubung`: Aplikasi terhubung ke database aktif.
   * `🟡 Mode Simulasi (DB Offline)`: Aplikasi berjalan mandiri tanpa database (tetap bisa menghitung emisi dan mencoba seluruh fitur).

---

### Langkah C: Menjalankan Layanan REST Web API
1. Klik ganda pada berkas:
   ```
   3-jalankan-web-api.bat
   ```
2. Jendela konsol server akan aktif dan peramban default Anda otomatis membuka halaman dokumentasi interaktif **Swagger UI**:
   ```
   http://localhost:5000/swagger
   ```
3. Anda dapat langsung menguji setiap endpoint secara interaktif (*Try it out*).

---

## 5. Panduan Fitur & Skenario Pengujian Aplikasi

### A. Akun Demo Bawaan
Untuk memudahkan pengujian autentikasi dan peran (RBAC):

| Peran (*Role*) | Alamat Email | Kata Sandi | Keterangan |
|---|---|---|---|
| **Administrator** | `admin@keranjanghijau.local` | `AdminKH2026!` | Memiliki akses ke **Panel Admin** sistem |
| **Pengguna Biasa** | `theo@keranjanghijau.local` | `UserKH2026!` | Akun pengguna belanja online |

*(Anda juga dapat mendaftarkan akun baru melalui tab **Daftar** pada menu Masuk).*

---

### B. Menghitung Emisi Paket (Halaman "Cek Emisi")
1. Buka menu **Cek Emisi** di navbar atas.
2. Pilih Kurir (contoh: `JNE Express`).
3. Masukkan Nomor Resi. Anda dapat mengklik tombol *chip* contoh resi untuk pengisian instan:
   * **Contoh 1 (JNE):** `JNE1001000001` (Rute: Jakarta ➔ Bandung, Berat: 1.2 kg)
   * **Contoh 2 (J&T):** `JP1002000001` (Rute: Tangerang ➔ Malang, Berat: 2.5 kg)
   * **Contoh 3 (SiCepat):** `004710030001` (Rute: Bandung ➔ Surakarta, Berat: 0.5 kg)
   * **Contoh 4 (Pos):** `POS2405000001` (Rute: Malang ➔ Jayapura, Berat: 2.0 kg, Moda Udara)
4. Klik tombol **"🔍 Hitung Emisi Karbon"**.
5. Hasil perhitungan komprehensif akan langsung muncul:
   * Angka emisi total dalam $kg\ CO_2e$.
   * Rincian rute, jarak tempuh ($km$), dan moda transportasi.
   * Kartu konversi setara (km motor, km mobil, pohon penyerap).
   * Notifikasi dampak penurunan kesehatan hutan virtual.

---

### C. Gamifikasi Hutan Virtual (Halaman "Hutan Virtual")
1. Buka menu **Hutan Virtual**.
2. Anda akan melihat ilustrasi grafis pohon virtual yang menyesuaikan dengan kondisi kesehatan pengguna.
3. Meteran kesehatan hutan menampilkan skor persentase ($0–100\%$) dan perkiraan jumlah pohon yang tersisa.
4. Tabel **Riwayat Perubahan Hutan (Audit Log)** mencatat detail waktu terjadinya penurunan kesehatan akibat paket belanja maupun peningkatan pemulihan alami harian (**+10 poin per hari**).

---

### D. Laporan Emisi & Ekspor Story (Halaman "Laporan & Riwayat")
1. Buka menu **Laporan & Riwayat**.
2. Pilih filter periode: **Mingguan**, **Bulanan**, atau **Semua Waktu**.
3. Sistem akan menyajikan total emisi, grafik batang tren harian, serta tabel riwayat seluruh paket yang tercatat.
4. Klik tombol **"📸 Buat Kartu Story"**:
   * Pratinjau kartu vertikal Instagram Story (rasio 9:16) akan tampil di layar.
   * Klik **"📋 Salin Ringkasan"** untuk menyalin caption siap unggah ke papan klip (*clipboard*).

---

### E. Panel Administrator (Khusus Akun Admin)
1. Masuk (*login*) menggunakan akun `admin@keranjanghijau.local`.
2. Menu **🛡️ Panel Admin** akan otomatis muncul pada navbar.
3. Administrator dapat memantau:
   * Total pengguna terdaftar, total paket sistem, dan rata-rata emisi per paket.
   * Distribusi persentase emisi antar kurir (JNE, J&T, SiCepat, Pos, dll.).
   * Tabel log audit seluruh paket yang dikirimkan oleh semua pengguna dalam sistem.

---

## 6. Dokumentasi Endpoint REST Web API

Layanan Web API berjalan pada `http://localhost:5000`:

| Metode | Endpoint | Deskripsi |
|---|---|---|
| `GET` | `/api/status` | Mengecek status kesehatan server dan koneksi database |
| `GET` | `/api/tracking/samples` | Mengambil daftar contoh nomor resi pengujian |
| `POST` | `/api/auth/login` | Autentikasi pengguna / administrator |
| `POST` | `/api/auth/register` | Mendaftarkan pengguna baru dan menginisialisasi hutan virtual |
| `POST` | `/api/auth/logout` | Mengakhiri sesi login |
| `POST` | `/api/emissions/calculate` | Menghitung emisi paket dari kode kurir dan nomor resi |
| `GET` | `/api/forest/{userId}` | Membaca skor kesehatan dan status hutan virtual pengguna |
| `GET` | `/api/forest/{userId}/history` | Membaca riwayat perubahan dan pemulihan hutan |
| `GET` | `/api/reports?period={p}` | Mengambil data laporan emisi periode (`Mingguan`, `Bulanan`, `Semua`) |
| `GET` | `/api/admin/summary` | Ringkasan metrik emisi platform untuk administrator |

---

## 7. Kontak & Dukungan Teknis

Aplikasi ini dikembangkan oleh **Kelompok KeranjangHijau (Kelompok 14 - Junior Project)**:
* **Ketua Kelompok:** Amelia Ocha Maharani - 24/534372/TK/59229
* **Anggota 1:** Theo Immanuel Sanyoto - 24/534368/TK/59227
* **Anggota 2:** Juan Christopher Reinaldo Sipayung - 24/544528/TK/60526
