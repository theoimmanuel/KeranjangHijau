-- =============================================================================
-- KeranjangHijau - skema database PostgreSQL (11 tabel)
-- Sumber kebenaran: CLAUDE.md bagian 6.
--
-- Jalankan di database kosong:   psql "$KH_DB" -v ON_ERROR_STOP=1 -f database/schema.sql
-- Untuk mengulang dari nol pakai: scripts/reset-db.sh  (atau scripts/reset-db.ps1)
--
-- Catatan:
--   * Semua waktu bertipe TIMESTAMPTZ (disimpan UTC).
--   * Angka desimal memakai NUMERIC.
--   * UNIQUE (courier_id, nomor_resi) SENGAJA belum dipasang (mode development,
--     CLAUDE.md 7.6). Lihat database/enable-unique-resi.sql.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. users : akun pengguna dan admin
-- -----------------------------------------------------------------------------
CREATE TABLE users (
    user_id         INTEGER      GENERATED ALWAYS AS IDENTITY,
    nama            VARCHAR(100) NOT NULL,
    email           VARCHAR(254) NOT NULL,
    password_hash   VARCHAR(100) NOT NULL,
    role            VARCHAR(10)  NOT NULL DEFAULT 'pengguna',
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_users          PRIMARY KEY (user_id),
    CONSTRAINT uq_users_email    UNIQUE (email),
    CONSTRAINT ck_users_role     CHECK (role IN ('pengguna', 'admin')),
    CONSTRAINT ck_users_nama     CHECK (length(trim(nama)) > 0)
);

-- -----------------------------------------------------------------------------
-- 2. couriers : master kurir
-- -----------------------------------------------------------------------------
CREATE TABLE couriers (
    courier_id      INTEGER      GENERATED ALWAYS AS IDENTITY,
    kode_kurir      VARCHAR(20)  NOT NULL,
    nama_kurir      VARCHAR(100) NOT NULL,

    CONSTRAINT pk_couriers            PRIMARY KEY (courier_id),
    CONSTRAINT uq_couriers_kode_kurir UNIQUE (kode_kurir)
);

-- -----------------------------------------------------------------------------
-- 3. cities : master kota (koordinat untuk haversine, pulau untuk moda)
-- -----------------------------------------------------------------------------
CREATE TABLE cities (
    city_id         INTEGER      GENERATED ALWAYS AS IDENTITY,
    nama_kota       VARCHAR(100) NOT NULL,
    provinsi        VARCHAR(100) NOT NULL,
    pulau           VARCHAR(50)  NOT NULL,
    latitude        NUMERIC(9,6) NOT NULL,
    longitude       NUMERIC(9,6) NOT NULL,

    CONSTRAINT pk_cities                PRIMARY KEY (city_id),
    CONSTRAINT uq_cities_nama_provinsi  UNIQUE (nama_kota, provinsi),
    CONSTRAINT ck_cities_latitude       CHECK (latitude  BETWEEN -90  AND 90),
    CONSTRAINT ck_cities_longitude      CHECK (longitude BETWEEN -180 AND 180)
);

-- -----------------------------------------------------------------------------
-- 4. shipments : paket yang diinput pengguna dari nomor resi
-- -----------------------------------------------------------------------------
CREATE TABLE shipments (
    shipment_id     INTEGER      GENERATED ALWAYS AS IDENTITY,
    user_id         INTEGER      NOT NULL,
    courier_id      INTEGER      NOT NULL,
    nomor_resi      VARCHAR(30)  NOT NULL,
    origin_city_id  INTEGER      NOT NULL,
    dest_city_id    INTEGER      NOT NULL,
    layanan         VARCHAR(50),
    berat_kg        NUMERIC(6,3) NOT NULL,
    sumber_data     VARCHAR(10)  NOT NULL,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_shipments              PRIMARY KEY (shipment_id),
    CONSTRAINT fk_shipments_user         FOREIGN KEY (user_id)
        REFERENCES users (user_id) ON DELETE CASCADE,
    CONSTRAINT fk_shipments_courier      FOREIGN KEY (courier_id)
        REFERENCES couriers (courier_id),
    CONSTRAINT fk_shipments_origin_city  FOREIGN KEY (origin_city_id)
        REFERENCES cities (city_id),
    CONSTRAINT fk_shipments_dest_city    FOREIGN KEY (dest_city_id)
        REFERENCES cities (city_id),
    -- 7.6: resi sudah dinormalisasi (tanpa spasi, huruf besar), 8-30 huruf/angka
    CONSTRAINT ck_shipments_nomor_resi   CHECK (nomor_resi ~ '^[A-Z0-9]{8,30}$'),
    -- 7.4: berat > 0 dan <= 100 kg
    CONSTRAINT ck_shipments_berat_kg     CHECK (berat_kg > 0 AND berat_kg <= 100),
    CONSTRAINT ck_shipments_sumber_data  CHECK (sumber_data IN ('api', 'mock', 'manual'))
);

