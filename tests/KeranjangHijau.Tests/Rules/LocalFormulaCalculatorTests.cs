using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class LocalFormulaCalculatorTests
{
    // Faktor sama dengan nilai sementara di database/seed.sql.
    private static readonly EmissionFactor Truk = Factor(TransportMode.Truk, 0.1m);
    private static readonly EmissionFactor Kapal = Factor(TransportMode.Kapal, 0.02m);
    private static readonly EmissionFactor Pesawat = Factor(TransportMode.Pesawat, 0.8m);

    private readonly LocalFormulaCalculator _calculator = new();

    private static EmissionFactor Factor(TransportMode mode, decimal value) =>
        new() { FactorId = (int)mode + 1, ModaTransport = mode, KgCo2ePerTonKm = value, Sumber = "test" };

    // ---------------------------------------------------------------- Angka contoh (dihitung manual)

    [Fact]
    public void Truk_2Kg_500Km()
    {
        // (2 / 1000) × 500 × 0,1 = 0,002 × 500 × 0,1 = 0,1 kg
        Assert.Equal(0.1m, _calculator.Calculate(2m, 500m, Truk).TotalCo2eKg);
    }

    [Fact]
    public void Kapal_075Kg_2000Km()
    {
        // (0,75 / 1000) × 2000 × 0,02 = 0,00075 × 2000 × 0,02 = 1,5 × 0,02 = 0,03 kg
        Assert.Equal(0.03m, _calculator.Calculate(0.75m, 2000m, Kapal).TotalCo2eKg);
    }

    [Fact]
    public void Pesawat_15Kg_123456Km_IsRoundedToFourDecimals()
    {
        // (1,5 / 1000) × 1234,56 × 0,8 = 0,0015 × 1234,56 × 0,8 = 1,85184 × 0,8 = 1,481472 → 1,4815
        Assert.Equal(1.4815m, _calculator.Calculate(1.5m, 1234.56m, Pesawat).TotalCo2eKg);
    }

    [Fact]
    public void MaxWeight_LongFlight()
    {
        // (100 / 1000) × 3500 × 0,8 = 0,1 × 3500 × 0,8 = 280 kg
        Assert.Equal(280m, _calculator.Calculate(100m, 3500m, Pesawat).TotalCo2eKg);
    }

    [Fact]
    public void SameCityMinimumDistance_1Kg()
    {
        // (1 / 1000) × 10 × 0,1 = 0,001 kg
        Assert.Equal(0.001m, _calculator.Calculate(1m, 10m, Truk).TotalCo2eKg);
    }

    // ---------------------------------------------------------------- Pembulatan 4 desimal

    [Fact]
    public void Midpoint_RoundsAwayFromZero()
    {
        // (0,5 / 1000) × 1 × 0,1 = 0,00005 → 0,0001 (bukan 0,0000 seperti banker's rounding)
        Assert.Equal(0.0001m, _calculator.Calculate(0.5m, 1m, Truk).TotalCo2eKg);
    }

    [Fact]
    public void BelowHalfOfLastDigit_RoundsDown()
    {
        // (0,123 / 1000) × 10 × 0,02 = 0,0000246 → 0,0000
        Assert.Equal(0m, _calculator.Calculate(0.123m, 10m, Kapal).TotalCo2eKg);
    }

    [Theory]
    [InlineData(0.123, 987.65, 0.1)]
    [InlineData(37.5, 2412.33, 0.8)]
    [InlineData(3.333, 33.33, 0.02)]
    public void Result_HasAtMostFourDecimals(double berat, double jarak, double faktor)
    {
        var result = _calculator.Calculate((decimal)berat, (decimal)jarak, Factor(TransportMode.Truk, (decimal)faktor));

        Assert.Equal(Math.Round(result.TotalCo2eKg, 4), result.TotalCo2eKg);
    }

    // ---------------------------------------------------------------- Sifat rumus

    [Fact]
    public void Result_IsProportionalToWeightAndDistance()
    {
        var baseResult = _calculator.Calculate(2m, 400m, Truk).TotalCo2eKg;

        Assert.Equal(baseResult * 2, _calculator.Calculate(4m, 400m, Truk).TotalCo2eKg);
        Assert.Equal(baseResult * 2, _calculator.Calculate(2m, 800m, Truk).TotalCo2eKg);
    }

    [Fact]
    public void Pesawat_EmitsMoreThanTruk_EmitsMoreThanKapal()
    {
        var pesawat = _calculator.Calculate(1m, 1000m, Pesawat).TotalCo2eKg;
        var truk = _calculator.Calculate(1m, 1000m, Truk).TotalCo2eKg;
        var kapal = _calculator.Calculate(1m, 1000m, Kapal).TotalCo2eKg;

        Assert.True(pesawat > truk && truk > kapal);
    }

    [Fact]
    public void Metode_IsRumusLokal()
    {
        var result = _calculator.Calculate(1m, 100m, Truk);

        Assert.Equal(EmissionMethod.RumusLokal, result.Metode);
        Assert.Equal("rumus_lokal", result.Metode.ToDbValue());
    }

    // ---------------------------------------------------------------- Argumen tidak valid

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveWeight_Throws(double berat)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate((decimal)berat, 100m, Truk));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NonPositiveDistance_Throws(double jarak)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(1m, (decimal)jarak, Truk));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    public void NonPositiveFactor_Throws(double faktor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _calculator.Calculate(1m, 100m, Factor(TransportMode.Truk, (decimal)faktor)));
    }

    [Fact]
    public void NullFactor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _calculator.Calculate(1m, 100m, null!));
    }
}
