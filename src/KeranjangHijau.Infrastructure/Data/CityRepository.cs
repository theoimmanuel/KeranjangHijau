using System.Data;
using Dapper;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="ICityRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class CityRepository : DapperRepositoryBase, ICityRepository
{
    private const string SelectColumns =
        "SELECT city_id, nama_kota, provinsi, pulau, latitude, longitude FROM cities";

    public CityRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public Task<City?> GetByNameAsync(
        string namaKota, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(namaKota);

        return RunAsync(
            transaction,
            conn => conn.QueryFirstOrDefaultAsync<City>(Command(
                $"{SelectColumns} WHERE lower(nama_kota) = lower(@NamaKota) ORDER BY city_id LIMIT 1",
                new { NamaKota = namaKota.Trim() },
                transaction,
                cancellationToken)),
            cancellationToken);
    }

    public async Task<IReadOnlyList<City>> GetAllAsync(
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<City>(Command($"{SelectColumns} ORDER BY city_id", null, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.AsList();
    }

    public Task<City?> GetByIdAsync(
        int cityId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        RunAsync(
            transaction,
            conn => conn.QuerySingleOrDefaultAsync<City>(Command(
                $"{SelectColumns} WHERE city_id = @CityId",
                new { CityId = cityId },
                transaction,
                cancellationToken)),
            cancellationToken);
}
