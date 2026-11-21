namespace KeranjangHijau.Core.Options;

/// <summary>
/// Angka-angka hutan virtual (bagian <c>Forest</c> di konfigurasi, CLAUDE.md 7.7).
/// </summary>
public sealed class ForestOptions
{
    public const string SectionName = "Forest";

    /// <summary>Poin kesehatan yang berkurang untuk setiap 100 gram CO2e.</summary>
    public decimal PointsPer100GramCo2e { get; set; } = 1m;

    /// <summary>Poin kesehatan yang pulih untuk setiap hari penuh tanpa paket baru.</summary>
    public decimal RecoveryPointsPerDay { get; set; } = 1m;

    /// <summary>Kesehatan hutan saat pertama dibuat (juga batas maksimum).</summary>
    public decimal InitialHealth { get; set; } = 100m;

    /// <summary>Jumlah pohon awal saat hutan dibuat.</summary>
    public int InitialTrees { get; set; } = 100;
}
