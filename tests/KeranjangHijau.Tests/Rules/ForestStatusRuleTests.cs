using System.Globalization;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class ForestStatusRuleTests
{
    // Batas di CLAUDE.md 7.7: 80–100 Lestari, 60–79,99 Sehat, 40–59,99 Menipis, 20–39,99 Terancam, 0–19,99 Kritis.
    [Theory]
    [InlineData("100", ForestStatus.Lestari)]
    [InlineData("80", ForestStatus.Lestari)]
    [InlineData("79.99", ForestStatus.Sehat)]
    [InlineData("60", ForestStatus.Sehat)]
    [InlineData("59.99", ForestStatus.Menipis)]
    [InlineData("40", ForestStatus.Menipis)]
    [InlineData("39.99", ForestStatus.Terancam)]
    [InlineData("20", ForestStatus.Terancam)]
    [InlineData("19.99", ForestStatus.Kritis)]
    [InlineData("0", ForestStatus.Kritis)]
    public void GetStatus_AtEachBoundary(string kesehatan, ForestStatus expected)
    {
        Assert.Equal(expected, ForestStatusRule.GetStatus(decimal.Parse(kesehatan, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("79.999")]
    [InlineData("79.9999999")]
    public void GetStatus_JustBelowBoundary_IsLowerStatus(string kesehatan)
    {
        Assert.Equal(ForestStatus.Sehat, ForestStatusRule.GetStatus(decimal.Parse(kesehatan, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("100.01")]
    public void GetStatus_OutOfRange_Throws(string kesehatan)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ForestStatusRule.GetStatus(decimal.Parse(kesehatan, CultureInfo.InvariantCulture)));
    }
}
