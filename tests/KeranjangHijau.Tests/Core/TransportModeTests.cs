using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Tests.Core;

public class TransportModeTests
{
    [Theory]
    [InlineData(TransportMode.Truk, "truk")]
    [InlineData(TransportMode.Kapal, "kapal")]
    [InlineData(TransportMode.Pesawat, "pesawat")]
    public void ToDbValue_ReturnsLowercaseText(TransportMode mode, string expected)
    {
        Assert.Equal(expected, mode.ToDbValue());
    }

    [Theory]
    [InlineData("truk", TransportMode.Truk)]
    [InlineData("KAPAL", TransportMode.Kapal)]
    [InlineData("  Pesawat ", TransportMode.Pesawat)]
    public void ParseTransportMode_AcceptsDbText(string text, TransportMode expected)
    {
        Assert.Equal(expected, TransportModeExtensions.ParseTransportMode(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("kereta")]
    [InlineData("0")]
    public void ParseTransportMode_UnknownText_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => TransportModeExtensions.ParseTransportMode(text));
    }

    [Fact]
    public void TryParseTransportMode_Null_ReturnsFalse()
    {
        Assert.False(TransportModeExtensions.TryParseTransportMode(null, out _));
    }

    [Fact]
    public void ToDbValue_UndefinedEnumValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((TransportMode)99).ToDbValue());
    }

    [Fact]
    public void DbValues_MatchSchemaCheckConstraint()
    {
        // ck_emission_factors_moda CHECK (moda_transport IN ('truk', 'kapal', 'pesawat'))
        var values = Enum.GetValues<TransportMode>().Select(m => m.ToDbValue()).Order();

        Assert.Equal(["kapal", "pesawat", "truk"], values);
    }
}
