using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Implementasi <see cref="ICarbonCalculator"/> dengan rumus lokal (CLAUDE.md 7.1):
/// <c>CO2e (kg) = (berat_kg / 1000) × jarak_km × kg_co2e_per_ton_km</c>.
/// </summary>
/// <remarks>Aturan murni tanpa I/O. Seluruh perhitungan memakai <c>decimal</c> agar tidak ada galat biner.</remarks>
public sealed class LocalFormulaCalculator : ICarbonCalculator
{
    /// <summary>Jumlah desimal <see cref="CarbonResult.TotalCo2eKg"/>.</summary>
    public const int Decimals = 4;

    private const decimal KgPerTon = 1000m;

    public CarbonResult Calculate(decimal beratKg, decimal jarakKm, EmissionFactor factor)
    {
        ArgumentNullException.ThrowIfNull(factor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(beratKg);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(jarakKm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor.KgCo2ePerTonKm, nameof(factor));

        var totalKg = beratKg / KgPerTon * jarakKm * factor.KgCo2ePerTonKm;

        return new CarbonResult(
            Math.Round(totalKg, Decimals, MidpointRounding.AwayFromZero),
            EmissionMethod.RumusLokal);
    }
}
