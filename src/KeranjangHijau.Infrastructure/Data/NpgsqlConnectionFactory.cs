using System.Data.Common;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Options;
using Microsoft.Extensions.Options;
using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>
/// <see cref="IDbConnectionFactory"/> berbasis <see cref="NpgsqlDataSource"/> (connection pooling).
/// Data source dibuat saat koneksi pertama diminta, sehingga aplikasi tetap bisa start
/// (dan service provider bisa dibangun) walaupun connection string belum diatur.
/// </summary>
public sealed class NpgsqlConnectionFactory : IDbConnectionFactory, IAsyncDisposable, IDisposable
{
    private readonly Lazy<NpgsqlDataSource> _dataSource;

    public NpgsqlConnectionFactory(IOptions<DatabaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _dataSource = new Lazy<NpgsqlDataSource>(
            () => NpgsqlDataSource.Create(PostgresConnectionString.Normalize(options.Value.ConnectionString)),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
        await _dataSource.Value.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

    public async ValueTask DisposeAsync()
    {
        if (_dataSource.IsValueCreated)
        {
            await _dataSource.Value.DisposeAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (_dataSource.IsValueCreated)
        {
            _dataSource.Value.Dispose();
        }
    }
}
