using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class CityNameMatcherTests
{
    private static readonly City Jakarta = City(1, "Jakarta");
    private static readonly City Bandung = City(4, "Bandung");
    private static readonly City Yogyakarta = City(8, "Yogyakarta");
    private static readonly City Malang = City(10, "Malang");
    private static readonly City BandarLampung = City(16, "Bandar Lampung");
    private static readonly City Lampung = City(90, "Lampung"); // fiktif: untuk uji "nama terpanjang"

    private static readonly City[] Cities = [Jakarta, Bandung, Yogyakarta, Malang, BandarLampung];

    private static City City(int id, string nama) =>
        new() { CityId = id, NamaKota = nama, Provinsi = "-", Pulau = "Jawa", Latitude = 0, Longitude = 0 };

    [Theory]
    [InlineData("Jakarta")]
    [InlineData("jakarta")]
    [InlineData("JAKARTA")]
    [InlineData("  jaKarta  ")]
    public void ExactName_IgnoresCaseAndSurroundingSpaces(string input)
    {
        Assert.Same(Jakarta, CityNameMatcher.Match(input, Cities));
    }

    [Fact]
    public void ExactName_CollapsesExtraInnerSpaces()
    {
        Assert.Same(BandarLampung, CityNameMatcher.Match("bandar    LAMPUNG", Cities));
    }

    [Theory]
    [InlineData("KOTA YOGYAKARTA")]
    [InlineData("Kota Yogyakarta")]
    [InlineData("KABUPATEN YOGYAKARTA")]
    [InlineData("KAB. YOGYAKARTA")]
    [InlineData("KAB.YOGYAKARTA")]
    [InlineData("YOGYAKARTA, DI YOGYAKARTA")]
    [InlineData("KOTA YOGYAKARTA, D.I. YOGYAKARTA")]
    public void AdministrativePrefixAndProvinceSuffix_AreRemoved(string input)
    {
        Assert.Same(Yogyakarta, CityNameMatcher.Match(input, Cities));
    }

    [Theory]
    [InlineData("JAKARTA BARAT")]
    [InlineData("Jakarta Selatan")]
    [InlineData("KOTA ADM. JAKARTA BARAT")]
    [InlineData("KOTA ADM.JAKARTA TIMUR")]
    [InlineData("KOTA ADM. JAKARTA PUSAT, DKI JAKARTA")]
    [InlineData("JAKARTA-UTARA")]
    public void CityNameAsWholeWord_Matches(string input)
    {
        Assert.Same(Jakarta, CityNameMatcher.Match(input, Cities));
    }

    [Theory]
    [InlineData("KAB. SLEMAN")]
    [InlineData("KABUPATEN BANTUL")]
    [InlineData("KOTA FIKSI")]
    [InlineData("Atlantis")]
    public void CityNotInList_ReturnsNull(string input)
    {
        Assert.Null(CityNameMatcher.Match(input, Cities));
    }

    [Theory]
    [InlineData("JAKARTABARAT")]   // bukan kata utuh
    [InlineData("MALANGBONG")]
    [InlineData("KOTABANDUNG")]    // "KOTA" tanpa spasi bukan awalan
    public void PartOfWord_DoesNotMatch(string input)
    {
        Assert.Null(CityNameMatcher.Match(input, Cities));
    }

    [Fact]
    public void ProvinceAfterComma_IsNotUsedForWordMatch()
    {
        // Kab. Sleman tidak ada di daftar; "YOGYAKARTA" di belakang koma tidak boleh dipakai.
        Assert.Null(CityNameMatcher.Match("SLEMAN, DI YOGYAKARTA", Cities));
    }

    [Fact]
    public void SeveralWordMatches_PickLongestName()
    {
        City[] cities = [Lampung, BandarLampung];

        Assert.Same(BandarLampung, CityNameMatcher.Match("KOTA BANDAR LAMPUNG SELATAN", cities));
        Assert.Same(Lampung, CityNameMatcher.Match("LAMPUNG TIMUR", cities));
    }

    [Fact]
    public void ExactMatch_BeatsWordMatch()
    {
        City[] cities = [BandarLampung, Lampung];

        Assert.Same(Lampung, CityNameMatcher.Match("LAMPUNG", cities));
    }

    [Fact]
    public void DuplicateNames_PickSmallestCityId()
    {
        var second = City(2, "Jakarta");
        City[] cities = [second, Jakarta];

        Assert.Same(Jakarta, CityNameMatcher.Match("JAKARTA", cities));
        Assert.Same(Jakarta, CityNameMatcher.Match("JAKARTA BARAT", cities));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("KOTA")]
    [InlineData("KAB.")]
    [InlineData(", JAWA BARAT")]
    public void EmptyInput_ReturnsNull(string? input)
    {
        Assert.Null(CityNameMatcher.Match(input, Cities));
    }

    [Fact]
    public void EmptyCityList_ReturnsNull()
    {
        Assert.Null(CityNameMatcher.Match("Jakarta", []));
    }

    [Fact]
    public void NullCityList_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CityNameMatcher.Match("Jakarta", null!));
    }
}
