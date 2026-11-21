using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class ResiFormatRuleTests
{
    [Theory]
    [InlineData("JP123456")]                          // tepat 8 karakter
    [InlineData("123456789012345678901234567890")]    // tepat 30 karakter
    [InlineData("SICEPAT000111222")]
    public void GetError_ValidResi_ReturnsNull(string resi)
    {
        Assert.Null(ResiFormatRule.GetError(resi));
    }

    [Fact]
    public void GetError_Empty_ReturnsRequiredMessage()
    {
        Assert.Equal(ResiFormatRule.RequiredMessage, ResiFormatRule.GetError(string.Empty));
    }

    [Theory]
    [InlineData("JP12345")]                           // 7 karakter
    [InlineData("A")]
    [InlineData("1234567890123456789012345678901")]   // 31 karakter
    public void GetError_WrongLength_ReturnsLengthMessage(string resi)
    {
        Assert.Equal(ResiFormatRule.LengthMessage, ResiFormatRule.GetError(resi));
    }

    [Theory]
    [InlineData("JP-12345678")]
    [InlineData("JP12345678!")]
    [InlineData("JP_12345678")]
    [InlineData("JP1234567é")]                        // huruf non-ASCII ditolak, sama seperti CHECK di DB
    [InlineData("jp12345678")]                        // huruf kecil: input belum dinormalisasi
    public void GetError_InvalidCharacters_ReturnsCharactersMessage(string resi)
    {
        Assert.Equal(ResiFormatRule.CharactersMessage, ResiFormatRule.GetError(resi));
    }
}
