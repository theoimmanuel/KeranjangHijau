using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class EmailNormalizerTests
{
    [Theory]
    [InlineData("budi@contoh.id", "budi@contoh.id")]
    [InlineData("  Budi@Contoh.ID  ", "budi@contoh.id")]
    [InlineData("\tSITI@MAIL.COM\n", "siti@mail.com")]
    [InlineData("", "")]
    public void Normalize_TrimsAndLowercases(string input, string expected)
    {
        Assert.Equal(expected, EmailNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => EmailNormalizer.Normalize(null!));
    }
}
