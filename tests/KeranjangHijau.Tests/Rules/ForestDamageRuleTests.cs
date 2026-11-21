using System.Globalization;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class ForestDamageRuleTests
{
    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    // ---------------------------------------------------------------- CalculateChange

    [Theory]
    [InlineData("1.5", "1", "-15")]       // 1,5 kg = 15 × 100 g
    [InlineData("0.1", "1", "-1")]        // 100 g = −1 poin
    [InlineData("0.0123", "1", "-0.12")]  // 0,123 → 0,12
    [InlineData("0.0125", "1", "-0.13")]  // 0,125 → 0,13 (AwayFromZero)
    [InlineData("2", "0.5", "-10")]       // poin per 100 g dari konfigurasi
    [InlineData("0", "1", "0")]
    [InlineData("3", "0", "0")]
    public void CalculateChange_FollowsFormula(string totalCo2eKg, string points, string expected)
    {
        Assert.Equal(D(expected), ForestDamageRule.CalculateChange(D(totalCo2eKg), D(points)));
    }

    [Fact]
    public void CalculateChange_Zero_IsNotNegativeZero()
    {
        Assert.Equal("0", ForestDamageRule.CalculateChange(0m, 1m).ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CalculateChange_NegativeInput_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestDamageRule.CalculateChange(-0.1m, 1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestDamageRule.CalculateChange(1m, -1m));
    }

    // ---------------------------------------------------------------- Apply

    [Fact]
    public void Apply_ReducesHealth()
    {
        var result = ForestDamageRule.Apply(100m, 1.5m, 1m);

        Assert.Equal(new ForestDamage(-15m, -15m, 85m), result);
    }

    [Fact]
    public void Apply_RoundsChangeToTwoDecimals()
    {
        var result = ForestDamageRule.Apply(50m, 0.01234m, 1m); // 0,1234 → 0,12

        Assert.Equal(-0.12m, result.Perubahan);
        Assert.Equal(49.88m, result.KesehatanBaru);
    }

    [Fact]
    public void Apply_HealthNeverBelowZero_AppliedChangeIsClamped()
    {
        var result = ForestDamageRule.Apply(12.5m, 4m, 1m); // nominal −40

        Assert.Equal(-40m, result.PerubahanNominal);
        Assert.Equal(-12.5m, result.Perubahan);
        Assert.Equal(0m, result.KesehatanBaru);
    }

    [Fact]
    public void Apply_ExactlyToZero()
    {
        var result = ForestDamageRule.Apply(20m, 2m, 1m);

        Assert.Equal(new ForestDamage(-20m, -20m, 0m), result);
    }

    [Fact]
    public void Apply_AlreadyZero_StaysZero_WithZeroChange()
    {
        var result = ForestDamageRule.Apply(0m, 1m, 1m);

        Assert.Equal(0m, result.KesehatanBaru);
        Assert.Equal("0", result.Perubahan.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(-10m, result.PerubahanNominal);
    }

    [Fact]
    public void Apply_ZeroEmission_NoChange()
    {
        Assert.Equal(new ForestDamage(0m, 0m, 73.4m), ForestDamageRule.Apply(73.4m, 0m, 1m));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("100.01")]
    public void Apply_HealthOutOfRange_Throws(string health)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestDamageRule.Apply(D(health), 1m, 1m));
    }
}
