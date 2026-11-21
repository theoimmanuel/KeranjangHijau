using System.Data;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="ICityRepository"/> di memori, berisi sebagian kota dari seed.sql (koordinat sama).</summary>
public sealed class InMemoryCityRepository : ICityRepository
{
    public static readonly City Jakarta = new() { CityId = 1, NamaKota = "Jakarta", Provinsi = "DKI Jakarta", Pulau = "Jawa", Latitude = -6.1754m, Longitude = 106.8272m };
    public static readonly City Bandung = new() { CityId = 4, NamaKota = "Bandung", Provinsi = "Jawa Barat", Pulau = "Jawa", Latitude = -6.9175m, Longitude = 107.6191m };
    public static readonly City Surabaya = new() { CityId = 9, NamaKota = "Surabaya", Provinsi = "Jawa Timur", Pulau = "Jawa", Latitude = -7.2575m, Longitude = 112.7521m };
    public static readonly City Medan = new() { CityId = 12, NamaKota = "Medan", Provinsi = "Sumatera Utara", Pulau = "Sumatra", Latitude = 3.5952m, Longitude = 98.6722m };
    public static readonly City Makassar = new() { CityId = 21, NamaKota = "Makassar", Provinsi = "Sulawesi Selatan", Pulau = "Sulawesi", Latitude = -5.1477m, Longitude = 119.4327m };

    private readonly List<City> _cities = [Jakarta, Bandung, Surabaya, Medan, Makassar];

    public Task<City?> GetByNameAsync(string namaKota, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cities
            .Where(c => string.Equals(c.NamaKota, namaKota.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.CityId)
            .FirstOrDefault());

    public Task<IReadOnlyList<City>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        GetAllCallCount++;
        return Task.FromResult<IReadOnlyList<City>>(_cities.OrderBy(c => c.CityId).ToList());
    }

    public int GetAllCallCount { get; private set; }

    public Task<City?> GetByIdAsync(int cityId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cities.SingleOrDefault(c => c.CityId == cityId));
}
