namespace KeranjangHijau.Core.Options;

/// <summary>
/// Angka-angka perhitungan emisi (bagian <c>Emission</c> di konfigurasi, CLAUDE.md 7.2–7.4).
/// </summary>
public sealed class EmissionOptions
{
    public const string SectionName = "Emission";

    /// <summary>Kata kunci default layanan ekspres (CLAUDE.md 7.3).</summary>
    public static IReadOnlyList<string> DefaultExpressKeywords { get; } =
        ["YES", "ONS", "SDS", "BEST", "EXPRESS", "KILAT", "NEXT DAY", "SAME DAY"];

    /// <summary>Pengali jarak garis lurus (haversine) agar mendekati jarak rute nyata.</summary>
    public decimal RouteCorrectionFactor { get; set; } = 1.3m;

    /// <summary>Jarak minimum (km) yang dipakai jika kota asal = kota tujuan.</summary>
    public decimal MinimumDistanceKm { get; set; } = 10m;

    /// <summary>Berat (kg) jika data resi dan input pengguna sama-sama kosong.</summary>
    public decimal DefaultWeightKg { get; set; } = 1m;

    /// <summary>Batas atas berat paket (kg).</summary>
    public decimal MaxWeightKg { get; set; } = 100m;

    /// <summary>
    /// Kata kunci (tidak peka huruf besar/kecil) yang menandakan layanan kilat/ekspres.
    /// Jika konfigurasi tidak mengisi daftar ini, dipakai <see cref="DefaultExpressKeywords"/>.
    /// </summary>
    /// <remarks>
    /// Sengaja kosong secara default: binder konfigurasi .NET <em>menambahkan</em> elemen ke
    /// koleksi yang sudah terisi, sehingga nilai default diisi lewat post-configure.
    /// </remarks>
    public List<string> ExpressKeywords { get; set; } = [];
}
