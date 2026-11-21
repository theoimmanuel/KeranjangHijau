using System.Data;
using Dapper;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>
/// Implementasi <see cref="IAdminQueryRepository"/> dengan Dapper. Hanya SELECT berparameter;
/// nilai filter tidak pernah disambung ke teks SQL.
/// </summary>
public sealed class AdminQueryRepository : DapperRepositoryBase, IAdminQueryRepository
{
    /// <summary>Karakter escape untuk pola ILIKE (dipakai juga di klausa <c>ESCAPE</c>).</summary>
    private const char LikeEscape = '\\';

    // Gabungan tabel untuk semua query admin. Semua FK NOT NULL, jadi INNER JOIN tidak mengubah jumlah baris.
    private const string FromClause = """
        FROM emission_records er
        JOIN shipments s ON s.shipment_id = er.shipment_id
        JOIN users u ON u.user_id = s.user_id
        JOIN couriers c ON c.courier_id = s.courier_id
        JOIN cities asal ON asal.city_id = s.origin_city_id
        JOIN cities tujuan ON tujuan.city_id = s.dest_city_id
        JOIN emission_factors ef ON ef.factor_id = er.factor_id
        """;

    // Filter bersama. CAST diperlukan agar PostgreSQL tahu tipe parameter saat bernilai NULL.
    // Rentang setengah terbuka [FromUtc, ToUtc) pada waktu hitung.
    private const string WhereClause = """
        WHERE (CAST(@FromUtc AS timestamptz) IS NULL OR er.calculated_at >= CAST(@FromUtc AS timestamptz))
          AND (CAST(@ToUtc AS timestamptz) IS NULL OR er.calculated_at < CAST(@ToUtc AS timestamptz))
          AND (CAST(@KodeKurir AS text) IS NULL OR c.kode_kurir = CAST(@KodeKurir AS text))
          AND (CAST(@Pola AS text) IS NULL
               OR u.nama ILIKE CAST(@Pola AS text) ESCAPE '\'
               OR u.email ILIKE CAST(@Pola AS text) ESCAPE '\')
        """;

    public AdminQueryRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task<IReadOnlyList<AdminRecordDto>> GetRecordsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT er.record_id, s.shipment_id, u.user_id, u.nama AS nama_pengguna, u.email, s.nomor_resi,
                   c.kode_kurir, c.nama_kurir, asal.nama_kota AS kota_asal, tujuan.nama_kota AS kota_tujuan,
                   ef.moda_transport, s.berat_kg, er.jarak_km, er.total_co2e_kg, er.calculated_at
            {FromClause}
            {WhereClause}
            ORDER BY er.calculated_at DESC, er.record_id DESC
            LIMIT @Limit OFFSET @Offset
            """;

        var parameters = Parameters(query);
        parameters.Add("Limit", query.UkuranHalaman);
        parameters.Add("Offset", query.Offset);

        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<RecordRow>(Command(sql, parameters, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.Select(r => r.ToDto()).ToList();
    }

    public Task<int> CountRecordsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT CAST(count(*) AS integer)
            {FromClause}
            {WhereClause}
            """;

