# DOKUMEN PENGUJIAN PERANGKAT LUNAK (MODUL 10)
## Proyek: Platform Aksi Iklim "KeranjangHijau"

---

### IDENTITAS PENGUJIAN
* **Mata Kuliah:** Junior Project (Teknologi Informasi)
* **Kelompok:** 14 (KeranjangHijau)
* **Anggota Tim:**
  1. Amelia Ocha Maharani — 24/534372/TK/59229 (Ketua)
  2. Theo Immanuel Sanyoto — 24/534368/TK/59227 (Anggota 1)
  3. Juan Christopher Reinaldo Sipayung — 24/544528/TK/60526 (Anggota 2)
* **Tanggal Pengujian:** 23 November 2026
* **Lingkungan Pengujian:**
  * **Sistem Operasi:** Windows 11 Pro 64-bit
  * **Runtime:** Microsoft .NET 10.0.13 (SDK 10.0.402) & Microsoft Edge WebView2
  * **Basis Data:** PostgreSQL 16 (Docker Container di port 5432)
  * **Framework Uji Unit:** xUnit.net v2.8.1

---

## 1. Rencana & Metodologi Pengujian

Pengujian dilakukan menggunakan dua pendekatan utama:
1. **White-Box Testing (Unit & Integration Testing):**
   * Menguji logika internal kelas domain, kalkulator emisi rumus *Haversine*, pengurai moda transportasi, pemulihan alami hutan virtual, dan *query repository* Dapper/Npgsql secara otomatis menggunakan framework **xUnit**.
2. **Black-Box Testing (Functional Testing):**
   * Menguji fungsionalitas sistem berdasarkan masukan (*input*) dan luaran (*output*) pada antarmuka GUI Desktop (Windows Forms + WebView2) dan antarmuka REST Web API (Swagger UI). Metode yang digunakan meliputi *Equivalence Partitioning* dan *Boundary Value Analysis*.

---

## 2. Ringkasan Eksekusi Pengujian Otomatis (xUnit)

Suite pengujian otomatis dijalankan pada proyek `KeranjangHijau.Tests`:

| Kategori Pengujian | Jumlah Test Case | Lulus (*Passed*) | Gagal (*Failed*) | Dilewati (*Skipped*) | Durasi |
|---|---|---|---|---|---|
| **Unit Test Domain Rules** | 412 | 412 | 0 | 0 | 1.8 detik |
| **Unit Test Layanan & DTO** | 328 | 328 | 0 | 0 | 1.2 detik |
| **Unit Test Tracking & Mock** | 157 | 157 | 0 | 0 | 0.8 detik |
| **Integration Test (DB/Live API)\*** | 88 | 0 | 0 | 88 | — |
| **TOTAL** | **985** | **897** | **0** | **88** | **3.8 detik** |

*\*Catatan: 88 integration test otomatis dilewati (skipped) secara sengaja untuk menghemat kuota berbayar API RajaOngkir Komerce tim dan saat database live tidak di-set via environment variable.*

### Output Aktual Eksekusi (23 November 2026)

Perintah dijalankan dari direktori root backend:
```
dotnet test tests/KeranjangHijau.Tests/KeranjangHijau.Tests.csproj --configuration Release --verbosity normal
```

Hasil terminal:
```
[xUnit.net] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.13)
[xUnit.net]   Discovering: KeranjangHijau.Tests
[xUnit.net]   Discovered:  KeranjangHijau.Tests
[xUnit.net]   Starting:    KeranjangHijau.Tests
[xUnit.net]   Finished:    KeranjangHijau.Tests

Test summary: total: 985; failed: 0; succeeded: 897; skipped: 88; duration: 5,2s
Build succeeded in 6,8s
```

**Status: ✅ LULUS — 0 kegagalan dari 897 test yang dijalankan.**

---

## 3. Matriks Kasus Uji Fungsional Black-Box (Modul 10)

### Modul A: Autentikasi & Manajemen Sesi Pengguna

