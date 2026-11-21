using System.Data;
using Dapper;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="IEquivalenceRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class EquivalenceRepository : DapperRepositoryBase, IEquivalenceRepository
{
    public const string RecordNotFoundMessage = "Hasil hitung emisi tidak ditemukan.";
    public const string EquivalenceNotFoundMessage = "Data konversi setara tidak ditemukan.";

    public EquivalenceRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task<IReadOnlyList<Equivalence>> GetAllAsync(
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT equivalence_id, aktivitas, satuan, kg_co2e_per_satuan, ikon, sumber
            FROM equivalences
            ORDER BY equivalence_id
            """;

        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<Equivalence>(Command(sql, null, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<int> InsertRecordEquivalencesAsync(
        IReadOnlyCollection<RecordEquivalence> rows,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            return 0;
        }

        foreach (var row in rows)
        {
            ArgumentNullException.ThrowIfNull(row, nameof(rows));
            ArgumentOutOfRangeException.ThrowIfNegative(row.NilaiSetara, nameof(rows));
        }

        const string sql = """
            INSERT INTO record_equivalences (record_id, equivalence_id, nilai_setara)
            VALUES (@RecordId, @EquivalenceId, @NilaiSetara)
            """;

        try
        {
            // Dapper menjalankan perintah sekali per elemen daftar (jumlah aktivitas sedikit).
            return await RunAsync(
                transaction,
                conn => conn.ExecuteAsync(Command(sql, rows, transaction, cancellationToken)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (PostgresErrors.IsUniqueViolation(ex, "pk_record_equivalences"))
        {
            throw new InvalidOperationException("Nilai setara untuk record emisi ini sudah tersimpan.", ex);
        }
        catch (PostgresException ex) when (PostgresErrors.IsForeignKeyViolation(ex, "fk_record_equivalences_record"))
        {
            throw new NotFoundException(RecordNotFoundMessage);
        }
        catch (PostgresException ex) when (PostgresErrors.IsForeignKeyViolation(ex, "fk_record_equivalences_equivalence"))
        {
            throw new NotFoundException(EquivalenceNotFoundMessage);
        }
    }

    public async Task<IReadOnlyList<EquivalenceValue>> GetByRecordIdAsync(
        int recordId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT e.equivalence_id, e.aktivitas, e.satuan, e.kg_co2e_per_satuan, e.ikon, e.sumber, re.nilai_setara
            FROM record_equivalences re
            JOIN equivalences e ON e.equivalence_id = re.equivalence_id
            WHERE re.record_id = @RecordId
            ORDER BY e.equivalence_id
            """;

        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<EquivalenceValueRow>(Command(
                sql, new { RecordId = recordId }, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.Select(r => r.ToValue()).ToList();
    }

    /// <summary>Baris gabungan <c>record_equivalences</c> + <c>equivalences</c>.</summary>
    private sealed class EquivalenceValueRow
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
