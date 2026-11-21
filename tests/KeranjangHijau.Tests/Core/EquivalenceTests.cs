using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Core;

public class EquivalenceTests
{
    [Theory]
    [InlineData("PLACEHOLDER - ganti dari jurnal", true)]
    [InlineData("PLACEHOLDER", true)]
    [InlineData("  PLACEHOLDER (sementara)", true)]
    [InlineData("placeholder - huruf kecil", true)]
    [InlineData("Jurnal Transportasi Indonesia (2024)", false)]
    [InlineData("Bukan PLACEHOLDER di awal", false)]
    [InlineData("", false)]
    public void IsPlaceholder_WhenSumberStartsWithPlaceholder(string sumber, bool expected)
    {
        Assert.Equal(expected, new Equivalence { Sumber = sumber }.IsPlaceholder);
    }

    [Fact]
    public void EquivalenceValue_ToRecordEquivalence_UsesGivenRecordId()
    {
        var value = new EquivalenceValue(new Equivalence { EquivalenceId = 4, KgCo2ePerSatuan = 0.5m }, 12.34m);

        var row = value.ToRecordEquivalence(99);

        Assert.Equal(99, row.RecordId);
        Assert.Equal(4, row.EquivalenceId);
        Assert.Equal(12.34m, row.NilaiSetara);
    }

    [Fact]
    public void EquivalenceValue_ToDto_NullIkon_StaysNull()
    {
        var value = new EquivalenceValue(
            new Equivalence { Aktivitas = "Naik bus", Satuan = "km", Ikon = null, Sumber = "Jurnal" }, 1m);

        Assert.Equal(new EquivalenceDto("Naik bus", "km", 1m, null, false), value.ToDto());
    }
}
