using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class WeightParserTests
{
    [Theory]
    [InlineData("1", 1)]
    [InlineData("1.5", 1.5)]
    [InlineData("1,5", 1.5)]
    [InlineData(" 2 ", 2)]
    [InlineData("1 kg", 1)]
    [InlineData("1kg", 1)]
    [InlineData("1,25 KG", 1.25)]
    [InlineData("2 kilogram", 2)]
    [InlineData("1000 gr", 1)]
    [InlineData("1000 gram", 1)]
    [InlineData("500g", 0.5)]
    [InlineData("250 GRAM", 0.25)]
    [InlineData("0.5", 0.5)]
    public void Text_IsReadAsKilograms(string text, double expected)
    {
        Assert.Equal((decimal)expected, WeightParser.Parse(text));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.5)]
    [InlineData(0.001)]
    public void Number_IsKilograms(double value)
    {
        Assert.Equal((decimal)value, WeightParser.Parse((decimal)value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("0,0")]
    [InlineData("0 gram")]
    [InlineData("-1")]
    [InlineData("-1 kg")]
    [InlineData("abc")]
    [InlineData("1 ton")]
    [InlineData("1.000,5")]
    [InlineData("1,5,0")]
    [InlineData("kg")]
    [InlineData("1 kg 2")]
    [InlineData("99999999999999999999999999999999")]
    public void UnreadableEmptyOrNotPositiveText_ReturnsNull(string? text)
    {
        Assert.Null(WeightParser.Parse(text));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotPositiveNumber_ReturnsNull(double value)
    {
        Assert.Null(WeightParser.Parse((decimal)value));
    }

    [Fact]
    public void NullNumber_ReturnsNull()
    {
        Assert.Null(WeightParser.Parse((decimal?)null));
    }

    [Fact]
    public void VeryLongText_ReturnsNull_WithoutThrowing()
    {
        Assert.Null(WeightParser.Parse(new string('1', 10_000)));
    }
}
