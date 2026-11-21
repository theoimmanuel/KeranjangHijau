using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class PhoneSuffixRuleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData(" 00007 ")]
    public void EmptyOrFiveDigits_HasNoError(string? input)
    {
        Assert.Null(PhoneSuffixRule.GetError(input));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("12a45")]
    [InlineData("12 45")]
    [InlineData("-1234")]
    [InlineData("１２３４５")] // digit lebar penuh (bukan ASCII)
    public void OtherInput_ReturnsIndonesianMessage_WithoutTheValue(string input)
    {
        var error = PhoneSuffixRule.GetError(input);

        Assert.Equal("Isi 5 digit terakhir nomor HP penerima.", error);
        Assert.DoesNotContain(input.Trim(), error);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData(" 12345 ", "12345")]
    public void Normalize_TrimsAndTurnsEmptyIntoNull(string? input, string? expected)
    {
        Assert.Equal(expected, PhoneSuffixRule.Normalize(input));
    }

    [Fact]
    public void TrackingRequest_Normalize_KeepsOnlyValidPhone()
    {
        Assert.Equal(new TrackingRequest("jne", "JNE1001000003", "12345"),
            new TrackingRequest(" JNE ", "jne 1001 000003", " 12345 ").Normalize());
        Assert.Null(new TrackingRequest("jne", "JNE1", "1234").Normalize().NomorHpAkhir);
        Assert.Null(new TrackingRequest("jne", "JNE1", "  ").Normalize().NomorHpAkhir);
    }

    [Fact]
    public void ToString_MasksPhone()
    {
        var tracking = new TrackingRequest("jne", "JNE1001000003", "98765").ToString();
        var emission = new EmissionRequest("jne", "JNE1001000003") { NomorHpAkhir = "98765" }.ToString();

        Assert.DoesNotContain("98765", tracking);
        Assert.DoesNotContain("98765", emission);
        Assert.Contains("JNE1001000003", tracking);
        Assert.Contains("*****", emission);
    }
}
