using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Hasil perhitungan emisi satu paket.</summary>
/// <param name="TotalCo2eKg">Total emisi (kg CO2e), dibulatkan 4 desimal.</param>
/// <param name="Metode">Cara emisi dihitung; disimpan ke <c>emission_records.metode</c>.</param>
public sealed record CarbonResult(decimal TotalCo2eKg, EmissionMethod Metode);
