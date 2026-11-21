using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class ResiNormalizerTests
{
    [Theory]
    [InlineData("JP1234567890", "JP1234567890")]
    [InlineData("jp1234567890", "JP1234567890")]
    [InlineData("  jp 1234 5678 90  ", "JP1234567890")]
    [InlineData("\tJp12\n3456\r78 ", "JP12345678")]
    [InlineData("   ", "")]
    [InlineData("", "")]
    public void Normalize_RemovesWhitespaceAndUppercases(string input, string expected)
    {
        Assert.Equal(expected, ResiNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_KeepsSymbols_SoFormatRuleCanRejectThem()
    {
        Assert.Equal("JP-1234/5678", ResiNormalizer.Normalize("jp-1234/5678"));
    }

    [Fact]
    public void Normalize_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ResiNormalizer.Normalize(null!));
    }
}
