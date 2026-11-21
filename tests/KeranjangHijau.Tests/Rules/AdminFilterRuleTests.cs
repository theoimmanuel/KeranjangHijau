using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class AdminFilterRuleTests
{
    [Fact]
    public void EmptyFilter_NoCriteria_FirstPageOfDefaultSize()
    {
        var q = AdminFilterRule.Normalize(new AdminRecordFilter());

        Assert.Equal(new AdminRecordQuery(null, null, null, null, 1, 20), q);
        Assert.Equal(0, q.Offset);
    }

    [Fact]
    public void Dates_AreInclusiveJakartaDays_ConvertedToHalfOpenUtcRange()
    {
        var q = AdminFilterRule.Normalize(new AdminRecordFilter
        {
            TanggalMulai = new DateOnly(2026, 3, 2),
            TanggalSelesai = new DateOnly(2026, 3, 8),
        });

        Assert.Equal(new DateTimeOffset(2026, 3, 1, 17, 0, 0, TimeSpan.Zero), q.FromUtc); // 2 Maret 00.00 WIB
        Assert.Equal(new DateTimeOffset(2026, 3, 8, 17, 0, 0, TimeSpan.Zero), q.ToUtc);   // 9 Maret 00.00 WIB
    }

    [Fact]
    public void SameStartAndEndDate_CoversOneFullDay()
    {
        var day = new DateOnly(2026, 3, 4);

        var q = AdminFilterRule.Normalize(new AdminRecordFilter { TanggalMulai = day, TanggalSelesai = day });

        Assert.Equal(TimeSpan.FromDays(1), q.ToUtc - q.FromUtc);
    }

    [Fact]
    public void OnlyOneDate_LeavesOtherSideOpen()
    {
        var fromOnly = AdminFilterRule.Normalize(new AdminRecordFilter { TanggalMulai = new DateOnly(2026, 3, 4) });
        var toOnly = AdminFilterRule.Normalize(new AdminRecordFilter { TanggalSelesai = new DateOnly(2026, 3, 4) });

        Assert.NotNull(fromOnly.FromUtc);
        Assert.Null(fromOnly.ToUtc);
        Assert.Null(toOnly.FromUtc);
        Assert.NotNull(toOnly.ToUtc);
    }

    [Fact]
    public void StartAfterEnd_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => AdminFilterRule.Normalize(new AdminRecordFilter
        {
            TanggalMulai = new DateOnly(2026, 3, 9),
            TanggalSelesai = new DateOnly(2026, 3, 8),
        }));

        Assert.Equal("Tanggal mulai tidak boleh setelah tanggal selesai.", ex.Message);
        Assert.Equal("TanggalSelesai", ex.Field);
    }

    [Fact]
    public void DateOutsideSupportedRange_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(
            () => AdminFilterRule.Normalize(new AdminRecordFilter { TanggalSelesai = DateOnly.MaxValue }));

        Assert.Equal("TanggalSelesai", ex.Field);
    }

    [Theory]
    [InlineData(" JNE ", "jne")]
    [InlineData("SiCepat", "sicepat")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void KodeKurir_IsTrimmedAndLowercased_BlankMeansAll(string? input, string? expected)
    {
        Assert.Equal(expected, AdminFilterRule.Normalize(new AdminRecordFilter { KodeKurir = input }).KodeKurir);
    }

    [Theory]
    [InlineData("  Siti ", "Siti")]
    [InlineData("budi@", "budi@")]
    [InlineData(" ", null)]
    [InlineData(null, null)]
    public void KataKunci_IsTrimmed_BlankMeansAll(string? input, string? expected)
    {
        Assert.Equal(expected, AdminFilterRule.Normalize(new AdminRecordFilter { KataKunci = input }).KataKunci);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    public void Halaman_StartsAtOne(int input, int expected)
    {
        Assert.Equal(expected, AdminFilterRule.Normalize(new AdminRecordFilter { Halaman = input }).Halaman);
    }

    [Theory]
    [InlineData(20, 20)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(int.MaxValue, 100)]
    [InlineData(0, 20)]
    [InlineData(-5, 20)]
    public void UkuranHalaman_DefaultsTo20_AndIsCappedAt100(int input, int expected)
    {
        Assert.Equal(expected, AdminFilterRule.Normalize(new AdminRecordFilter { UkuranHalaman = input }).UkuranHalaman);
    }

    [Fact]
    public void Offset_IsComputedFromPage_WithoutOverflow()
    {
        Assert.Equal(40, AdminFilterRule.Normalize(new AdminRecordFilter { Halaman = 3, UkuranHalaman = 20 }).Offset);

        var huge = AdminFilterRule.Normalize(new AdminRecordFilter { Halaman = int.MaxValue, UkuranHalaman = 100 });
        Assert.Equal((int.MaxValue - 1L) * 100, huge.Offset);
    }

    [Fact]
    public void NullFilter_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AdminFilterRule.Normalize(null!));
    }
}