CREATE INDEX idx_shipments_user_id     ON shipments (user_id);
CREATE INDEX idx_shipments_created_at  ON shipments (created_at);
-- Index biasa (tidak unik) untuk cek duplikat resi selama development.
-- enable-unique-resi.sql menggantinya dengan constraint UNIQUE.
CREATE INDEX idx_shipments_courier_resi ON shipments (courier_id, nomor_resi);

-- -----------------------------------------------------------------------------
-- 5. emission_factors : faktor emisi per moda transport
-- -----------------------------------------------------------------------------
CREATE TABLE emission_factors (
    factor_id           INTEGER       GENERATED ALWAYS AS IDENTITY,
    moda_transport      VARCHAR(10)   NOT NULL,
    kg_co2e_per_ton_km  NUMERIC(10,6) NOT NULL,
    sumber              TEXT          NOT NULL,

    CONSTRAINT pk_emission_factors        PRIMARY KEY (factor_id),
    CONSTRAINT uq_emission_factors_moda   UNIQUE (moda_transport),
    CONSTRAINT ck_emission_factors_moda   CHECK (moda_transport IN ('truk', 'kapal', 'pesawat')),
    CONSTRAINT ck_emission_factors_nilai  CHECK (kg_co2e_per_ton_km > 0)
);

-- -----------------------------------------------------------------------------
-- 6. emission_records : hasil hitung emisi (1 shipment = 1 record)
-- -----------------------------------------------------------------------------
CREATE TABLE emission_records (
    record_id       INTEGER        GENERATED ALWAYS AS IDENTITY,
    shipment_id     INTEGER        NOT NULL,
    factor_id       INTEGER        NOT NULL,
    jarak_km        NUMERIC(10,2)  NOT NULL,
    total_co2e_kg   NUMERIC(12,6)  NOT NULL,
    metode          VARCHAR(15)    NOT NULL,
    calculated_at   TIMESTAMPTZ    NOT NULL DEFAULT now(),

    CONSTRAINT pk_emission_records           PRIMARY KEY (record_id),
    CONSTRAINT uq_emission_records_shipment  UNIQUE (shipment_id),
    CONSTRAINT fk_emission_records_shipment  FOREIGN KEY (shipment_id)
        REFERENCES shipments (shipment_id) ON DELETE CASCADE,
    CONSTRAINT fk_emission_records_factor    FOREIGN KEY (factor_id)
        REFERENCES emission_factors (factor_id),
    CONSTRAINT ck_emission_records_jarak     CHECK (jarak_km > 0),
    CONSTRAINT ck_emission_records_total     CHECK (total_co2e_kg >= 0),
    CONSTRAINT ck_emission_records_metode    CHECK (metode IN ('api', 'rumus_lokal'))
);

CREATE INDEX idx_emission_records_calculated_at ON emission_records (calculated_at);
CREATE INDEX idx_emission_records_factor_id     ON emission_records (factor_id);

-- -----------------------------------------------------------------------------
-- 7. equivalences : konversi setara (mis. km naik motor/mobil)
-- -----------------------------------------------------------------------------
CREATE TABLE equivalences (
    equivalence_id      INTEGER       GENERATED ALWAYS AS IDENTITY,
    aktivitas           VARCHAR(100)  NOT NULL,
    satuan              VARCHAR(20)   NOT NULL,
    kg_co2e_per_satuan  NUMERIC(10,6) NOT NULL,
    ikon                VARCHAR(50),
    sumber              TEXT          NOT NULL,

    CONSTRAINT pk_equivalences            PRIMARY KEY (equivalence_id),
    CONSTRAINT uq_equivalences_aktivitas  UNIQUE (aktivitas),
    CONSTRAINT ck_equivalences_nilai      CHECK (kg_co2e_per_satuan > 0)
);

-- -----------------------------------------------------------------------------
-- 8. record_equivalences : nilai setara per hasil emisi (many-to-many)
-- -----------------------------------------------------------------------------
CREATE TABLE record_equivalences (
    record_id       INTEGER        NOT NULL,
    equivalence_id  INTEGER        NOT NULL,
    nilai_setara    NUMERIC(14,4)  NOT NULL,

    CONSTRAINT pk_record_equivalences             PRIMARY KEY (record_id, equivalence_id),
    CONSTRAINT fk_record_equivalences_record      FOREIGN KEY (record_id)
        REFERENCES emission_records (record_id) ON DELETE CASCADE,
    CONSTRAINT fk_record_equivalences_equivalence FOREIGN KEY (equivalence_id)
        REFERENCES equivalences (equivalence_id),
    CONSTRAINT ck_record_equivalences_nilai       CHECK (nilai_setara >= 0)
);

