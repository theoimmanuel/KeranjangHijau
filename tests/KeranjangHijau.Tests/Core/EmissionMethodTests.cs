using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Tests.Core;

public class EmissionMethodTests
{
    [Theory]
    [InlineData(EmissionMethod.Api, "api")]
    [InlineData(EmissionMethod.RumusLokal, "rumus_lokal")]
    public void ToDbValue_ReturnsSchemaText(EmissionMethod method, string expected)
    {
        Assert.Equal(expected, method.ToDbValue());
    }

    [Theory]
    [InlineData("api", EmissionMethod.Api)]
    [InlineData(" RUMUS_LOKAL ", EmissionMethod.RumusLokal)]
    public void ParseEmissionMethod_AcceptsDbText(string text, EmissionMethod expected)
    {
        Assert.Equal(expected, EmissionMethodExtensions.ParseEmissionMethod(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("rumuslokal")]
    [InlineData("manual")]
    public void ParseEmissionMethod_UnknownText_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => EmissionMethodExtensions.ParseEmissionMethod(text));
    }

    [Fact]
    public void ToDbValue_UndefinedEnumValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((EmissionMethod)99).ToDbValue());
    }

    [Fact]
    public void DbValues_MatchSchemaCheckConstraint()
    {
        // ck_emission_records_metode CHECK (metode IN ('api', 'rumus_lokal'))
        var values = Enum.GetValues<EmissionMethod>().Select(m => m.ToDbValue()).Order();

        Assert.Equal(["api", "rumus_lokal"], values);
    }
}
