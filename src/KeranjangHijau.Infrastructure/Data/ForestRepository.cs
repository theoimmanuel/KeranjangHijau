using System.Data;
using Dapper;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="IForestRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class ForestRepository : DapperRepositoryBase, IForestRepository
{
    public const string ForestNotFoundMessage = "Hutan tidak ditemukan.";
    public const string UserNotFoundMessage = "Pengguna tidak ditemukan.";
    public const string ForestAlreadyExistsMessage = "Pengguna ini sudah memiliki hutan.";

    private readonly IClock _clock;

    public ForestRepository(IDbConnectionFactory connectionFactory, IClock clock)
        : base(connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public Task<Forest?> GetByUserIdAsync(
        int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT forest_id, user_id, nama_hutan, jumlah_pohon_awal, kesehatan, status, updated_at
            FROM forests
            WHERE user_id = @UserId
            """;

        return RunAsync(
            transaction,
            conn => conn.QuerySingleOrDefaultAsync<Forest>(Command(sql, new { UserId = userId }, transaction, cancellationToken)),
            cancellationToken);
    }

    public Task<Forest?> GetByUserIdForUpdateAsync(
        int userId, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        const string sql = """
            SELECT forest_id, user_id, nama_hutan, jumlah_pohon_awal, kesehatan, status, updated_at
            FROM forests
            WHERE user_id = @UserId
            FOR UPDATE
            """;

        return RunAsync(
            transaction,
            conn => conn.QuerySingleOrDefaultAsync<Forest>(Command(sql, new { UserId = userId }, transaction, cancellationToken)),
            cancellationToken);
    }

    public async Task<int> InsertAsync(
        Forest forest, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(forest);
        ArgumentException.ThrowIfNullOrWhiteSpace(forest.NamaHutan, nameof(forest.NamaHutan));
        ArgumentException.ThrowIfNullOrWhiteSpace(forest.Status, nameof(forest.Status));

        const string sql = """
            INSERT INTO forests (user_id, nama_hutan, jumlah_pohon_awal, kesehatan, status, updated_at)
            VALUES (@UserId, @NamaHutan, @JumlahPohonAwal, @Kesehatan, @Status, @UpdatedAt)
            RETURNING forest_id
            """;

        var parameters = new
        {
            forest.UserId,
            NamaHutan = forest.NamaHutan.Trim(),
            forest.JumlahPohonAwal,
            forest.Kesehatan,
            forest.Status,
            UpdatedAt = (forest.UpdatedAt == default ? _clock.UtcNow : forest.UpdatedAt).ToUniversalTime(),
        };

        try
        {
            return await RunAsync(
                transaction,
                conn => conn.ExecuteScalarAsync<int>(Command(sql, parameters, transaction, cancellationToken)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (PostgresErrors.IsUniqueViolation(ex, "uq_forests_user"))
        {
            throw new ValidationException(ForestAlreadyExistsMessage);
        }
        catch (PostgresException ex) when (PostgresErrors.IsForeignKeyViolation(ex, "fk_forests_user"))
        {
            throw new NotFoundException(UserNotFoundMessage);
        }
    }

    public async Task UpdateHealthAsync(
        int forestId,
        decimal kesehatan,
        string status,
        DateTimeOffset updatedAt,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        const string sql = """
            UPDATE forests
            SET kesehatan = @Kesehatan, status = @Status, updated_at = @UpdatedAt
            WHERE forest_id = @ForestId
            """;

        var affected = await RunAsync(
            transaction,
            conn => conn.ExecuteAsync(Command(
                sql,
                new { ForestId = forestId, Kesehatan = kesehatan, Status = status, UpdatedAt = updatedAt.ToUniversalTime() },
                transaction,
                cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        if (affected == 0)
        {
            throw new NotFoundException(ForestNotFoundMessage);
        }
    }
}
