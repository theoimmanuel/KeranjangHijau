using System.Data;
using Dapper;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>
/// Dasar repository Dapper. Menangani pola "transaksi opsional":
/// <list type="bullet">
///   <item>Ada transaksi → query memakai koneksi transaksi itu, koneksi TIDAK ditutup.</item>
///   <item>Tanpa transaksi → membuka koneksi baru dari pool lalu menutupnya setelah selesai.</item>
/// </list>
/// </summary>
public abstract class DapperRepositoryBase
{
    private readonly IDbConnectionFactory _connectionFactory;

    protected DapperRepositoryBase(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    protected async Task<T> RunAsync<T>(
        IDbTransaction? transaction,
        Func<IDbConnection, Task<T>> work,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            var connection = transaction.Connection
                ?? throw new InvalidOperationException("Transaksi sudah selesai (commit/rollback) dan tidak bisa dipakai lagi.");
            return await work(connection).ConfigureAwait(false);
        }

        await using var ownConnection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await work(ownConnection).ConfigureAwait(false);
    }

    protected static CommandDefinition Command(
        string sql, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken) =>
        new(sql, parameters, transaction, cancellationToken: cancellationToken);
}
