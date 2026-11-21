using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class CredentialRulesTests
{
    [Theory]
    [InlineData("budi@contoh.id")]
    [InlineData("budi.santoso+belanja@mail.ugm.ac.id")]
    [InlineData("admin@keranjanghijau.local")]
    [InlineData("a_b-c@sub-domain.com")]
    public void IsValidEmail_AcceptsCommonFormats(string email)
    {
        Assert.True(CredentialRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("budi")]
    [InlineData("budi@")]
    [InlineData("@contoh.id")]
    [InlineData("budi@contoh")]
    [InlineData("budi@contoh.i")]
    [InlineData("budi@@contoh.id")]
    [InlineData("budi santoso@contoh.id")]
    public void IsValidEmail_RejectsInvalidFormats(string? email)
    {
        Assert.False(CredentialRules.IsValidEmail(email));
    }

    [Fact]
    public void IsValidEmail_RejectsTooLong()
    {
        var email = new string('a', 250) + "@x.id"; // 255 karakter

        Assert.False(CredentialRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("abcdefg1", true)]
    [InlineData("12345678a", true)]
    [InlineData("Pässwörd9", true)]
    [InlineData("abcdef1", false)]   // 7 karakter
    [InlineData("abcdefgh", false)]  // tanpa angka
    [InlineData("12345678", false)]  // tanpa huruf
    [InlineData("!!!!!!!!", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStrongPassword(string? password, bool expected)
    {
        Assert.Equal(expected, CredentialRules.IsStrongPassword(password));
    }

    [Theory]
    [InlineData(72, false)]
    [InlineData(73, true)]
    public void IsPasswordTooLong_UsesUtf8Bytes(int asciiLength, bool expected)
    {
        Assert.Equal(expected, CredentialRules.IsPasswordTooLong(new string('a', asciiLength)));
    }

    [Fact]
    public void IsPasswordTooLong_CountsMultiByteCharacters()
    {
        // 'é' = 2 byte UTF-8 → 37 karakter = 74 byte.
        Assert.True(CredentialRules.IsPasswordTooLong(new string('é', 37)));
    }
}
