using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Menghitung jarak tempuh paket antara kota asal dan kota tujuan (CLAUDE.md 7.2).</summary>
public interface IDistanceCalculator
{
    /// <summary>
    /// Jarak tempuh (km), dibulatkan 2 desimal sesuai kolom <c>emission_records.jarak_km</c>.
    /// Selalu ≥ <c>Emission:MinimumDistanceKm</c>.
    /// </summary>
    decimal CalculateDistanceKm(City origin, City destination);
}
