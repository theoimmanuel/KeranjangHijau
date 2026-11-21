using System.Globalization;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class EquivalenceCalculatorTests
{
    // Angka test sengaja BUKAN angka seed: kalkulator tidak boleh bergantung pada nilai tertentu.
    private readonly EquivalenceCalculator _calculator = new();

    private static Equivalence Eq(int id, decimal factor, string sumber = "Jurnal X (2025)") => new()
    {
        EquivalenceId = id,
        Aktivitas = $"Aktivitas {id}",
        Satuan = "km",
        KgCo2ePerSatuan = factor,
        Ikon = "ikon",
        Sumber = sumber,
    };

    // ---------------------------------------------------------------- Pembagian

    [Fact]
    public void Divides_TotalByFactor()
    {
        // 1,5 / 0,25 = 6 ; 1,5 / 0,5 = 3
        var result = _calculator.Calculate(1.5m, [Eq(1, 0.25m), Eq(2, 0.5m)]);

        Assert.Equal([6m, 3m], result.Select(r => r.NilaiSetara));
    }

    [Fact]
    public void KeepsEquivalenceReference_AndInputOrder()
    {
        var a = Eq(5, 0.2m);
        var b = Eq(2, 0.4m);

        var result = _calculator.Calculate(1m, [a, b]);

        Assert.Same(a, result[0].Equivalence);
        Assert.Same(b, result[1].Equivalence);
    }

    [Fact]
    public void FactorAboveOne_GivesValueBelowTotal()
    {
        // 0,9 / 1,8 = 0,5
        Assert.Equal(0.5m, Assert.Single(_calculator.Calculate(0.9m, [Eq(1, 1.8m)])).NilaiSetara);
    }

    [Fact]
    public void ZeroTotal_GivesZero()
    {
        Assert.Equal(0m, Assert.Single(_calculator.Calculate(0m, [Eq(1, 0.3m)])).NilaiSetara);
    }

    [Fact]
    public void LargeValue_DoesNotOverflow()
    {
        // 500 / 0,000001 = 500.000.000 (masih muat di nilai_setara NUMERIC(14,4)).
        Assert.Equal(500_000_000m, Assert.Single(_calculator.Calculate(500m, [Eq(1, 0.000001m)])).NilaiSetara);
    }

    // ---------------------------------------------------------------- Pembulatan 2 desimal

    [Theory]
    [InlineData("1", "0.3", "3.33")]        // 3,3333…
    [InlineData("2", "0.3", "6.67")]        // 6,6666…
    [InlineData("0.125", "1", "0.13")]      // tepat di tengah → menjauhi nol (bukan 0,12)
    [InlineData("0.0049", "1", "0")]        // terlalu kecil → 0,00
    [InlineData("0.0001", "0.03", "0")]     // 0,00333… → 0,00
    [InlineData("1.4815", "0.09", "16.46")] // 16,4611…
    public void RoundsToTwoDecimals_AwayFromZero(string total, string factor, string expected)
    {
        var result = _calculator.Calculate(decimal.Parse(total, CultureInfo.InvariantCulture), [Eq(1, decimal.Parse(factor, CultureInfo.InvariantCulture))]);

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), Assert.Single(result).NilaiSetara);
    }

    [Fact]
    public void Result_HasAtMostTwoDecimals()
    {
        var nilai = Assert.Single(_calculator.Calculate(1m, [Eq(1, 0.7m)])).NilaiSetara;

        Assert.Equal(1.43m, nilai);
        Assert.Equal(nilai, Math.Round(nilai, EquivalenceCalculator.Decimals));
    }

    // ---------------------------------------------------------------- Faktor ≤ 0 diabaikan

    [Fact]
    public void ZeroFactor_IsIgnored()
    {
        var result = _calculator.Calculate(1m, [Eq(1, 0m), Eq(2, 0.5m)]);

        var only = Assert.Single(result);
        Assert.Equal(2, only.Equivalence.EquivalenceId);
        Assert.Equal(2m, only.NilaiSetara);
    }

    [Fact]
    public void NegativeFactor_IsIgnored()
    {
        Assert.Empty(_calculator.Calculate(1m, [Eq(1, -0.1m)]));
    }

    [Fact]
    public void AllFactorsInvalid_OrEmptyInput_GivesEmptyList()
    {
        Assert.Empty(_calculator.Calculate(1m, [Eq(1, 0m), Eq(2, -1m)]));
        Assert.Empty(_calculator.Calculate(1m, []));
    }

    // ---------------------------------------------------------------- Flag placeholder

    [Fact]
    public void ToDto_CarriesPlaceholderFlag_AndDisplayFields()
    {
        var placeholder = Eq(1, 0.5m, "PLACEHOLDER - ganti dari jurnal");
        var real = Eq(2, 0.5m, "Jurnal X (2025)");

        var dtos = _calculator.Calculate(1m, [placeholder, real]).Select(v => v.ToDto()).ToList();

        Assert.True(dtos[0].IsPlaceholder);
        Assert.False(dtos[1].IsPlaceholder);
        Assert.Equal("Aktivitas 1", dtos[0].Aktivitas);
        Assert.Equal("km", dtos[0].Satuan);
        Assert.Equal("ikon", dtos[0].Ikon);
        Assert.Equal(2m, dtos[0].NilaiSetara);
    }

    // ---------------------------------------------------------------- Argumen tidak valid

    [Fact]
    public void NegativeTotal_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(-0.01m, [Eq(1, 0.5m)]));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => _calculator.Calculate(1m, null!));
        Assert.Throws<ArgumentNullException>(() => _calculator.Calculate(1m, [null!]));
    }
}
