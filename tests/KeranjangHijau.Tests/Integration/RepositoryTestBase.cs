using System.Data.Common;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// Dasar integration test repository. Setiap test berjalan di dalam transaksi yang di-rollback
/// saat test selesai, sehingga database tidak berubah. Butuh <c>KH_DB</c> dan skema + seed
/// yang sudah dijalankan.
/// </summary>
public abstract class RepositoryTestBase : IAsyncLifetime
{
    private DbConnection? _connection;
    private DbTransaction? _transaction;

    protected ServiceProvider Provider { get; } = new ServiceCollection()
        .AddKeranjangHijauBackend(BackendConfiguration.Build())
        .BuildServiceProvider();

    protected DbTransaction Transaction =>
        _transaction ?? throw new InvalidOperationException("Transaksi test belum dibuat.");

    protected IUserRepository Users => Provider.GetRequiredService<IUserRepository>();

    protected IForestRepository Forests => Provider.GetRequiredService<IForestRepository>();

    protected ICourierRepository Couriers => Provider.GetRequiredService<ICourierRepository>();

    protected IShipmentRepository Shipments => Provider.GetRequiredService<IShipmentRepository>();

    protected ICityRepository Cities => Provider.GetRequiredService<ICityRepository>();

    protected IEmissionFactorRepository EmissionFactors => Provider.GetRequiredService<IEmissionFactorRepository>();

    protected IEmissionRecordRepository EmissionRecords => Provider.GetRequiredService<IEmissionRecordRepository>();

    protected IEquivalenceRepository Equivalences => Provider.GetRequiredService<IEquivalenceRepository>();

    protected IForestLogRepository ForestLogs => Provider.GetRequiredService<IForestLogRepository>();

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BackendConfiguration.DbEnvironmentVariable)))
        {
            return; // Test akan di-skip oleh [DatabaseFact].
        }

        _connection = await Provider.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync();
        _transaction = await _connection.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        if (_transaction is not null)
        {
            // Transaksi bisa sudah tidak aktif jika test memicu error koneksi; menutup koneksi
            // tetap membatalkan semua perubahan yang belum di-commit.
            if (_transaction.Connection is not null)
            {
                try
                {
                    await _transaction.RollbackAsync();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            await _transaction.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await Provider.DisposeAsync();
    }

    /// <summary>Email unik per test agar tidak bentrok dengan data lain.</summary>
    protected static string UniqueEmail() => $"test-{Guid.NewGuid():N}@keranjanghijau.test";

    protected static User NewUser(string? email = null) => new()
    {
        Nama = "Pengguna Test",
        Email = email ?? UniqueEmail(),
        PasswordHash = "$2a$04$abcdefghijklmnopqrstuuJ0fBk3Yc3D6Yb2c1tJz5w6XjFJ7Q8xa",
        CreatedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
    };
}
