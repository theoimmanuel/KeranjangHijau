using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Options;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Implementasi <see cref="IDistanceCalculator"/> (CLAUDE.md 7.2):
/// <c>jarak_km = haversine(asal, tujuan) × RouteCorrectionFactor</c>.
/// Kota sama → <c>MinimumDistanceKm</c>. Hasil juga tidak pernah di bawah <c>MinimumDistanceKm</c>.
/// </summary>
/// <remarks>Aturan murni tanpa I/O; opsi dibaca setiap kali menghitung.</remarks>
public sealed class HaversineDistanceCalculator : IDistanceCalculator
{
    /// <summary>Radius rata-rata bumi (km).</summary>
    public const double EarthRadiusKm = 6371.0;

    /// <summary>Jumlah desimal hasil, sama dengan <c>NUMERIC(10,2)</c> di kolom <c>jarak_km</c>.</summary>
    public const int Decimals = 2;

    private readonly IOptions<EmissionOptions> _options;

    public HaversineDistanceCalculator(IOptions<EmissionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public decimal CalculateDistanceKm(City origin, City destination)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);

        var options = _options.Value;
        var minimum = Math.Round(options.MinimumDistanceKm, Decimals, MidpointRounding.AwayFromZero);

        if (IsSameCity(origin, destination))
        {
            return minimum;
        }

        var straightKm = GreatCircleKm(
            (double)origin.Latitude, (double)origin.Longitude,
            (double)destination.Latitude, (double)destination.Longitude);

        var routeKm = Math.Round((decimal)straightKm * options.RouteCorrectionFactor, Decimals, MidpointRounding.AwayFromZero);

        return Math.Max(routeKm, minimum);
    }

    /// <summary>Jarak garis lurus di permukaan bumi (km) dengan rumus haversine, tanpa faktor koreksi.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Latitude di luar ±90 atau longitude di luar ±180.</exception>
    public static double GreatCircleKm(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        EnsureCoordinate(latitude1, longitude1);
        EnsureCoordinate(latitude2, longitude2);

        var dLat = ToRadians(latitude2 - latitude1);
        var dLon = ToRadians(longitude2 - longitude1);
        var lat1 = ToRadians(latitude1);
        var lat2 = ToRadians(latitude2);

        var a = Math.Pow(Math.Sin(dLat / 2), 2)
              + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        // Min(1, ...) mencegah NaN dari Asin akibat galat pembulatan pada titik yang berseberangan.
        var c = 2 * Math.Asin(Math.Sqrt(Math.Min(1.0, a)));

        return EarthRadiusKm * c;
    }

    // Kota sama: ID sama (jika keduanya sudah tersimpan di DB) atau koordinat persis sama.
    private static bool IsSameCity(City origin, City destination) =>
        (origin.CityId > 0 && origin.CityId == destination.CityId)
        || (origin.Latitude == destination.Latitude && origin.Longitude == destination.Longitude);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static void EnsureCoordinate(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude harus di antara -90 dan 90.");
        }

        if (double.IsNaN(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude harus di antara -180 dan 180.");
        }
    }
}