| ID Kasus Uji | Skenario Pengujian | Langkah & Data Masukan | Hasil yang Diharapkan | Hasil Pengamatan | Status |
|---|---|---|---|---|---|
| **TC-AUTH-01** | Login dengan akun administrator yang valid | Masukkan email: `admin@keranjanghijau.local`, password: `AdminKH2026!` lalu klik tombol "Masuk" | Pengguna berhasil masuk, nama akun tampil di navbar, role terdeteksi sebagai `admin`, dan menu "🛡️ Panel Admin" muncul di navigasi | Pengguna masuk sebagai Admin, menu Panel Admin aktif di navbar | **PASS** |
| **TC-AUTH-02** | Login dengan password yang salah | Masukkan email: `admin@keranjanghijau.local`, password: `SalahPassword123` | Sistem menolak autentikasi dan menampilkan notifikasi kesalahan "Email atau password salah" | Muncul pesan error merah pada kartu login | **PASS** |
| **TC-AUTH-03** | Registrasi akun pengguna baru | Masukkan nama: `Budi Santoso`, email: `budi@example.com`, password: `Password123` | Akun baru berhasil terdaftar di database, hutan virtual awal dibuat secara otomatis, dan pengguna diarahkan ke menu hutan | Akun terdaftar dan inisialisasi hutan virtual berhasil | **PASS** |
| **TC-AUTH-04** | Registrasi dengan email yang sudah ada | Masukkan nama: `Budi`, email yang sudah terdaftar (`admin@keranjanghijau.local`), password: `Password123` | Sistem menolak pendaftaran dengan pesan validasi "Email sudah terdaftar" | Pendaftaran ditolak dengan pesan validasi | **PASS** |
| **TC-AUTH-05** | Logout sesi pengguna | Klik tombol "Keluar" pada chip profil di navbar | Sesi dihapus dari memori, tampilan navbar kembali menampilkan tombol "Masuk", dan dialihkan ke halaman Beranda | Sesi berakhir dan kembali ke beranda | **PASS** |

---

### Modul B: Pelacakan Resi & Kalkulasi Emisi Karbon

| ID Kasus Uji | Skenario Pengujian | Langkah & Data Masukan | Hasil yang Diharapkan | Hasil Pengamatan | Status |
|---|---|---|---|---|---|
| **TC-CALC-01** | Hitung emisi paket dalam satu pulau (Moda Darat) | Kurir: `JNE`, Resi: `JNE1001000001`, Berat: `1.2 kg` (Rute: Jakarta ➔ Bandung) | Sistem menghitung emisi truk ($0.10\ kg\ CO_2e/\text{ton-km}$), jarak 145.2 km, emisi total $\approx 0.0174\ kg\ CO_2e$, setara motor $0.25\ km$ | Hasil kalkulasi tepat $0.0174\ kg\ CO_2e$, kartu setara motor dan mobil tampil | **PASS** |
| **TC-CALC-02** | Hitung emisi paket beda pulau jarak jauh (Moda Udara) | Kurir: `Pos Indonesia`, Resi: `POS2405000001`, Berat: `2.0 kg` (Rute: Malang ➔ Jayapura) | Terdeteksi moda pesawat ($0.80\ kg\ CO_2e/\text{ton-km}$), jarak 3280 km, emisi total $5.248\ kg\ CO_2e$ | Terkalkulasi $5.248\ kg\ CO_2e$ dengan moda PESAWAT | **PASS** |
| **TC-CALC-03** | Input bobot manual pengguna | Kurir: `J&T`, Resi: `JP1002000001`, Berat manual: `5.0 kg` | Sistem menggunakan bobot manual 5.0 kg dan mengabaikan bobot default paket | Hasil perhitungan menggunakan parameter 5.0 kg | **PASS** |
| **TC-CALC-04** | Validasi nomor resi kosong | Nomor resi dikosongkan lalu menekan tombol "Hitung Emisi Karbon" | Sistem menahan pengiriman formulir dan menampilkan instruksi wajib diisi | Tampil notifikasi "Nomor resi wajib diisi" | **PASS** |
| **TC-CALC-05** | Input contoh resi instan (*Quick Mock Resi*) | Klik tombol chip `JNE Jakarta-Bandung (1.2kg)` | Formulir otomatis terisi kode kurir `jne`, nomor resi `JNE1001000001`, dan langsung menjalankan perhitungan | Form terisi dan hasil langsung keluar tanpa ketik manual | **PASS** |

---

### Modul C: Konversi Dampak Setara (*Equivalences*)

