using System.Text.Json;
using System.Text.RegularExpressions;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Database;

/// <summary>
/// Test teks: isi <c>MockData/mock-resi.json</c> harus cocok dengan <c>seed.sql</c>
/// (kurir dan kota) dan mencakup semua skenario demo (CLAUDE.md 7.3, 7.4).
/// </summary>
public class MockResiFileTests
{
    private static readonly IReadOnlyList<MockEntry> Entries = LoadMockEntries();
    private static readonly IReadOnlyDictionary<string, string> SeedCityIslands = LoadSeedCityIslands();
    private static readonly IReadOnlySet<string> SeedCourierCodes = LoadSeedCourierCodes();

    [Fact]
    public void MockFile_HasAtLeast15Entries_From5Couriers()
    {
        Assert.True(Entries.Count >= 15, $"Baru ada {Entries.Count} resi mock.");
        Assert.Equal(5, Entries.Select(e => e.KodeKurir).Distinct().Count());
    }

    [Fact]
    public void AllCouriers_ExistInSeed()
    {
        // 5 kurir awal + spx (Shopee Express, M2.4).
        Assert.Equal(6, SeedCourierCodes.Count);
        Assert.All(Entries, e => Assert.Contains(e.KodeKurir, SeedCourierCodes));
    }

    [Fact]
    public void AllCities_ExistInSeed()
    {
        Assert.Equal(26, SeedCityIslands.Count);

        var missing = Entries
            .SelectMany(e => new[] { e.KotaAsal, e.KotaTujuan })
            .Where(city => !SeedCityIslands.ContainsKey(city))
            .Distinct()
            .ToList();

        Assert.True(missing.Count == 0, $"Kota tidak ada di seed.sql: {string.Join(", ", missing)}");
    }

    [Fact]
    public void AllCities_MatchSameSeedCity_ThroughCityNameMatcher()
    {
        // Alur hitung emisi memakai CityNameMatcher (M2.4); nama di mock harus tetap cocok persis.
        var seedCities = SeedCityIslands.Keys
            .Select((nama, i) => new City { CityId = i + 1, NamaKota = nama, Provinsi = "-", Pulau = SeedCityIslands[nama] })
            .ToList();

        Assert.All(
            Entries.SelectMany(e => new[] { e.KotaAsal, e.KotaTujuan }).Distinct(),
            kota => Assert.Equal(kota, CityNameMatcher.Match(kota, seedCities)?.NamaKota));
    }

    [Fact]
    public void ApiSample_CitiesMatchSeed()
    {
        // docs/api-samples/rajaongkir-contoh-dokumentasi.json: JAKARTA BARAT → Jakarta, YOGYAKARTA → Yogyakarta.
        var seedCities = SeedCityIslands.Keys
            .Select((nama, i) => new City { CityId = i + 1, NamaKota = nama, Provinsi = "-", Pulau = SeedCityIslands[nama] })
            .ToList();

        Assert.Equal("Jakarta", CityNameMatcher.Match("JAKARTA BARAT", seedCities)?.NamaKota);
        Assert.Equal("Yogyakarta", CityNameMatcher.Match("YOGYAKARTA", seedCities)?.NamaKota);
        Assert.Contains("spx", SeedCourierCodes);
    }

    [Fact]
    public void AllResi_HaveValidFormat_AndAreUniquePerCourier()
    {
        Assert.All(Entries, e => Assert.Null(ResiFormatRule.GetError(ResiNormalizer.Normalize(e.NomorResi))));
        Assert.Equal(
            Entries.Count,
            Entries.Select(e => (e.KodeKurir, ResiNormalizer.Normalize(e.NomorResi))).Distinct().Count());
    }

    [Fact]
    public void Weights_AreWithinAllowedRange()
    {
        var options = new EmissionOptions();
        Assert.All(Entries.Where(e => e.BeratKg is not null), e => Assert.InRange(e.BeratKg!.Value, 0.001m, options.MaxWeightKg));
    }

    [Fact]
    public void Entries_CoverAllDemoScenarios()
    {
        Assert.Contains(Entries, e => SameIsland(e));
        Assert.Contains(Entries, e => !SameIsland(e) && !IsExpress(e));
        Assert.Contains(Entries, e => !SameIsland(e) && IsExpress(e));
        Assert.Contains(Entries, e => e.BeratKg is not null);
        Assert.Contains(Entries, e => e.BeratKg is null);
    }

    private static bool SameIsland(MockEntry e) => SeedCityIslands[e.KotaAsal] == SeedCityIslands[e.KotaTujuan];

    // Sama dengan aturan 7.3: layanan ekspres jika namanya mengandung salah satu kata kunci default.
    private static bool IsExpress(MockEntry e) =>
        e.Layanan is not null
        && EmissionOptions.DefaultExpressKeywords.Any(k => e.Layanan.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static List<MockEntry> LoadMockEntries()
    {
        var json = File.ReadAllText(RepoFiles.Path("src", "KeranjangHijau.Infrastructure", "MockData", "mock-resi.json"));
        return JsonSerializer.Deserialize<List<MockEntry>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("mock-resi.json kosong.");
    }

    private static Dictionary<string, string> LoadSeedCityIslands()
    {
        var block = SeedInsertBlock("cities");
        return Regex.Matches(block, @"\(\s*'([^']*)'\s*,\s*'[^']*'\s*,\s*'([^']*)'\s*,")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
    }

    private static HashSet<string> LoadSeedCourierCodes() =>
        Regex.Matches(SeedInsertBlock("couriers"), @"\(\s*'([^']*)'\s*,")
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

    private static string SeedInsertBlock(string table)
    {
        var sql = Regex.Replace(File.ReadAllText(RepoFiles.Path("database", "seed.sql")), @"--[^\r\n]*", string.Empty);
        var match = Regex.Match(sql, $@"INSERT\s+INTO\s+{table}\b[^;]*;", RegexOptions.IgnoreCase);
        Assert.True(match.Success, $"INSERT INTO {table} tidak ditemukan di seed.sql.");
        return match.Value;
    }

    private sealed record MockEntry(
        string KodeKurir, string NomorResi, string KotaAsal, string KotaTujuan, string? Layanan, decimal? BeratKg);
}
