using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Rules;

public class WeightResolverTests
{
    private readonly EmissionOptions _options = new() { DefaultWeightKg = 1m, MaxWeightKg = 100m };

    private WeightResolver CreateResolver() => new(Options.Create(_options));

    // ---------------------------------------------------------------- Urutan pemilihan berat

    [Fact]
    public void TrackerWeight_IsPreferredOverManual()
    {
        var result = CreateResolver().Resolve(trackerBeratKg: 2.5m, manualBeratKg: 7m);

        Assert.Equal(2.5m, result.BeratKg);
        Assert.Equal(WeightSource.Api, result.Sumber);
    }

    [Fact]
    public void TrackerWeight_UsedWithoutManual()
    {
        var result = CreateResolver().Resolve(3m, null);

        Assert.Equal(3m, result.BeratKg);
        Assert.Equal(WeightSource.Api, result.Sumber);
    }

    [Fact]
    public void NoTrackerWeight_UsesManual()
    {
        var result = CreateResolver().Resolve(null, 7m);

        Assert.Equal(7m, result.BeratKg);
        Assert.Equal(WeightSource.Manual, result.Sumber);
    }

    [Fact]
    public void NoTrackerAndNoManual_UsesDefault()
    {
        var result = CreateResolver().Resolve(null, null);

        Assert.Equal(1m, result.BeratKg);
        Assert.Equal(WeightSource.Default, result.Sumber);
    }

    [Fact]
    public void Default_ComesFromOptions()
    {
        _options.DefaultWeightKg = 0.5m;

        var result = CreateResolver().Resolve(null, null);

        Assert.Equal(0.5m, result.BeratKg);
        Assert.Equal(WeightSource.Default, result.Sumber);
    }

    [Fact]
    public void InvalidManual_IsIgnored_WhenTrackerWeightExists()
    {
        // Hanya berat terpilih yang divalidasi; berat manual tidak dipakai jika data resi punya berat.
        var result = CreateResolver().Resolve(4m, -1m);

        Assert.Equal(4m, result.BeratKg);
        Assert.Equal(WeightSource.Api, result.Sumber);
    }

    // ---------------------------------------------------------------- Batas bawah: harus > 0

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void ZeroOrNegativeManual_Throws(double berat)
    {
        var ex = Assert.Throws<ValidationException>(() => CreateResolver().Resolve(null, (decimal)berat));

        Assert.Equal(WeightResolver.MustBePositiveMessage, ex.Message);
        Assert.Equal("BeratKg", ex.Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void ZeroOrNegativeTracker_Throws(double berat)
    {
        var ex = Assert.Throws<ValidationException>(() => CreateResolver().Resolve((decimal)berat, 5m));

        Assert.Equal(WeightResolver.MustBePositiveMessage, ex.Message);
    }

    [Fact]
    public void WeightRoundingToZero_Throws()
    {
        // 0,0004 kg dibulatkan ke 3 desimal = 0,000 → tidak lolos CHECK berat_kg > 0.
        Assert.Throws<ValidationException>(() => CreateResolver().Resolve(null, 0.0004m));
    }

    [Fact]
    public void SmallestStorableWeight_IsAccepted()
    {
        Assert.Equal(0.001m, CreateResolver().Resolve(null, 0.001m).BeratKg);
    }

    // ---------------------------------------------------------------- Batas atas: ≤ MaxWeightKg

    [Fact]
    public void ExactlyMaxWeight_IsAccepted()
    {
        Assert.Equal(100m, CreateResolver().Resolve(null, 100m).BeratKg);
    }

    [Theory]
    [InlineData(100.001)]
    [InlineData(101)]
    [InlineData(1000)]
    public void ManualAboveMax_Throws(double berat)
    {
        var ex = Assert.Throws<ValidationException>(() => CreateResolver().Resolve(null, (decimal)berat));

        Assert.Equal("Berat paket maksimal 100 kg.", ex.Message);
        Assert.Equal("BeratKg", ex.Field);
    }

    [Fact]
    public void TrackerAboveMax_Throws()
    {
        Assert.Throws<ValidationException>(() => CreateResolver().Resolve(150m, 5m));
    }

    [Fact]
    public void MaxWeight_ComesFromOptions()
    {
        _options.MaxWeightKg = 30.5m;

        Assert.Equal(30.5m, CreateResolver().Resolve(null, 30.5m).BeratKg);
        var ex = Assert.Throws<ValidationException>(() => CreateResolver().Resolve(null, 31m));
        Assert.Equal("Berat paket maksimal 30,5 kg.", ex.Message);
    }

    // ---------------------------------------------------------------- Pembulatan 3 desimal

    [Theory]
    [InlineData(1.2344, 1.234)]
    [InlineData(1.2345, 1.235)]   // midpoint dibulatkan menjauhi nol
    [InlineData(2.5, 2.5)]
    public void Weight_IsRoundedToThreeDecimals(double input, double expected)
    {
        Assert.Equal((decimal)expected, CreateResolver().Resolve(null, (decimal)input).BeratKg);
    }

    [Fact]
    public void Rounding_HappensBeforeMaxCheck()
    {
        // 100,0004 → 100,000 sehingga masih sesuai CHECK berat_kg <= 100.
        Assert.Equal(100m, CreateResolver().Resolve(null, 100.0004m).BeratKg);
    }
}
