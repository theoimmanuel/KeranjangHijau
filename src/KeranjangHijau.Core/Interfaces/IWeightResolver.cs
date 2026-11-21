using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Memilih dan memvalidasi berat paket (CLAUDE.md 7.4).</summary>
public interface IWeightResolver
{
    /// <summary>
    /// Urutan: berat dari data resi → berat manual pengguna → <c>Emission:DefaultWeightKg</c>.
    /// Hanya berat yang terpilih yang divalidasi (0 &lt; berat ≤ <c>Emission:MaxWeightKg</c>).
    /// </summary>
    /// <param name="trackerBeratKg">Berat dari tracker resi; <c>null</c> jika tidak tersedia.</param>
    /// <param name="manualBeratKg">Berat yang diisi pengguna; <c>null</c> jika kosong.</param>
    /// <exception cref="Exceptions.ValidationException">Berat terpilih ≤ 0 atau melebihi batas (<c>Field = "BeratKg"</c>).</exception>
    ResolvedWeight Resolve(decimal? trackerBeratKg, decimal? manualBeratKg);
}
