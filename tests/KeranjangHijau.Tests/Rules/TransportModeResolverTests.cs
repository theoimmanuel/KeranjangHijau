using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Rules;

public class TransportModeResolverTests
{
    private static TransportModeResolver CreateResolver(params string[] keywords)
    {
        var options = new EmissionOptions();
        options.ExpressKeywords.AddRange(keywords.Length > 0 ? keywords : EmissionOptions.DefaultExpressKeywords);
        return new TransportModeResolver(Options.Create(options));
    }

    // ---------------------------------------------------------------- Satu pulau → truk

    [Theory]
    [InlineData("REG")]
    [InlineData("YES")]
    [InlineData(null)]
    public void SameIsland_IsTruk_RegardlessOfService(string? layanan)
    {
        Assert.Equal(TransportMode.Truk, CreateResolver().Resolve("Jawa", "Jawa", layanan));
    }

    [Fact]
    public void SameIsland_ComparisonIgnoresCaseAndSpaces()
    {
        Assert.Equal(TransportMode.Truk, CreateResolver().Resolve("Jawa", "  JAWA ", "YES"));
    }

    // ---------------------------------------------------------------- Beda pulau

    [Fact]
    public void DifferentIsland_RegularService_IsKapal()
    {
        Assert.Equal(TransportMode.Kapal, CreateResolver().Resolve("Jawa", "Sumatra", "REG"));
    }

    [Fact]
    public void DifferentIsland_YesService_IsPesawat()
    {
        Assert.Equal(TransportMode.Pesawat, CreateResolver().Resolve("Jawa", "Sumatra", "YES"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DifferentIsland_NoService_IsTreatedAsRegular(string? layanan)
    {
        Assert.Equal(TransportMode.Kapal, CreateResolver().Resolve("Jawa", "Kalimantan", layanan));
    }

    [Theory]
    [InlineData("REG")]
    [InlineData("OKE")]
    [InlineData("EZ")]
    [InlineData("Reguler")]
    [InlineData("ECO")]
    public void DifferentIsland_RegularServiceNames_IsKapal(string layanan)
    {
        Assert.Equal(TransportMode.Kapal, CreateResolver().Resolve("Jawa", "Sulawesi", layanan));
    }

    [Fact]
    public void KeywordInsideLongerName_CountsAsExpress()
    {
        // Kata kunci dicocokkan sebagai substring: "Pos Kilat Khusus" mengandung KILAT (lihat catatan M2.2).
        Assert.Equal(TransportMode.Pesawat, CreateResolver().Resolve("Jawa", "Sulawesi", "Pos Kilat Khusus"));
    }

    [Theory]
    [InlineData("YES")]
    [InlineData("ONS")]
    [InlineData("SDS")]
    [InlineData("BEST")]
    [InlineData("EXPRESS")]
    [InlineData("KILAT")]
    [InlineData("NEXT DAY")]
    [InlineData("SAME DAY")]
    public void DifferentIsland_EachDefaultKeyword_IsPesawat(string layanan)
    {
        Assert.Equal(TransportMode.Pesawat, CreateResolver().Resolve("Jawa", "Bali", layanan));
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("Yes")]
    [InlineData("SiCepat BEST")]
    [InlineData("Paket Express Nasional")]
    [InlineData("next day")]
    public void ExpressKeyword_IsCaseInsensitive_AndMatchesInsideName(string layanan)
    {
        Assert.Equal(TransportMode.Pesawat, CreateResolver().Resolve("Jawa", "Papua", layanan));
    }

    // ---------------------------------------------------------------- Kata kunci dari konfigurasi

    [Fact]
    public void Keywords_ComeFromOptions()
    {
        var resolver = CreateResolver("PRIORITAS");

        Assert.Equal(TransportMode.Pesawat, resolver.Resolve("Jawa", "Sumatra", "Prioritas"));
        Assert.Equal(TransportMode.Kapal, resolver.Resolve("Jawa", "Sumatra", "YES")); // bukan kata kunci lagi
    }

    [Fact]
    public void EmptyKeywordList_FallsBackToDefaults()
    {
        var resolver = new TransportModeResolver(Options.Create(new EmissionOptions()));

        Assert.Equal(TransportMode.Pesawat, resolver.Resolve("Jawa", "Sumatra", "YES"));
        Assert.Equal(TransportMode.Kapal, resolver.Resolve("Jawa", "Sumatra", "REG"));
    }

    [Theory]
    [InlineData(null, "Jawa")]
    [InlineData("", "Jawa")]
    [InlineData("Jawa", " ")]
    public void BlankIsland_Throws(string? pulauAsal, string? pulauTujuan)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateResolver().Resolve(pulauAsal!, pulauTujuan!, "REG"));
    }
}
