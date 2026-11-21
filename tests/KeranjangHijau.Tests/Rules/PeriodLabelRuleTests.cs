using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class PeriodLabelRuleTests
{
    [Theory]
    [InlineData("2026-11-01", "2026-11-30", "November 2026")]
    [InlineData("2025-02-01", "2025-02-28", "Februari 2025")]
    [InlineData("2025-05-01", "2025-05-31", "Mei 2025")]
    public void Monthly_IsMonthNameAndYear(string mulai, string selesai, string expected)
    {
        Assert.Equal(expected, PeriodLabelRule.Create(ReportPeriod.Bulanan, DateOnly.Parse(mulai), DateOnly.Parse(selesai)));
    }

    [Theory]
    [InlineData("2026-11-16", "2026-11-22", "16–22 November 2026")]
    [InlineData("2025-02-24", "2025-03-02", "24 Februari – 2 Maret 2025")]
    [InlineData("2025-12-29", "2026-01-04", "29 Desember 2025 – 4 Januari 2026")]
    public void Weekly_IsCompactDateRange(string mulai, string selesai, string expected)
    {
        Assert.Equal(expected, PeriodLabelRule.Create(ReportPeriod.Mingguan, DateOnly.Parse(mulai), DateOnly.Parse(selesai)));
    }

    [Fact]
    public void All_StartsWithSejak()
    {
        Assert.Equal(
            "Sejak 3 Maret 2026",
            PeriodLabelRule.Create(ReportPeriod.Semua, new DateOnly(2026, 3, 3), new DateOnly(2026, 11, 20)));
    }

    [Fact]
    public void MonthNames_AreIndonesian()
    {
        Assert.Equal(
            ["Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "November", "Desember"],
            Enumerable.Range(1, 12).Select(PeriodLabelRule.MonthName));
        Assert.Throws<ArgumentOutOfRangeException>(() => PeriodLabelRule.MonthName(13));
    }

    [Fact]
    public void FormatRange_SameDay_IsSingleDate()
    {
        Assert.Equal("5 Maret 2025", PeriodLabelRule.FormatRange(new DateOnly(2025, 3, 5), new DateOnly(2025, 3, 5)));
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentException>(
            () => PeriodLabelRule.Create(ReportPeriod.Mingguan, new DateOnly(2025, 3, 9), new DateOnly(2025, 3, 3)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PeriodLabelRule.Create((ReportPeriod)9, new DateOnly(2025, 3, 3), new DateOnly(2025, 3, 9)));
    }
}
