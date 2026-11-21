# Catatan API cek resi RajaOngkir (Komerce)

API cek resi yang dipakai KeranjangHijau sejak milestone M2.4. Kodenya ada di
`src/KeranjangHijau.Infrastructure/Tracking/RajaOngkirCourierApiAdapter.cs`.

> API key **tidak** ditulis di sini atau di file mana pun di repo. Simpan di file `.env`
> (`KH_CourierApi__ApiKey=...`), yang sudah masuk `.gitignore`.

## Fakta yang sudah diuji langsung

| Hal | Nilai |
| --- | --- |
| Paket | Starter (gratis), kuota 100 request/hari |
| Endpoint | `POST https://rajaongkir.komerce.id/api/v1/track/waybill` |
| Autentikasi | Header `key: <API key>` |
| Parameter | `awb` (nomor resi), `courier` (kode kurir), `last_phone_number` (opsional, 5 digit terakhir HP penerima; wajib untuk beberapa kurir) |
| Cara kirim parameter | Query string maupun body `application/x-www-form-urlencoded` sama-sama diterima. Aplikasi memakai **body form**, supaya resi dan nomor HP tidak tercatat di log URL HttpClient. |
| Kurir yang didukung | `jne`, `jnt`, `ninja`, `tiki`, `pos`, `anteraja`, `sap`, `lion`, `wahana`, `first`, `spx` (dari pesan error API). Daftar ini disimpan di `CourierApi:SupportedCouriers`. |
| Endpoint lain | `GET /destination/domestic-destination?search=jakarta&limit=1` → `city_name` `"JAKARTA BARAT"` (huruf besar, kota administratif) |

### Respons error yang sudah didapat

- Resi tidak ditemukan / resi lama → [`rajaongkir-404-invalid-awb.json`](rajaongkir-404-invalid-awb.json)
- Kurir tidak valid → [`rajaongkir-400-invalid-courier.json`](rajaongkir-400-invalid-courier.json)

Cara aplikasi menanganinya:

| Respons | Hasil |
| --- | --- |
| HTTP 404, **atau** HTTP 200 dengan `meta.code = 404` | `null` (tidak ditemukan) → data mock dipakai |
| `data` bernilai `null` | `null` → data mock dipakai |
| HTTP 400, **atau** HTTP 200 dengan `meta.code = 400` | `CourierApiException` → data mock dipakai |
| HTTP/`meta.code` 401, 403, 429, 5xx | `CourierApiException` → data mock dipakai |
| JSON rusak, atau kota asal/tujuan kosong | `CourierApiException` ("Respons API cek resi tidak valid.") → data mock dipakai |
| Kurir di luar daftar didukung (mis. `sicepat`) | Tidak ada request sama sekali (hemat kuota) → data mock dipakai |

### Respons sukses (belum pernah didapat)

[`rajaongkir-contoh-dokumentasi.json`](rajaongkir-contoh-dokumentasi.json) adalah contoh **buatan** berisi
data palsu, disusun dari struktur di dokumentasi resmi:

- `data.delivered`
- `data.summary`: `courier_code`, `courier_name`, `waybill_number`, `service_code`, `waybill_date`,
  `shipper_name`, `receiver_name`, `origin`, `destination`, `status`
- `data.details`: `waybill_number`, `waybill_date`, `waybill_time`, `weight`, `origin`, `destination`,
  `shipper_name`, `shipper_address1-3`, `shipper_city`, `receiver_name`, `receiver_address1-3`, `receiver_city`
- `data.delivery_status`, `data.manifest`

Field yang dibaca aplikasi (hanya ini; nama dan alamat **tidak pernah** dibaca, disimpan, atau dicatat di log):

| Data | Urutan sumber (nilai tidak kosong pertama) |
| --- | --- |
| Kota asal | `details.origin` → `summary.origin` → `details.shipper_city` |
| Kota tujuan | `details.destination` → `summary.destination` → `details.receiver_city` |
| Layanan | `summary.service_code` (boleh kosong, dianggap reguler) |
| Berat | `details.weight` lewat `WeightParser` (angka atau teks; tanpa satuan = kg; `"1000 gram"` = 1 kg) |

Nama kota dari API dicocokkan ke tabel `cities` lewat `CityNameMatcher`: cocok persis → tanpa awalan
`KOTA ADM.`/`KOTA`/`KAB.`/`KABUPATEN` dan tanpa bagian setelah koma → nama kota sebagai kata utuh
(`"JAKARTA BARAT"` → Jakarta).

## Hal yang belum pasti

1. **Format `weight`**: angka atau teks? Satuan kg atau gram? Jika API mengirim gram **tanpa satuan**
   (mis. `"1000"`), aplikasi akan membacanya sebagai 1000 kg dan menolaknya (maks. 100 kg). Perlu
   dicek di respons asli; jika benar gram, ubah `RajaOngkirCourierApiAdapter.Weight`.
2. **Format `origin`/`destination`**: nama kota saja, kota + provinsi, kode cabang (mis. `CGK10000`),
   atau nama kecamatan? `CityNameMatcher` hanya menangani nama kota. Kode cabang atau nama kecamatan
   akan gagal dicocokkan dan pengguna mendapat pesan "Kota asal ... belum terdaftar di sistem."
3. **Status HTTP error**: apakah "Invalid Awb" dan kurir tidak valid dikirim dengan HTTP 404/400 atau
   HTTP 200? Keduanya sudah ditangani, tetapi perlu dicatat di sini setelah diketahui.
4. **Isi `service_code`**: apakah memakai kode yang sama dengan kata kunci ekspres di
   `Emission:ExpressKeywords` (mis. `YES`, `BEST`)?

## Cara mengambil contoh respons asli

Lakukan saat ada resi baru (paket yang masih bisa dilacak). Setiap percobaan memakai 1 kuota.

1. Pastikan `.env` berisi `KH_CourierApi__ApiKey`.
2. Kirim request dari terminal (nilai key diambil dari `.env`, tidak diketik di perintah):

   ```bash
   set -a; . ./.env; set +a
   curl -s -X POST "https://rajaongkir.komerce.id/api/v1/track/waybill" \
     -H "key: $KH_CourierApi__ApiKey" \
     --data-urlencode "awb=NOMOR_RESI" \
     --data-urlencode "courier=jne" \
     -o /tmp/respons-asli.json -w "HTTP %{http_code}\n"
   ```

   Tambahkan `--data-urlencode "last_phone_number=12345"` jika kurir memintanya. Catat kode HTTP
   yang tercetak (menjawab poin 3 di atas).
3. **Samarkan data pribadi sebelum disimpan di repo (repo ini publik):**
   - ganti semua nama (`shipper_name`, `receiver_name`, `pod_receiver`, dll.) dengan `"NAMA DISAMARKAN"`;
   - ganti semua alamat (`shipper_address1-3`, `receiver_address1-3`) dengan `"ALAMAT DISAMARKAN"`;
   - ganti nomor resi asli dengan nomor palsu (mis. `CONTOH000001`);
   - hapus nomor HP, kode pos lengkap, dan keterangan manifest yang menyebut nama orang.
   Biarkan `origin`, `destination`, `*_city`, `service_code`, dan `weight` apa adanya: justru itu yang
   ingin diketahui.
4. Simpan sebagai `docs/api-samples/rajaongkir-sukses-<kurir>.json`, baca ulang seluruh isinya, lalu
   tambahkan test di `RajaOngkirCourierApiAdapterTests` yang membaca file itu.
5. Perbarui bagian "Hal yang belum pasti" di atas.
