using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Berat paket yang dipakai menghitung emisi beserta asalnya (CLAUDE.md 7.4).</summary>
/// <param name="BeratKg">Berat (kg), 3 desimal sesuai <c>shipments.berat_kg</c>; selalu 0 &lt; berat ≤ <c>MaxWeightKg</c>.</param>
/// <param name="Sumber">Asal berat: data resi, input manual, atau default.</param>
public sealed record ResolvedWeight(decimal BeratKg, WeightSource Sumber);
