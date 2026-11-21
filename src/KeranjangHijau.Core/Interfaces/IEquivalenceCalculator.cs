using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Menghitung konversi setara dari total emisi (CLAUDE.md 7.5).</summary>
public interface IEquivalenceCalculator
{
    /// <summary>
    /// <c>nilai_setara = total_co2e_kg / kg_co2e_per_satuan</c> untuk setiap aktivitas, dibulatkan 2 desimal.
    /// Aktivitas dengan <c>kg_co2e_per_satuan ≤ 0</c> diabaikan. Urutan hasil mengikuti urutan masukan.
    /// </summary>
    /// <param name="totalCo2eKg">Total emisi satu paket (kg CO2e), ≥ 0.</param>
    /// <param name="equivalences">Aktivitas pembanding dari tabel <c>equivalences</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="totalCo2eKg"/> negatif.</exception>
    IReadOnlyList<EquivalenceValue> Calculate(decimal totalCo2eKg, IEnumerable<Equivalence> equivalences);
}
