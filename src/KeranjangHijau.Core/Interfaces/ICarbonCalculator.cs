using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Menghitung emisi CO2e satu paket dari berat, jarak, dan faktor moda (CLAUDE.md 7.1).</summary>
public interface ICarbonCalculator
{
    /// <param name="beratKg">Berat paket (kg), sudah divalidasi <see cref="IWeightResolver"/>.</param>
    /// <param name="jarakKm">Jarak tempuh (km) dari <see cref="IDistanceCalculator"/>.</param>
    /// <param name="factor">Faktor emisi moda transport.</param>
    /// <returns>Total emisi (dibulatkan 4 desimal) dan metode perhitungannya.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Berat, jarak, atau nilai faktor ≤ 0.</exception>
    CarbonResult Calculate(decimal beratKg, decimal jarakKm, EmissionFactor factor);
}