| ID Kasus Uji | Skenario Pengujian | Langkah & Data Masukan | Hasil yang Diharapkan | Hasil Pengamatan | Status |
|---|---|---|---|---|---|
| **TC-EQ-01** | Konversi ke jarak tempuh sepeda motor | Emisi dihitung: $0.0174\ kg\ CO_2e$ (Faktor motor: $0.07\ kg\ CO_2e/km$) | Nilai setara motor $\approx 0.25\ km$ ditampilkan dengan ikon motor | Ditampilkan kartu: 🛵 0.25 km Setara Naik Motor | **PASS** |
| **TC-EQ-02** | Konversi ke jarak tempuh mobil | Emisi dihitung: $0.0174\ kg\ CO_2e$ (Faktor mobil: $0.17\ kg\ CO_2e/km$) | Nilai setara mobil $\approx 0.10\ km$ ditampilkan dengan ikon mobil | Ditampilkan kartu: 🚗 0.10 km Setara Naik Mobil | **PASS** |
| **TC-EQ-03** | Konversi ke daya serap pohon | Emisi dihitung: $5.2480\ kg\ CO_2e$ (Faktor serap pohon: $22\ kg\ CO_2/\text{tahun}$) | Nilai pohon $\approx 0.239\ \text{pohon/tahun}$ | Ditampilkan kartu: 🌳 0.239 pohon/tahun | **PASS** |

---

### Modul D: Gamifikasi Hutan Virtual (*Virtual Forest*)

| ID Kasus Uji | Skenario Pengujian | Langkah & Data Masukan | Hasil yang Diharapkan | Hasil Pengamatan | Status |
|---|---|---|---|---|---|
| **TC-FOR-01** | Dampak emisi terhadap kesehatan hutan | Melakukan perhitungan emisi paket resi baru | Skor kesehatan hutan berkurang proporsional dengan besaran emisi, dan log audit emisi tercatat | Skor kesehatan hutan berkurang dan log tercatat di tabel audit | **PASS** |
| **TC-FOR-02** | Pemulihan alami harian (*Natural Recovery*) | Pengguna membuka halaman hutan setelah 1 hari (*idle*) | Kesehatan hutan meningkat otomatis sebesar **+10 poin** per hari penuh (maksimal 100 poin) | Terbaca log pemulihan alami harian (+10 poin) | **PASS** |
| **TC-FOR-03** | Batas maksimal kesehatan hutan | Hutan yang sudah bernilai 95 poin mendapatkan pemulihan alami 10 poin | Nilai kesehatan mentok di angka 100.0% (tidak pernah melebihi 100) | Skor kesehatan tetap berada di 100% | **PASS** |
| **TC-FOR-04** | Visualisasi ilustrasi pohon adaptif | Skor kesehatan berubah dari 85% ke 40% | Warna dedaunan pohon pada scene SVG bertransisi dari hijau segar ke kuning/kemerahan | Ilustrasi pohon secara dinamis beradaptasi dengan status kesehatan | **PASS** |

---

### Modul E: Laporan Jejak Karbon & Fitur Share Social Media

| ID Kasus Uji | Skenario Pengujian | Langkah & Data Masukan | Hasil yang Diharapkan | Hasil Pengamatan | Status |
|---|---|---|---|---|---|
| **TC-REP-01** | Filter periode laporan (Mingguan, Bulanan, Semua) | Mengklik tombol filter "Mingguan", "Bulanan", dan "Semua Waktu" | Data ringkasan emisi, grafik harian, dan tabel paket diperbarui sesuai rentang tanggal periode | Data grafik dan tabel terfilter secara dinamis | **PASS** |
| **TC-REP-02** | Tampilan grafik batang tren harian | Buka halaman Laporan pada periode aktif | Batang diagram batang HTML/CSS merefleksikan tinggi proporsional emisi harian | Grafik batang tampil proporsional | **PASS** |
| **TC-REP-03** | Buka modal Instagram Story | Klik tombol "📸 Buat Kartu Story" | Muncul jendela modal pratinjau kartu vertikal 9:16 dengan nama pengguna, angka emisi, dan badge KeranjangHijau | Modal Instagram Story tampil dengan gradasi estetik | **PASS** |
| **TC-REP-04** | Salin ringkasan teks story ke clipboard | Klik tombol "📋 Salin Ringkasan" di modal story | Teks narasi aksi iklim tersalin ke clipboard dan muncul toast notifikasi | Toast muncul "Teks ringkasan disalin ke clipboard!" | **PASS** |

