using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Implementasi <see cref="IEquivalenceCalculator"/> (CLAUDE.md 7.5):
/// <c>nilai_setara = total_co2e_kg / kg_co2e_per_satuan</c>.
/// </summary>
/// <remarks>
/// Aturan murni tanpa I/O. Tidak ada angka faktor di sini: semua faktor berasal dari tabel <c>equivalences</c>.
/// Faktor ≤ 0 diabaikan (bukan dilempar), supaya satu baris data yang salah tidak menggagalkan hitung emisi.
/// </remarks>
public sealed class EquivalenceCalculator : IEquivalenceCalculator
{
    /// <summary>Jumlah desimal <see cref="EquivalenceValue.NilaiSetara"/>.</summary>
    public const int Decimals = 2;

    public IReadOnlyList<EquivalenceValue> Calculate(decimal totalCo2eKg, IEnumerable<Equivalence> equivalences)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCo2eKg);
        ArgumentNullException.ThrowIfNull(equivalences);

        var results = new List<EquivalenceValue>();
        foreach (var equivalence in equivalences)
        {
            ArgumentNullException.ThrowIfNull(equivalence, nameof(equivalences));
            if (equivalence.KgCo2ePerSatuan <= 0)
            {
                continue;
            }

            var nilai = Math.Round(totalCo2eKg / equivalence.KgCo2ePerSatuan, Decimals, MidpointRounding.AwayFromZero);
            results.Add(new EquivalenceValue(equivalence, nilai));
        }

        return results;
    }
}