        var parameters = Parameters(query);
        return RunAsync(
            transaction,
            conn => conn.ExecuteScalarAsync<int>(Command(sql, parameters, transaction, cancellationToken)),
            cancellationToken);
    }

    public async Task<AdminTotals> GetTotalsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT CAST(count(DISTINCT u.user_id) AS integer) AS total_pengguna,
                   CAST(count(*) AS integer) AS total_paket,
                   COALESCE(sum(er.total_co2e_kg), 0) AS total_co2e_kg
            {FromClause}
            {WhereClause}
            """;

        var parameters = Parameters(query);
        var row = await RunAsync(
            transaction,
            conn => conn.QuerySingleAsync<TotalsRow>(Command(sql, parameters, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return new AdminTotals(row.TotalPengguna, row.TotalPaket, row.TotalCo2eKg);
    }

    public async Task<IReadOnlyList<AdminCourierTotalDto>> GetCourierTotalsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT k.kode_kurir, k.nama_kurir,
                   CAST(COALESCE(t.jumlah_paket, 0) AS integer) AS jumlah_paket,
                   COALESCE(t.total_co2e_kg, 0) AS total_co2e_kg
            FROM couriers k
            LEFT JOIN (
                SELECT c.courier_id, count(*) AS jumlah_paket, sum(er.total_co2e_kg) AS total_co2e_kg
                {FromClause}
                {WhereClause}
                GROUP BY c.courier_id
            ) t ON t.courier_id = k.courier_id
            WHERE CAST(@KodeKurir AS text) IS NULL OR k.kode_kurir = CAST(@KodeKurir AS text)
            ORDER BY k.courier_id
            """;

        var parameters = Parameters(query);
        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<CourierTotalRow>(Command(sql, parameters, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.Select(r => new AdminCourierTotalDto(r.KodeKurir, r.NamaKurir, r.JumlahPaket, r.TotalCo2eKg)).ToList();
    }

    /// <summary>
    /// Pola ILIKE "mengandung" untuk kata kunci; <c>%</c>, <c>_</c>, dan <c>\</c> di kata kunci di-escape
    /// supaya dicari sebagai huruf biasa, bukan wildcard.
    /// </summary>
    public static string? ToContainsPattern(string? keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return null;
        }

        var escaped = keyword
            .Replace(LikeEscape.ToString(), $"{LikeEscape}{LikeEscape}", StringComparison.Ordinal)
            .Replace("%", $"{LikeEscape}%", StringComparison.Ordinal)
            .Replace("_", $"{LikeEscape}_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    /// <summary>Parameter filter. Npgsql menolak offset ≠ 0, jadi waktu selalu dikirim dalam UTC.</summary>
    private static DynamicParameters Parameters(AdminRecordQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.FromUtc is { } from && query.ToUtc is { } to && from > to)
        {
            throw new ArgumentException("Awal rentang tidak boleh setelah akhir rentang.", nameof(query));
        }

        if (query.Halaman < 1 || query.UkuranHalaman < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Halaman dan ukuran halaman minimal 1.");
        }

        var parameters = new DynamicParameters();
        parameters.Add("FromUtc", query.FromUtc?.ToUniversalTime());
        parameters.Add("ToUtc", query.ToUtc?.ToUniversalTime());
        parameters.Add("KodeKurir", query.KodeKurir, DbType.String);
        parameters.Add("Pola", ToContainsPattern(query.KataKunci), DbType.String);
        return parameters;
    }

    /// <summary>Baris gabungan; moda dibaca sebagai teks lalu dipetakan ke enum.</summary>
    private sealed class RecordRow
    {
        public int RecordId { get; init; }

        public int ShipmentId { get; init; }

        public int UserId { get; init; }

        public string NamaPengguna { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string NomorResi { get; init; } = string.Empty;

        public string KodeKurir { get; init; } = string.Empty;

        public string NamaKurir { get; init; } = string.Empty;

        public string KotaAsal { get; init; } = string.Empty;

        public string KotaTujuan { get; init; } = string.Empty;

        public string ModaTransport { get; init; } = string.Empty;

        public decimal BeratKg { get; init; }

        public decimal JarakKm { get; init; }

        public decimal TotalCo2eKg { get; init; }

        public DateTimeOffset CalculatedAt { get; init; }

        public AdminRecordDto ToDto() => new(
            RecordId,
            ShipmentId,
            UserId,
            NamaPengguna,
            Email,
            NomorResi,
            KodeKurir,
            NamaKurir,
            KotaAsal,
            KotaTujuan,
            TransportModeExtensions.ParseTransportMode(ModaTransport),
            BeratKg,
            JarakKm,
            TotalCo2eKg,
            CalculatedAt);
    }

    private sealed class TotalsRow
    {
        public int TotalPengguna { get; init; }

        public int TotalPaket { get; init; }

        public decimal TotalCo2eKg { get; init; }
    }

    private sealed class CourierTotalRow
    {
        public string KodeKurir { get; init; } = string.Empty;

        public string NamaKurir { get; init; } = string.Empty;

        public int JumlahPaket { get; init; }

        public decimal TotalCo2eKg { get; init; }
    }
}
