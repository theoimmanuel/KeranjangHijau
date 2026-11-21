using System.Data;
using Dapper;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="ICourierRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class CourierRepository : DapperRepositoryBase, ICourierRepository
{
    private const string SelectColumns = "SELECT courier_id, kode_kurir, nama_kurir FROM couriers";

    public CourierRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task<IReadOnlyList<Courier>> GetAllAsync(
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<Courier>(Command($"{SelectColumns} ORDER BY nama_kurir", null, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.AsList();
    }

    public Task<Courier?> GetByKodeAsync(
        string kodeKurir, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(kodeKurir);

        // Kode kurir disimpan huruf kecil (seed.sql), jadi input cukup dinormalisasi.
        return RunAsync(
            transaction,
            conn => conn.QuerySingleOrDefaultAsync<Courier>(Command(
                $"{SelectColumns} WHERE kode_kurir = @KodeKurir",
                new { KodeKurir = kodeKurir.Trim().ToLowerInvariant() },
                transaction,
                cancellationToken)),
            cancellationToken);
    }
}
