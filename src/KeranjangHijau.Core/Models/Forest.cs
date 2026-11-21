namespace KeranjangHijau.Core.Models;

/// <summary>Hutan virtual milik satu pengguna (tabel <c>forests</c>, CLAUDE.md 7.7).</summary>
public sealed class Forest
{
    public int ForestId { get; set; }

    public int UserId { get; set; }

    public string NamaHutan { get; set; } = string.Empty;

    public int JumlahPohonAwal { get; set; }

    /// <summary>Kesehatan 0–100 (NUMERIC(5,2)).</summary>
    public decimal Kesehatan { get; set; }

    /// <summary>Salah satu dari: Lestari, Sehat, Menipis, Terancam, Kritis.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Waktu perubahan kesehatan terakhir (UTC); dasar perhitungan pemulihan.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
