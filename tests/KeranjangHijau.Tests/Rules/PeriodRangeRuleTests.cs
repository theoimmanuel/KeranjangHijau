using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class PeriodRangeRuleTests
{
    private static DateTimeOffset Utc(int y, int mo, int d, int h = 0, int mi = 0, int s = 0) =>
        new(y, mo, d, h, mi, s, TimeSpan.Zero);

    // =================================================================== Zona waktu

    [Fact]
    public void JakartaTimeZone_IsUtcPlus7_WithoutDaylightSaving()
    {
        Assert.Equal(TimeSpan.FromHours(7), PeriodRangeRule.JakartaTimeZone.GetUtcOffset(Utc(2025, 1, 15)));
        Assert.Equal(TimeSpan.FromHours(7), PeriodRangeRule.JakartaTimeZone.GetUtcOffset(Utc(2025, 7, 15)));
    }

    [Theory]
    [InlineData("2025-03-02T16:59:59Z", "2025-03-02")] // Minggu 23.59.59 WIB
    [InlineData("2025-03-02T17:00:00Z", "2025-03-03")] // Senin 00.00 WIB
    [InlineData("2025-02-28T17:00:00Z", "2025-03-01")] // pergantian bulan di WIB, masih Februari di UTC
    [InlineData("2025-12-31T17:00:00Z", "2026-01-01")] // pergantian tahun
    [InlineData("2025-03-03T08:00:00+09:00", "2025-03-03")] // offset lain (WIT) tetap dikonversi ke WIB
    [InlineData("2025-03-03T00:30:00+09:00", "2025-03-02")] // 00.30 WIT = 22.30 WIB hari sebelumnya
    public void ToJakartaDate_UsesWibCalendarDate(string instant, string expected)
    {
        Assert.Equal(DateOnly.Parse(expected), PeriodRangeRule.ToJakartaDate(DateTimeOffset.Parse(instant)));
    }

    [Fact]
    public void StartOfJakartaDayUtc_IsPreviousDay17Utc()
    {
        var start = PeriodRangeRule.StartOfJakartaDayUtc(new DateOnly(2025, 3, 1));

        Assert.Equal(Utc(2025, 2, 28, 17), start);
        Assert.Equal(TimeSpan.Zero, start.Offset);
    }

    // =================================================================== Mingguan

    [Theory]
    [InlineData("2025-03-03", "2025-03-03", "2025-03-09")] // Senin → minggu itu sendiri
    [InlineData("2025-03-05", "2025-03-03", "2025-03-09")] // Rabu
    [InlineData("2025-03-09", "2025-03-03", "2025-03-09")] // Minggu → Senin sebelumnya, bukan sesudahnya
    [InlineData("2025-03-01", "2025-02-24", "2025-03-02")] // minggu melewati pergantian bulan
    [InlineData("2025-12-31", "2025-12-29", "2026-01-04")] // minggu melewati pergantian tahun
    [InlineData("2024-02-29", "2024-02-26", "2024-03-03")] // tahun kabisat
    public void Weekly_IsMondayToSunday(string reference, string monday, string sunday)
    {
        var range = PeriodRangeRule.Calculate(ReportPeriod.Mingguan, DateOnly.Parse(reference));

        Assert.Equal(ReportPeriod.Mingguan, range.Period);
        Assert.Equal(DateOnly.Parse(monday), range.TanggalMulai);
        Assert.Equal(DateOnly.Parse(sunday), range.TanggalSelesai);
        Assert.Equal(DayOfWeek.Monday, range.TanggalMulai!.Value.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, range.TanggalSelesai.DayOfWeek);
    }

    [Fact]
    public void Weekly_UtcBounds_AreWibMidnights()
    {
        var range = PeriodRangeRule.Calculate(ReportPeriod.Mingguan, new DateOnly(2025, 3, 5));

        // Senin 3 Maret 00.00 WIB = Minggu 2 Maret 17.00 UTC; batas akhir = Senin 10 Maret 00.00 WIB (eksklusif).
        Assert.Equal(Utc(2025, 3, 2, 17), range.MulaiUtc);
        Assert.Equal(Utc(2025, 3, 9, 17), range.SelesaiUtc);
        Assert.Equal(TimeSpan.FromDays(7), range.SelesaiUtc - range.MulaiUtc!.Value);
    }

    [Fact]
    public void Weekly_Contains_StartInclusive_EndExclusive()
    {
        var range = PeriodRangeRule.Calculate(ReportPeriod.Mingguan, new DateOnly(2025, 3, 5));

        Assert.False(range.Contains(Utc(2025, 3, 2, 16, 59, 59))); // Minggu 23.59.59 WIB minggu lalu
        Assert.True(range.Contains(Utc(2025, 3, 2, 17)));           // Senin 00.00 WIB
        Assert.True(range.Contains(Utc(2025, 3, 9, 16, 59, 59)));   // Minggu 23.59.59 WIB
        Assert.False(range.Contains(Utc(2025, 3, 9, 17)));          // Senin berikutnya 00.00 WIB
    }

    [Fact]
    public void Weekly_FromInstant_UsesWibDate_NotUtcDate()
    {
        // Minggu 2 Maret 17.30 UTC sudah Senin 3 Maret 00.30 WIB → minggu baru.
        var range = PeriodRangeRule.Calculate(ReportPeriod.Mingguan, Utc(2025, 3, 2, 17, 30));

        Assert.Equal(new DateOnly(2025, 3, 3), range.TanggalMulai);

        // Minggu 2 Maret 16.30 UTC masih Minggu 23.30 WIB → minggu sebelumnya.
        var previous = PeriodRangeRule.Calculate(ReportPeriod.Mingguan, Utc(2025, 3, 2, 16, 30));
        Assert.Equal(new DateOnly(2025, 2, 24), previous.TanggalMulai);
    }

    // =================================================================== Bulanan

    [Theory]
    [InlineData("2025-01-15", "2025-01-01", "2025-01-31")]
    [InlineData("2025-02-01", "2025-02-01", "2025-02-28")]
    [InlineData("2025-02-28", "2025-02-01", "2025-02-28")]
    [InlineData("2024-02-10", "2024-02-01", "2024-02-29")] // kabisat
    [InlineData("2025-04-30", "2025-04-01", "2025-04-30")]
    [InlineData("2025-12-31", "2025-12-01", "2025-12-31")]
    public void Monthly_IsCalendarMonth(string reference, string first, string last)
    {
        var range = PeriodRangeRule.Calculate(ReportPeriod.Bulanan, DateOnly.Parse(reference));

        Assert.Equal(ReportPeriod.Bulanan, range.Period);
        Assert.Equal(DateOnly.Parse(first), range.TanggalMulai);
        Assert.Equal(DateOnly.Parse(last), range.TanggalSelesai);
    }

    [Fact]
    public void Monthly_UtcBounds_CrossUtcMonthBoundary()
    {
        var range = PeriodRangeRule.Calculate(ReportPeriod.Bulanan, new DateOnly(2025, 3, 15));

        // 1 Maret 00.00 WIB = 28 Februari 17.00 UTC; 1 April 00.00 WIB = 31 Maret 17.00 UTC.
        Assert.Equal(Utc(2025, 2, 28, 17), range.MulaiUtc);
        Assert.Equal(Utc(2025, 3, 31, 17), range.SelesaiUtc);

        Assert.True(range.Contains(Utc(2025, 2, 28, 17, 30)));  // masih Februari di UTC, sudah Maret di WIB
        Assert.False(range.Contains(Utc(2025, 3, 31, 17, 30))); // masih Maret di UTC, sudah April di WIB
    }

    [Fact]
    public void Monthly_December_EndsAtNewYearWib()
    {
        var range = PeriodRangeRule.Calculate(ReportPeriod.Bulanan, new DateOnly(2025, 12, 10));

        Assert.Equal(Utc(2025, 11, 30, 17), range.MulaiUtc);
        Assert.Equal(Utc(2025, 12, 31, 17), range.SelesaiUtc);
    }

    [Fact]
    public void Monthly_FromInstant_LastUtcEveningOfMonth_IsNextMonth()
    {
        // 31 Maret 17.00 UTC = 1 April 00.00 WIB.
        var range = PeriodRangeRule.Calculate(ReportPeriod.Bulanan, Utc(2025, 3, 31, 17));

        Assert.Equal(new DateOnly(2025, 4, 1), range.TanggalMulai);
        Assert.Equal(new DateOnly(2025, 4, 30), range.TanggalSelesai);
    }

    // =================================================================== Semua

    [Fact]
    public void All_HasNoStart_AndEndsAfterReferenceDay()
    {
        var range = PeriodRangeRule.Calculate(ReportPeriod.Semua, new DateOnly(2025, 3, 5));

        Assert.Equal(ReportPeriod.Semua, range.Period);
        Assert.Null(range.TanggalMulai);
        Assert.Null(range.MulaiUtc);
        Assert.Equal(new DateOnly(2025, 3, 5), range.TanggalSelesai);
        Assert.Equal(Utc(2025, 3, 5, 17), range.SelesaiUtc);
        Assert.True(range.Contains(Utc(2000, 1, 1)));
        Assert.False(range.Contains(Utc(2025, 3, 5, 17)));
    }

    // =================================================================== Argumen salah

    [Fact]
    public void UnknownPeriod_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PeriodRangeRule.Calculate((ReportPeriod)99, new DateOnly(2025, 3, 5)));
    }

    [Fact]
    public void ReferenceDateOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PeriodRangeRule.Calculate(ReportPeriod.Bulanan, DateOnly.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PeriodRangeRule.Calculate(ReportPeriod.Mingguan, DateOnly.MinValue));
    }
}
