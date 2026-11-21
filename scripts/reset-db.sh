#!/usr/bin/env bash
# Reset database KeranjangHijau: hapus 11 tabel, lalu jalankan schema.sql dan seed.sql.
# PERINGATAN: semua data di tabel tersebut akan hilang.
#
# Koneksi:
#   1. Jika `psql` terpasang  -> memakai variabel KH_DB (dari environment atau file .env).
#   2. Jika tidak             -> memakai psql di dalam container docker compose (service `postgres`).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# Muat .env tanpa menimpa variabel yang sudah diset di environment.
if [[ -f .env ]]; then
  while IFS='=' read -r key value; do
    [[ -z "$key" || "$key" =~ ^[[:space:]]*# ]] && continue
    key="${key//[[:space:]]/}"
    [[ -z "${!key:-}" ]] && export "$key=$value"
  done < .env
fi

DROP_SQL="SET client_min_messages = warning;
DROP TABLE IF EXISTS
  shared_reports, forest_logs, forests, record_equivalences, equivalences,
  emission_records, emission_factors, shipments, cities, couriers, users CASCADE;"

if command -v psql >/dev/null 2>&1; then
  if [[ -z "${KH_DB:-}" ]]; then
    echo "KH_DB belum diisi. Set di environment atau di file .env (lihat .env.example)." >&2
    exit 1
  fi
  run_sql()  { psql "$KH_DB" -v ON_ERROR_STOP=1 -q "$@"; }
  run_file() { run_sql -f "$1"; }
else
  echo "psql tidak ditemukan, memakai container docker compose (service postgres)."
  run_sql()  { docker compose exec -T postgres psql -U "${POSTGRES_USER:?}" -d "${POSTGRES_DB:-keranjanghijau}" -v ON_ERROR_STOP=1 -q "$@"; }
  run_file() { run_sql < "$1"; }
fi

echo "1/3 Menghapus tabel lama..."
run_sql -c "$DROP_SQL"
echo "2/3 Menjalankan database/schema.sql..."
run_file database/schema.sql
echo "3/3 Menjalankan database/seed.sql..."
run_file database/seed.sql
echo "Selesai. Database sudah di-reset."
