using System.Text.Json;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Integration;

public class CityRepositoryTests : RepositoryTestBase
{
    [DatabaseFact]
    public async Task GetByName_IsCaseInsensitive_AndTrims()
    {
        var city = await Cities.GetByNameAsync("  bANDAR lampung ", Transaction);

        Assert.NotNull(city);
        Assert.Equal("Bandar Lampung", city.NamaKota);
        Assert.Equal("Lampung", city.Provinsi);
        Assert.Equal("Sumatra", city.Pulau);
        Assert.Equal(-5.3971m, city.Latitude);
        Assert.Equal(105.2668m, city.Longitude);
    }

    [DatabaseFact]
    public async Task GetById_ReturnsSameCity()
    {
        var byName = await Cities.GetByNameAsync("Makassar", Transaction);
        Assert.NotNull(byName);

        var byId = await Cities.GetByIdAsync(byName.CityId, Transaction);

        Assert.NotNull(byId);
        Assert.Equal("Makassar", byId.NamaKota);
        Assert.Equal("Sulawesi", byId.Pulau);
    }

    [DatabaseFact]
    public async Task Unknown_ReturnsNull()
    {
        Assert.Null(await Cities.GetByNameAsync("Kota Fiksi", Transaction));
        Assert.Null(await Cities.GetByIdAsync(-1, Transaction));
    }

    [DatabaseFact]
    public async Task AllMockCities_CanBeMatched()
    {
        using var doc = JsonDocument.Parse(
            await File.ReadAllTextAsync(RepoFiles.Path("src", "KeranjangHijau.Infrastructure", "MockData", "mock-resi.json")));
        var names = doc.RootElement.EnumerateArray()
            .SelectMany(e => new[] { e.GetProperty("kotaAsal").GetString()!, e.GetProperty("kotaTujuan").GetString()! })
            .Distinct();

        foreach (var name in names)
        {
            Assert.True(await Cities.GetByNameAsync(name, Transaction) is not null, $"Kota '{name}' tidak ditemukan di tabel cities.");
        }
    }

    [DatabaseFact]
    public async Task GetAll_ReturnsSeededCities_OrderedById()
    {
        var cities = await Cities.GetAllAsync(Transaction);

        Assert.True(cities.Count >= 26);
        Assert.Equal(cities.Select(c => c.CityId).Order(), cities.Select(c => c.CityId));
        Assert.Equal("Jakarta", CityNameMatcher.Match("KOTA ADM. JAKARTA BARAT", cities)?.NamaKota);
        Assert.Equal("Yogyakarta", CityNameMatcher.Match("YOGYAKARTA", cities)?.NamaKota);
    }
}
