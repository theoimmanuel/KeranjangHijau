using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class ForestRecoveryRuleTests
{
    private static readonly DateTimeOffset Now = new(2025, 3, 10, 1, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------- Hari penuh

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]                 // 1 menit
    [InlineData(24 * 60 - 1, 0)]       // 23 jam 59 menit
    [InlineData(24 * 60, 1)]           // tepat 1 hari
    [InlineData(84 * 60, 3)]           // 3,5 hari
    [InlineData(-60, 0)]               // updated_at di masa depan
    public void FullDaysBetween_CountsWholeDays(int minutes, int expected)
    {
        Assert.Equal(expected, ForestRecoveryRule.FullDaysBetween(Now.AddMinutes(-minutes), Now));
    }

    // ---------------------------------------------------------------- Calculate

    [Fact]
    public void ZeroFullDays_NoChange_UpdatedAtUnchanged()
    {
        var updatedAt = Now.AddHours(-23);

        var result = ForestRecoveryRule.Calculate(50m, updatedAt, Now, 1m);

        Assert.Equal(new ForestRecovery(0, 0m, 50m, updatedAt), result);
        Assert.False(result.HasHealthChange);
    }

    [Fact]
    public void OneFullDay_AddsOnePoint()
    {
        var result = ForestRecoveryRule.Calculate(50m, Now.AddDays(-1), Now, 1m);

        Assert.Equal(new ForestRecovery(1, 1m, 51m, Now), result);
        Assert.True(result.HasHealthChange);
    }

    [Fact]
    public void ThreeAndHalfDays_AddsThreePoints_AndAdvancesUpdatedAtByThreeDays()
    {
        var updatedAt = Now.AddDays(-3.5);

        var result = ForestRecoveryRule.Calculate(50m, updatedAt, Now, 1m);

        Assert.Equal(3, result.HariPenuh);
        Assert.Equal(3m, result.Perubahan);
        Assert.Equal(53m, result.KesehatanBaru);
        Assert.Equal(updatedAt.AddDays(3), result.UpdatedAtBaru);
        Assert.Equal(TimeSpan.FromHours(12), Now - result.UpdatedAtBaru); // sisa 0,5 hari tidak hilang
    }

    [Fact]
    public void AlreadyFull_NoChange_UpdatedAtBecomesNow()
    {
        var result = ForestRecoveryRule.Calculate(100m, Now.AddDays(-7.25), Now, 1m);

        Assert.Equal(new ForestRecovery(7, 0m, 100m, Now), result);
        Assert.False(result.HasHealthChange);
    }

    [Fact]
    public void AlreadyFull_ZeroDays_UpdatedAtStillBecomesNow()
    {
        Assert.Equal(Now, ForestRecoveryRule.Calculate(100m, Now.AddHours(-3), Now, 1m).UpdatedAtBaru);
    }

    [Fact]
    public void Recovery_IsCappedAt100()
    {
        var updatedAt = Now.AddDays(-30);

        var result = ForestRecoveryRule.Calculate(95.5m, updatedAt, Now, 1m);

        Assert.Equal(new ForestRecovery(30, 4.5m, 100m, updatedAt.AddDays(30)), result);
    }

    [Fact]
    public void ConfiguredPointsPerDay_AreUsed_AndRoundedToTwoDecimals()
    {
        var result = ForestRecoveryRule.Calculate(10m, Now.AddDays(-3), Now, 0.333m); // 3 × 0,333 = 0,999 → 1,00

        Assert.Equal(1m, result.Perubahan);
        Assert.Equal(11m, result.KesehatanBaru);
    }

    [Fact]
    public void ZeroPointsPerDay_NoHealthChange_ButUpdatedAtAdvances()
    {
        var updatedAt = Now.AddDays(-2);

        var result = ForestRecoveryRule.Calculate(10m, updatedAt, Now, 0m);

        Assert.Equal(new ForestRecovery(2, 0m, 10m, Now), result);
        Assert.False(result.HasHealthChange);
    }

    [Fact]
    public void FromZeroHealth_Recovers()
    {
        Assert.Equal(2m, ForestRecoveryRule.Calculate(0m, Now.AddDays(-2), Now, 1m).KesehatanBaru);
    }

    [Fact]
    public void FutureUpdatedAt_NoChange()
    {
        var updatedAt = Now.AddHours(2);

        Assert.Equal(new ForestRecovery(0, 0m, 40m, updatedAt), ForestRecoveryRule.Calculate(40m, updatedAt, Now, 1m));
    }

    [Fact]
    public void InvalidInput_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestRecoveryRule.Calculate(-0.01m, Now, Now, 1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestRecoveryRule.Calculate(100.01m, Now, Now, 1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestRecoveryRule.Calculate(50m, Now, Now, -1m));
    }
}
