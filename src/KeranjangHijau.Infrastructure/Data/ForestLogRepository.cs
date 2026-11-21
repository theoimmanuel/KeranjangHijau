using System.Data;
using Dapper;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="IForestLogRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class ForestLogRepository : DapperRepositoryBase, IForestLogRepository
{
    public const string ForestNotFoundMessage = "Hutan tidak ditemukan.";
    public const string RecordNotFoundMessage = "Hasil hitung emisi tidak ditemukan.";

    /// <summary>Panjang maksimum <c>forest_logs.keterangan</c> (VARCHAR(200)).</summary>
    public const int MaxKeteranganLength = 200;

    private readonly IClock _clock;

    public ForestLogRepository(IDbConnectionFactory connectionFactory, IClock clock)
        : base(connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public async Task<int> InsertAsync(
        ForestLog log, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentException.ThrowIfNullOrWhiteSpace(log.Keterangan, nameof(log.Keterangan));
        var keterangan = log.Keterangan.Trim();
        if (keterangan.Length > MaxKeteranganLength)
        {
            throw new ArgumentException($"Keterangan log hutan maksimal {MaxKeteranganLength} karakter.", nameof(log.Keterangan));
        }

        if (log.KesehatanSetelah is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(log.KesehatanSetelah), log.KesehatanSetelah, "Kesehatan hutan harus di antara 0 dan 100.");
        }

        const string sql = """
            INSERT INTO forest_logs (forest_id, record_id, perubahan, kesehatan_setelah, keterangan, created_at)
            VALUES (@ForestId, @RecordId, @Perubahan, @KesehatanSetelah, @Keterangan, @CreatedAt)
            RETURNING log_id
            """;

        var parameters = new
        {
            log.ForestId,
            log.RecordId,
            log.Perubahan,
            log.KesehatanSetelah,
            Keterangan = keterangan,
            CreatedAt = (log.CreatedAt == default ? _clock.UtcNow : log.CreatedAt).ToUniversalTime(),
        };

        try
        {
            return await RunAsync(
                transaction,
                conn => conn.ExecuteScalarAsync<int>(Command(sql, parameters, transaction, cancellationToken)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (PostgresErrors.IsUniqueViolation(ex, "uq_forest_logs_record"))
        {
            throw new InvalidOperationException("Log hutan untuk record emisi ini sudah tersimpan.", ex);
        }
        catch (PostgresException ex) when (PostgresErrors.IsForeignKeyViolation(ex, "fk_forest_logs_forest"))
        {
            throw new NotFoundException(ForestNotFoundMessage);
        }
        catch (PostgresException ex) when (PostgresErrors.IsForeignKeyViolation(ex, "fk_forest_logs_record"))
        {
            throw new NotFoundException(RecordNotFoundMessage);
        }
    }

    public async Task<IReadOnlyList<ForestLog>> GetByForestIdAsync(
        int forestId, int limit, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        const string sql = """
            SELECT log_id, forest_id, record_id, perubahan, kesehatan_setelah, keterangan, created_at
            FROM forest_logs
            WHERE forest_id = @ForestId
            ORDER BY created_at DESC, log_id DESC
            LIMIT @Limit
            """;

        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<ForestLog>(Command(sql, new { ForestId = forestId, Limit = limit }, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.ToList();
    }
}
