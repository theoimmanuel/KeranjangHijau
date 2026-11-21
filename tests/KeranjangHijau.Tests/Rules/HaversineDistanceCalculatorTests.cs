using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Rules;

public class HaversineDistanceCalculatorTests
{
    // Koordinat sama dengan database/seed.sql.
    private static readonly City Jakarta = new() { CityId = 1, NamaKota = "Jakarta", Pulau = "Jawa", Latitude = -6.1754m, Longitude = 106.8272m };
    private static readonly City Bandung = new() { CityId = 4, NamaKota = "Bandung", Pulau = "Jawa", Latitude = -6.9175m, Longitude = 107.6191m };
    private static readonly City Surabaya = new() { CityId = 9, NamaKota = "Surabaya", Pulau = "Jawa", Latitude = -7.2575m, Longitude = 112.7521m };

    private const double Tolerance = 0.01; // 1%

    private static HaversineDistanceCalculator CreateCalculator(decimal routeFactor = 1.3m, decimal minimumKm = 10m) =>
        new(Options.Create(new EmissionOptions { RouteCorrectionFactor = routeFactor, MinimumDistanceKm = minimumKm }));

    /// <summary>
    /// Haversine yang ditulis ulang langsung dari rumus, terpisah dari kode produksi:
    /// a = sin²(Δφ/2) + cos φ1 · cos φ2 · sin²(Δλ/2); d = 2R · atan2(√a, √(1−a)).
    /// </summary>
    private static double ManualHaversineKm(City a, City b)
    {
        const double r = 6371.0;
        double Rad(decimal deg) => (double)deg * Math.PI / 180.0;

        var phi1 = Rad(a.Latitude);
        var phi2 = Rad(b.Latitude);
        var dPhi = Rad(b.Latitude - a.Latitude);
        var dLambda = Rad(b.Longitude - a.Longitude);

        var h = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2)
              + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);

        return 2 * r * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    private static void AssertWithinTolerance(double expected, double actual)
    {
        Assert.InRange(actual, expected * (1 - Tolerance), expected * (1 + Tolerance));
    }

    // ---------------------------------------------------------------- Jarak antar kota

    [Fact]
    public void JakartaBandung_MatchesManualHaversine_WithoutCorrection()
    {
        var actual = CreateCalculator(routeFactor: 1m).CalculateDistanceKm(Jakarta, Bandung);

        AssertWithinTolerance(ManualHaversineKm(Jakarta, Bandung), (double)actual);
        Assert.InRange(actual, 110m, 130m); // garis lurus Jakarta–Bandung ± 120 km
    }

    [Fact]
    public void JakartaSurabaya_MatchesManualHaversine_WithoutCorrection()
    {
        var actual = CreateCalculator(routeFactor: 1m).CalculateDistanceKm(Jakarta, Surabaya);

        AssertWithinTolerance(ManualHaversineKm(Jakarta, Surabaya), (double)actual);
        Assert.InRange(actual, 640m, 690m); // garis lurus Jakarta–Surabaya ± 665 km
    }

    [Fact]
    public void Distance_IsSymmetric()
    {
        var calculator = CreateCalculator();

        Assert.Equal(calculator.CalculateDistanceKm(Jakarta, Surabaya), calculator.CalculateDistanceKm(Surabaya, Jakarta));
    }

    // ---------------------------------------------------------------- Faktor koreksi rute

    [Fact]
    public void DefaultCorrectionFactor_IsApplied()
    {
        var actual = CreateCalculator(routeFactor: 1.3m).CalculateDistanceKm(Jakarta, Bandung);

        AssertWithinTolerance(ManualHaversineKm(Jakarta, Bandung) * 1.3, (double)actual);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.3)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void CorrectionFactor_ScalesDistance(double factor)
    {
        var actual = CreateCalculator(routeFactor: (decimal)factor).CalculateDistanceKm(Jakarta, Surabaya);

        AssertWithinTolerance(ManualHaversineKm(Jakarta, Surabaya) * factor, (double)actual);
    }

    [Fact]
    public void CorrectionFactor_ChangesResultRelativeToStraightLine()
    {
        var straight = CreateCalculator(routeFactor: 1m).CalculateDistanceKm(Jakarta, Bandung);
        var corrected = CreateCalculator(routeFactor: 1.3m).CalculateDistanceKm(Jakarta, Bandung);

        Assert.True(corrected > straight);
        Assert.Equal(Math.Round(straight * 1.3m, 0), Math.Round(corrected, 0));
    }

    [Fact]
    public void Result_IsRoundedToTwoDecimals()
    {
        var actual = CreateCalculator().CalculateDistanceKm(Jakarta, Surabaya);

        Assert.Equal(Math.Round(actual, 2), actual);
    }

    // ---------------------------------------------------------------- Kota sama → jarak minimum

    [Fact]
    public void SameCity_ReturnsMinimumDistance()
    {
        Assert.Equal(10m, CreateCalculator().CalculateDistanceKm(Jakarta, Jakarta));
    }

    [Fact]
    public void SameCity_UsesConfiguredMinimum_AndIgnoresCorrectionFactor()
    {
        Assert.Equal(25.5m, CreateCalculator(routeFactor: 2m, minimumKm: 25.5m).CalculateDistanceKm(Bandung, Bandung));
    }

    [Fact]
    public void SameCityId_DifferentInstances_ReturnsMinimumDistance()
    {
        var copy = new City { CityId = Jakarta.CityId, NamaKota = "Jakarta", Pulau = "Jawa", Latitude = Jakarta.Latitude, Longitude = Jakarta.Longitude };

        Assert.Equal(10m, CreateCalculator().CalculateDistanceKm(Jakarta, copy));
    }

    [Fact]
    public void SameCoordinates_WithoutId_ReturnsMinimumDistance()
    {
        var a = new City { Latitude = -6.2m, Longitude = 106.8m };
        var b = new City { Latitude = -6.2m, Longitude = 106.8m };

        Assert.Equal(10m, CreateCalculator().CalculateDistanceKm(a, b));
    }

    [Fact]
    public void VeryCloseCities_NeverBelowMinimum()
    {
        var a = new City { CityId = 100, Latitude = -6.2000m, Longitude = 106.8000m };
        var b = new City { CityId = 101, Latitude = -6.2001m, Longitude = 106.8001m };

        Assert.Equal(10m, CreateCalculator().CalculateDistanceKm(a, b));
    }

    // ---------------------------------------------------------------- Opsi dibaca per panggilan

    [Fact]
    public void Options_AreReadOnEveryCall()
    {
        var options = new EmissionOptions { RouteCorrectionFactor = 1m, MinimumDistanceKm = 10m };
        var calculator = new HaversineDistanceCalculator(Options.Create(options));

        var before = calculator.CalculateDistanceKm(Jakarta, Bandung);
        options.RouteCorrectionFactor = 2m;
        var after = calculator.CalculateDistanceKm(Jakarta, Bandung);

        Assert.True(after > before * 1.9m);
    }

    // ---------------------------------------------------------------- GreatCircleKm

    [Fact]
    public void GreatCircleKm_SamePoint_IsZero()
    {
        Assert.Equal(0, HaversineDistanceCalculator.GreatCircleKm(-6.1754, 106.8272, -6.1754, 106.8272));
    }

    [Fact]
    public void GreatCircleKm_OneDegreeAlongEquator_IsAbout111Km()
    {
        var expected = 2 * Math.PI * HaversineDistanceCalculator.EarthRadiusKm / 360; // ± 111,19 km

        Assert.Equal(expected, HaversineDistanceCalculator.GreatCircleKm(0, 0, 0, 1), 6);
    }

    [Fact]
    public void GreatCircleKm_AntipodalPoints_IsHalfCircumference()
    {
        var expected = Math.PI * HaversineDistanceCalculator.EarthRadiusKm;

        Assert.Equal(expected, HaversineDistanceCalculator.GreatCircleKm(0, 0, 0, 180), 6);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    public void GreatCircleKm_InvalidCoordinate_Throws(double latitude, double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HaversineDistanceCalculator.GreatCircleKm(latitude, longitude, 0, 0));
    }

    [Fact]
    public void NullCity_Throws()
    {
        var calculator = CreateCalculator();

        Assert.Throws<ArgumentNullException>(() => calculator.CalculateDistanceKm(null!, Jakarta));
        Assert.Throws<ArgumentNullException>(() => calculator.CalculateDistanceKm(Jakarta, null!));
    }
}
