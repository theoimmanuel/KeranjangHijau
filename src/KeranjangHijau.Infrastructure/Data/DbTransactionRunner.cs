using System.Data;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>
/// <see cref="ITransactionRunner"/> untuk PostgreSQL: membuka satu koneksi, memulai transaksi,
/// menjalankan <c>work</c>, lalu commit. Jika <c>work</c> gagal, transaksi di-rollback dan
/// exception asli diteruskan.
/// </summary>
public sealed class DbTransactionRunner : ITransactionRunner
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DbTransactionRunner(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<IDbTransaction, CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        T result;
        try
        {
            result = await work(transaction, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await TryRollbackAsync(transaction).ConfigureAwait(false);
            throw;
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static async Task TryRollbackAsync(System.Data.Common.DbTransaction transaction)
    {
        try
        {
            if (transaction.Connection is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // Rollback gagal (mis. koneksi putus): server tetap membatalkan transaksi yang belum
            // di-commit saat koneksi ditutup. Jangan menutupi exception asli dari 'work'.
        }
    }
}
