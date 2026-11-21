-- =============================================================================
-- KeranjangHijau - data awal (seed)
-- Jalankan setelah schema.sql:  psql "$KH_DB" -v ON_ERROR_STOP=1 -f database/seed.sql
--
-- Aman dijalankan ulang: setiap INSERT memakai ON CONFLICT DO NOTHING.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- Kurir
-- -----------------------------------------------------------------------------
INSERT INTO couriers (kode_kurir, nama_kurir) VALUES
    ('jne',      'JNE'),
    ('jnt',      'J&T Express'),
    ('sicepat',  'SiCepat Ekspres'),
    ('anteraja', 'AnterAja'),
    ('pos',      'Pos Indonesia'),
    ('spx',      'Shopee Express')
ON CONFLICT (kode_kurir) DO NOTHING;

-- -----------------------------------------------------------------------------
-- Kota (koordinat pusat kota, pembulatan 4 desimal ~ 11 m)
-- Nilai `pulau` dipakai untuk menebak moda transport (CLAUDE.md 7.3).
-- -----------------------------------------------------------------------------
INSERT INTO cities (nama_kota, provinsi, pulau, latitude, longitude) VALUES
    -- Jawa
    ('Jakarta',        'DKI Jakarta',        'Jawa',        -6.1754, 106.8272),
    ('Bogor',          'Jawa Barat',         'Jawa',        -6.5950, 106.8166),
    ('Bekasi',         'Jawa Barat',         'Jawa',        -6.2383, 106.9756),
    ('Bandung',        'Jawa Barat',         'Jawa',        -6.9175, 107.6191),
    ('Tangerang',      'Banten',             'Jawa',        -6.1783, 106.6319),
    ('Semarang',       'Jawa Tengah',        'Jawa',        -6.9667, 110.4167),
    ('Surakarta',      'Jawa Tengah',        'Jawa',        -7.5755, 110.8243),
    ('Yogyakarta',     'DI Yogyakarta',      'Jawa',        -7.7956, 110.3695),
    ('Surabaya',       'Jawa Timur',         'Jawa',        -7.2575, 112.7521),
    ('Malang',         'Jawa Timur',         'Jawa',        -7.9666, 112.6326),
    -- Sumatra
    ('Banda Aceh',     'Aceh',               'Sumatra',      5.5483,  95.3238),
    ('Medan',          'Sumatera Utara',     'Sumatra',      3.5952,  98.6722),
    ('Padang',         'Sumatera Barat',     'Sumatra',     -0.9471, 100.4172),
    ('Pekanbaru',      'Riau',               'Sumatra',      0.5071, 101.4478),
    ('Palembang',      'Sumatera Selatan',   'Sumatra',     -2.9761, 104.7754),
    ('Bandar Lampung', 'Lampung',            'Sumatra',     -5.3971, 105.2668),
    -- Kalimantan
    ('Pontianak',      'Kalimantan Barat',   'Kalimantan',  -0.0263, 109.3425),
    ('Banjarmasin',    'Kalimantan Selatan', 'Kalimantan',  -3.3167, 114.5901),
    ('Balikpapan',     'Kalimantan Timur',   'Kalimantan',  -1.2379, 116.8529),
    ('Samarinda',      'Kalimantan Timur',   'Kalimantan',  -0.5021, 117.1537),
    -- Sulawesi
    ('Makassar',       'Sulawesi Selatan',   'Sulawesi',    -5.1477, 119.4327),
    ('Palu',           'Sulawesi Tengah',    'Sulawesi',    -0.8917, 119.8707),
    ('Manado',         'Sulawesi Utara',     'Sulawesi',     1.4748, 124.8421),
    -- Bali dan Nusa Tenggara
    ('Denpasar',       'Bali',               'Bali',        -8.6705, 115.2126),
    ('Mataram',        'Nusa Tenggara Barat','Lombok',      -8.5833, 116.1167),
    -- Papua
    ('Jayapura',       'Papua',              'Papua',       -2.5337, 140.7181)
ON CONFLICT (nama_kota, provinsi) DO NOTHING;

-- -----------------------------------------------------------------------------
-- Faktor emisi per moda transport (kg CO2e per ton-km)
-- NILAI SEMENTARA: ganti dengan angka dan sumber resmi sebelum rilis.
-- -----------------------------------------------------------------------------
INSERT INTO emission_factors (moda_transport, kg_co2e_per_ton_km, sumber) VALUES
    ('truk',    0.100000, 'PLACEHOLDER - ganti dari sumber resmi'),
    ('kapal',   0.020000, 'PLACEHOLDER - ganti dari sumber resmi'),
    ('pesawat', 0.800000, 'PLACEHOLDER - ganti dari sumber resmi')
ON CONFLICT (moda_transport) DO NOTHING;

-- -----------------------------------------------------------------------------
-- Konversi setara (kg CO2e per satuan aktivitas)
-- NILAI SEMENTARA: tim akan mengganti dari jurnal (CLAUDE.md 7.5).
-- -----------------------------------------------------------------------------
INSERT INTO equivalences (aktivitas, satuan, kg_co2e_per_satuan, ikon, sumber) VALUES
    ('Naik motor', 'km', 0.070000, 'motor', 'PLACEHOLDER - ganti dari jurnal'),
    ('Naik mobil', 'km', 0.170000, 'mobil', 'PLACEHOLDER - ganti dari jurnal')
ON CONFLICT (aktivitas) DO NOTHING;

-- -----------------------------------------------------------------------------
-- Akun admin (tidak dibuat lewat registrasi, CLAUDE.md bagian 2)
-- Password contoh ada di README bagian "Akun demo". Hash: BCrypt cost 11.
-- -----------------------------------------------------------------------------
INSERT INTO users (nama, email, password_hash, role) VALUES
    ('Admin KeranjangHijau',
     'admin@keranjanghijau.local',
     '$2a$11$QBArkqLcwm2TI4WcIAFhyuKAJYKI.OEVCqiu1boSBfDCk3MxalQYu',
     'admin')
ON CONFLICT (email) DO NOTHING;

COMMIT;
