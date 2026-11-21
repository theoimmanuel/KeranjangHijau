namespace KeranjangHijau.Core.Enums;

/// <summary>Asal berat paket yang dipakai menghitung emisi (CLAUDE.md 7.4).</summary>
/// <remarks>Tidak disimpan di database; hanya informasi untuk UI dan alur hitung emisi.</remarks>
public enum WeightSource
{
    /// <summary>Berat dari data resi (hasil tracker, baik API maupun mock).</summary>
    Api,

    /// <summary>Berat yang diisi pengguna secara manual.</summary>
    Manual,

    /// <summary>Berat default dari <c>Emission:DefaultWeightKg</c>.</summary>
    Default,
}
