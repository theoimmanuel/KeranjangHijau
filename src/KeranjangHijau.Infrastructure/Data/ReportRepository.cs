using System.Data;
using Dapper;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="IReportRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class ReportRepository : DapperRepositoryBase, IReportRepository
{
    // Filter bersama: pemilik data + rentang setengah terbuka [FromUtc, ToUtc) pada shipments.created_at.
    // CAST diperlukan agar PostgreSQL tahu tipe parameter saat FromUtc bernilai NULL (periode "Semua").
    private const string ShipmentFilter = """
        s.user_id = @UserId
          AND (CAST(@FromUtc AS timestamptz) IS NULL OR s.created_at >= CAST(@FromUtc AS timestamptz))
          AND s.created_at < @ToUtc
        """;

    public ReportRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task<IReadOnlyList<ReportShipmentDto>> GetShipmentsAsync(
        int userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset toUtc,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT s.shipment_id, er.record_id, s.nomor_resi, c.kode_kurir, c.nama_kurir,
                   asal.nama_kota AS kota_asal, tujuan.nama_kota AS kota_tujuan, s.layanan, s.berat_kg,
                   er.jarak_km, ef.moda_transport, er.total_co2e_kg, s.created_at
            FROM shipments s
            JOIN emission_records er ON er.shipment_id = s.shipment_id
            JOIN emission_factors ef ON ef.factor_id = er.factor_id
            JOIN couriers c ON c.courier_id = s.courier_id
            JOIN cities asal ON asal.city_id = s.origin_city_id
            JOIN cities tujuan ON tujuan.city_id = s.dest_city_id
            WHERE {ShipmentFilter}
            ORDER BY s.created_at DESC, s.shipment_id DESC
            """;

        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<ShipmentRow>(Command(
                sql, Parameters(userId, fromUtc, toUtc), transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.Select(r => r.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<DailyEmissionDto>> GetDailyTotalsAsync(
        int userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset toUtc,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        // Dikelompokkan per tanggal kalender WIB, bukan tanggal UTC.
        const string sql = $"""
            SELECT (s.created_at AT TIME ZONE @TimeZone)::date AS tanggal,
                   CAST(count(*) AS integer) AS jumlah_paket,
                   sum(er.total_co2e_kg) AS total_co2e_kg
            FROM shipments s
            JOIN emission_records er ON er.shipment_id = s.shipment_id
            WHERE {ShipmentFilter}
            GROUP BY 1
            ORDER BY 1
            """;

        var parameters = new DynamicParameters(Parameters(userId, fromUtc, toUtc));
        parameters.Add("TimeZone", PeriodRangeRule.TimeZoneId);

        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<DailyRow>(Command(sql, parameters, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.Select(r => new DailyEmissionDto(DateOnly.FromDateTime(r.Tanggal), r.JumlahPaket, r.TotalCo2eKg)).ToList();
    }

    public async Task<IReadOnlyList<EquivalenceValue>> GetEquivalenceTotalsAsync(
        int userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset toUtc,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT e.equivalence_id, e.aktivitas, e.satuan, e.kg_co2e_per_satuan, e.ikon, e.sumber,
                   COALESCE(t.nilai_setara, 0) AS nilai_setara
            FROM equivalences e
            LEFT JOIN (
                SELECT re.equivalence_id, sum(re.nilai_setara) AS nilai_setara
                FROM shipments s
                JOIN emission_records er ON er.shipment_id = s.shipment_id
                JOIN record_equivalences re ON re.record_id = er.record_id
                WHERE {ShipmentFilter}
                GROUP BY re.equivalence_id
            ) t ON t.equivalence_id = e.equivalence_id
            ORDER BY e.equivalence_id
            """;

        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<EquivalenceTotalRow>(Command(
                sql, Parameters(userId, fromUtc, toUtc), transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.Select(r => r.ToValue()).ToList();
    }

    /// <summary>Parameter filter. Npgsql menolak offset ≠ 0, jadi waktu selalu dikirim dalam UTC.</summary>
    private static object Parameters(int userId, DateTimeOffset? fromUtc, DateTimeOffset toUtc)
    {
        if (fromUtc is { } from && from > toUtc)
        {
            throw new ArgumentException("Awal periode tidak boleh setelah akhir periode.", nameof(fromUtc));
        }

        return new
        {
            UserId = userId,
            FromUtc = fromUtc?.ToUniversalTime(),
            ToUtc = toUtc.ToUniversalTime(),
        };
    }

    /// <summary>Baris gabungan daftar paket; moda dibaca sebagai teks lalu dipetakan ke enum.</summary>
    private sealed class ShipmentRow
    {
        public int ShipmentId { get; init; }

        public int RecordId { get; init; }

        public string NomorResi { get; init; } = string.Empty;

        public string KodeKurir { get; init; } = string.Empty;

        public string NamaKurir { get; init; } = string.Empty;

        public string KotaAsal { get; init; } = string.Empty;

        public string KotaTujuan { get; init; } = string.Empty;

        public string? Layanan { get; init; }

        public decimal BeratKg { get; init; }

        public decimal JarakKm { get; init; }

        public string ModaTransport { get; init; } = string.Empty;

        public decimal TotalCo2eKg { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public ReportShipmentDto ToDto() => new(
            ShipmentId,
            RecordId,
            NomorResi,
            KodeKurir,
            NamaKurir,
            KotaAsal,
            KotaTujuan,
            Layanan,
            BeratKg,
            JarakKm,
            TransportModeExtensions.ParseTransportMode(ModaTransport),
            TotalCo2eKg,
            CreatedAt);
    }

    private sealed class DailyRow
    {
        public DateTime Tanggal { get; init; }

        public int JumlahPaket { get; init; }

        public decimal TotalCo2eKg { get; init; }
    }

    private sealed class EquivalenceTotalRow
    {
        public int EquivalenceId { get; init; }

        public string Aktivitas { get; init; } = string.Empty;

        public string Satuan { get; init; } = string.Empty;

        public decimal KgCo2ePerSatuan { get; init; }

        public string? Ikon { get; init; }

        public string Sumber { get; init; } = string.Empty;

        public decimal NilaiSetara { get; init; }

        public EquivalenceValue ToValue() => new(
            new Equivalence
            {
                EquivalenceId = EquivalenceId,
                Aktivitas = Aktivitas,
                Satuan = Satuan,
                KgCo2ePerSatuan = KgCo2ePerSatuan,
                Ikon = Ikon,
                Sumber = Sumber,
            },
            NilaiSetara);
    }
}
