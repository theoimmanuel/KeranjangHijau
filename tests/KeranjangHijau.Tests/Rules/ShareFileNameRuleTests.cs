using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class ShareFileNameRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 2, 30, 15, TimeSpan.Zero); // 09.30.15 WIB

    [Theory]
    [InlineData(ReportPeriod.Mingguan, "keranjanghijau-mingguan-20261120093015.png")]
    [InlineData(ReportPeriod.Bulanan, "keranjanghijau-bulanan-20261120093015.png")]
    [InlineData(ReportPeriod.Semua, "keranjanghijau-semua-20261120093015.png")]
    public void Create_UsesPeriodAndWibTimestamp(ReportPeriod period, string expected)
    {
        var name = ShareFileNameRule.Create(period, Now);

        Assert.Equal(expected, name);
        Assert.True(ShareFileNameRule.IsValid(name));
    }

    [Fact]
    public void Create_UsesWibDate_AcrossUtcMidnight()
    {
        // 31 Desember 2025 17.05 UTC = 1 Januari 2026 00.05 WIB.
        var name = ShareFileNameRule.Create(ReportPeriod.Bulanan, new DateTimeOffset(2025, 12, 31, 17, 5, 0, TimeSpan.Zero));

        Assert.Equal("keranjanghijau-bulanan-20260101000500.png", name);
    }

    [Fact]
    public void Create_DuplicateIndex_AddsSuffix()
    {
        Assert.Equal("keranjanghijau-semua-20261120093015-1.png", ShareFileNameRule.Create(ReportPeriod.Semua, Now, 1));
        Assert.Equal("keranjanghijau-semua-20261120093015-12.png", ShareFileNameRule.Create(ReportPeriod.Semua, Now, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShareFileNameRule.Create(ReportPeriod.Semua, Now, -1));
    }

    [Fact]
    public void Create_IsSafeFileName_OnAllPlatforms()
    {
        foreach (var period in Enum.GetValues<ReportPeriod>())
        {
            var name = ShareFileNameRule.Create(period, Now, 3);

            Assert.Equal(-1, name.IndexOfAny(Path.GetInvalidFileNameChars()));
            Assert.Equal(-1, name.IndexOfAny(['\\', '/', ':', '*', '?', '"', '<', '>', '|', ' ']));
            Assert.Equal(name.ToLowerInvariant(), name);
            Assert.Equal(name, Path.GetFileName(name));
            Assert.EndsWith(".png", name);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("keranjanghijau-bulanan-20261120093015.jpg")]
    [InlineData("keranjanghijau-tahunan-20261120093015.png")]
    [InlineData("keranjanghijau-bulanan-2026112009301.png")]
    [InlineData("keranjanghijau-bulanan-20261120093015-0.png")]
    [InlineData("../keranjanghijau-bulanan-20261120093015.png")]
    [InlineData("KeranjangHijau-bulanan-20261120093015.png")]
    public void IsValid_RejectsOtherNames(string? name)
    {
        Assert.False(ShareFileNameRule.IsValid(name));
    }

    [Fact]
    public void UnknownPeriod_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShareFileNameRule.Create((ReportPeriod)7, Now));
    }
}