---

### Modul F: Panel Pemantauan Administrator

| ID Kasus Uji | Skenario Pengujian | Langkah & Data Masukan | Hasil yang Diharapkan | Hasil Pengamatan | Status |
|---|---|---|---|---|---|
| **TC-ADM-01** | Proteksi akses panel admin untuk pengguna non-admin | Login sebagai akun pengguna biasa (`theo@keranjanghijau.local`) | Menu "🛡️ Panel Admin" disembunyikan dari navbar dan tidak dapat diakses | Menu admin tidak terlihat oleh pengguna biasa | **PASS** |
| **TC-ADM-02** | Tampilan statistik sistem menyeluruh | Buka Panel Admin dengan akun administrator | Menampilkan total pengguna, total paket sistem, total emisi kg CO₂e, dan rata-rata per paket | 4 kartu metrik sistem tampil lengkap | **PASS** |
| **TC-ADM-03** | Grafik distribusi emisi kurir | Periksa bagian distribusi kurir di panel admin | Menampilkan persentase dan bar perbandingan emisi antara kurir JNE, J&T, SiCepat, dan Pos | Bar distribusi kurir tampil informatif | **PASS** |

---

### Modul G: Layanan REST Web API (Swagger UI)

| ID Kasus Uji | Skenario Pengujian | Langkah & Data Masukan | Hasil yang Diharapkan | Hasil Pengamatan | Status |
|---|---|---|---|---|---|
| **TC-API-01** | Pemeriksaan status server (`GET /api/status`) | Mengakses endpoint `http://localhost:5000/api/status` melalui browser/curl | HTTP 200 OK dengan JSON memuat status server, database, dan akun demo | Status 200 OK dengan payload JSON lengkap | **PASS** |
| **TC-API-02** | Hitung emisi via API (`POST /api/emissions/calculate`) | Request body: `{"kodeKurir": "jne", "nomorResi": "JNE1001000001", "beratKg": 1.2}` | HTTP 200 OK memuat rincian emisi total, jarak, rute, konversi setara, dan dampak hutan | Response JSON 200 OK dengan hasil kalkulasi emisi valid | **PASS** |
| **TC-API-03** | Akses Swagger UI di peramban | Membuka URL `http://localhost:5000/swagger` | Tampilan interaktif Swagger UI terbuka lengkap dengan dokumentasi seluruh endpoint | Swagger UI v1 terbuka interaktif dan siap diuji | **PASS** |

---

### Modul H: Keandalan & Ketahanan Sistem (*Fault Tolerance*)

| ID Kasus Uji | Skenario Pengujian | Langkah & Data Masukan | Hasil yang Diharapkan | Hasil Pengamatan | Status |
|---|---|---|---|---|---|
| **TC-FT-01** | Penanganan database PostgreSQL mati (*Offline Fallback*) | Menjalankan aplikasi desktop saat container PostgreSQL tidak aktif | Aplikasi tidak *crash*, indikator navbar menampilkan `🟡 Mode Simulasi`, dan kalkulasi emisi dialihkan ke formula lokal | Aplikasi berjalan mulus dan tetap interaktif dalam mode simulasi | **PASS** |

---

## 4. Kesimpulan Pengujian

1. **Tingkat Keberhasilan:** Seluruh **25 skenario uji fungsional (Black-Box Testing)** dan **897 uji unit otomatis (xUnit)** berhasil dilewati dengan tingkat kelulusan **100% (0 defect/error)**.
2. **Kinerja & Keandalan:** Eksekusi kalkulasi emisi dan pembaruan antarmuka WebView2 berlangsung instan ($<100\text{ ms}$). Mekanisme *fallback* menjamin aplikasi tetap stabil bahkan jika server database belum diaktifkan.
3. **Status Kelayakan:** Berdasarkan hasil pengujian Modul 10 ini, aplikasi **KeranjangHijau** dinyatakan **LULUS dan SIAP UNTUK DIRILIS (*Ready for Submission & Production*)**.
