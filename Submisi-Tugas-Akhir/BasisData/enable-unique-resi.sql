-- =============================================================================
-- KeranjangHijau - aktifkan UNIQUE (courier_id, nomor_resi)
--
-- JALANKAN SEBELUM RILIS, bersamaan dengan mengubah setting
-- Features:AllowDuplicateResi menjadi false (CLAUDE.md 7.6).
--
-- Akan GAGAL jika masih ada resi duplikat. Cek dulu dengan:
--   SELECT courier_id, nomor_resi, count(*)
--   FROM shipments GROUP BY courier_id, nomor_resi HAVING count(*) > 1;
-- =============================================================================

BEGIN;

-- Index biasa dari schema.sql tidak diperlukan lagi; constraint UNIQUE
-- otomatis membuat index unik di kolom yang sama.
DROP INDEX IF EXISTS idx_shipments_courier_resi;

ALTER TABLE shipments
    ADD CONSTRAINT uq_shipments_courier_resi UNIQUE (courier_id, nomor_resi);

COMMIT;