CREATE INDEX idx_record_equivalences_equivalence_id ON record_equivalences (equivalence_id);

-- -----------------------------------------------------------------------------
-- 9. forests : hutan virtual (1 user = 1 hutan)
-- -----------------------------------------------------------------------------
CREATE TABLE forests (
    forest_id           INTEGER       GENERATED ALWAYS AS IDENTITY,
    user_id             INTEGER       NOT NULL,
    nama_hutan          VARCHAR(120)  NOT NULL,
    jumlah_pohon_awal   INTEGER       NOT NULL DEFAULT 100,
    kesehatan           NUMERIC(5,2)  NOT NULL DEFAULT 100,
    status              VARCHAR(10)   NOT NULL DEFAULT 'Lestari',
    updated_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT pk_forests              PRIMARY KEY (forest_id),
    CONSTRAINT uq_forests_user         UNIQUE (user_id),
    CONSTRAINT fk_forests_user         FOREIGN KEY (user_id)
        REFERENCES users (user_id) ON DELETE CASCADE,
    CONSTRAINT ck_forests_pohon_awal   CHECK (jumlah_pohon_awal > 0),
    CONSTRAINT ck_forests_kesehatan    CHECK (kesehatan BETWEEN 0 AND 100),
    CONSTRAINT ck_forests_status       CHECK (status IN ('Lestari', 'Sehat', 'Menipis', 'Terancam', 'Kritis'))
);

-- -----------------------------------------------------------------------------
-- 10. forest_logs : riwayat perubahan kesehatan hutan
--     record_id NULL = log pemulihan alami.
-- -----------------------------------------------------------------------------
CREATE TABLE forest_logs (
    log_id              INTEGER       GENERATED ALWAYS AS IDENTITY,
    forest_id           INTEGER       NOT NULL,
    record_id           INTEGER,
    perubahan           NUMERIC(7,2)  NOT NULL,
    kesehatan_setelah   NUMERIC(5,2)  NOT NULL,
    keterangan          VARCHAR(200)  NOT NULL,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT pk_forest_logs            PRIMARY KEY (log_id),
    -- UNIQUE tetap mengizinkan banyak baris dengan record_id NULL (log pemulihan).
    CONSTRAINT uq_forest_logs_record     UNIQUE (record_id),
    CONSTRAINT fk_forest_logs_forest     FOREIGN KEY (forest_id)
        REFERENCES forests (forest_id) ON DELETE CASCADE,
    CONSTRAINT fk_forest_logs_record     FOREIGN KEY (record_id)
        REFERENCES emission_records (record_id) ON DELETE CASCADE,
    CONSTRAINT ck_forest_logs_kesehatan  CHECK (kesehatan_setelah BETWEEN 0 AND 100)
);

CREATE INDEX idx_forest_logs_forest_created ON forest_logs (forest_id, created_at);

-- -----------------------------------------------------------------------------
-- 11. shared_reports : riwayat laporan yang dibagikan (kartu Instagram Story)
-- -----------------------------------------------------------------------------
CREATE TABLE shared_reports (
    share_id          INTEGER        GENERATED ALWAYS AS IDENTITY,
    user_id           INTEGER        NOT NULL,
    periode_mulai     TIMESTAMPTZ    NOT NULL,
    periode_selesai   TIMESTAMPTZ    NOT NULL,
    jumlah_paket      INTEGER        NOT NULL,
    total_co2e_kg     NUMERIC(12,6)  NOT NULL,
    file_gambar       VARCHAR(260),
    created_at        TIMESTAMPTZ    NOT NULL DEFAULT now(),

    CONSTRAINT pk_shared_reports          PRIMARY KEY (share_id),
    CONSTRAINT fk_shared_reports_user     FOREIGN KEY (user_id)
        REFERENCES users (user_id) ON DELETE CASCADE,
    CONSTRAINT ck_shared_reports_periode  CHECK (periode_selesai >= periode_mulai),
    CONSTRAINT ck_shared_reports_jumlah   CHECK (jumlah_paket >= 0),
    CONSTRAINT ck_shared_reports_total    CHECK (total_co2e_kg >= 0)
);

CREATE INDEX idx_shared_reports_user_id ON shared_reports (user_id);

COMMIT;
