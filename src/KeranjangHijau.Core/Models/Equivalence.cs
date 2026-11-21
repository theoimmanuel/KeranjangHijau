namespace KeranjangHijau.Core.Models;

/// <summary>
/// Satu aktivitas pembanding untuk konversi setara, mis. "Naik motor" per km (tabel <c>equivalences</c>).
/// Angkanya diisi tim dari jurnal (CLAUDE.md 7.5), tidak pernah ditulis di kode.
/// </summary>
public sealed class Equivalence
{
    /// <summary>Awalan <see cref="Sumber"/> untuk angka sementara yang belum diganti dari jurnal.</summary>
    public const string PlaceholderPrefix = "PLACEHOLDER";

    public int EquivalenceId { get; set; }

    /// <summary>Nama aktivitas, mis. "Naik motor".</summary>
    public string Aktivitas { get; set; } = string.Empty;

    /// <summary>Satuan aktivitas, mis. "km".</summary>
    public string Satuan { get; set; } = string.Empty;

    /// <summary>Emisi satu satuan aktivitas (kg CO2e). Di database selalu &gt; 0 (<c>ck_equivalences_nilai</c>).</summary>
    public decimal KgCo2ePerSatuan { get; set; }

    /// <summary>Nama ikon untuk UI, mis. "motor"; <c>null</c> jika tidak ada.</summary>
    public string? Ikon { get; set; }

    /// <summary>Rujukan angka (jurnal). Diawali "PLACEHOLDER" jika masih angka sementara.</summary>
    public string Sumber { get; set; } = string.Empty;

    /// <summary>
    /// <c>true</c> jika <see cref="Sumber"/> diawali <see cref="PlaceholderPrefix"/> (spasi di depan diabaikan,
    /// tidak peka huruf besar/kecil), supaya UI bisa menandai "angka sementara".
    /// </summary>
    public bool IsPlaceholder =>
        Sumber.TrimStart().StartsWith(PlaceholderPrefix, StringComparison.OrdinalIgnoreCase);
}
