using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Models;

/// <summary>Faktor emisi per moda transport (tabel <c>emission_factors</c>).</summary>
public sealed class EmissionFactor
{
    public int FactorId { get; set; }

    public TransportMode ModaTransport { get; set; }

    /// <summary>kg CO2e per ton-km (selalu &gt; 0, sesuai <c>ck_emission_factors_nilai</c>).</summary>
    public decimal KgCo2ePerTonKm { get; set; }

    public string Sumber { get; set; } = string.Empty;
}
