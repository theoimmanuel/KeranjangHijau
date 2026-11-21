namespace KeranjangHijau.Core.Models;

/// <summary>Satu perubahan kesehatan hutan (tabel <c>forest_logs</c>, CLAUDE.md 7.7).</summary>
public sealed class ForestLog
{
    public int LogId { get; set; }

    public int ForestId { get; set; }

    /// <summary>Record emisi penyebab penurunan; <c>null</c> = log pemulihan alami.</summary>
    public int? RecordId { get; set; }

    /// <summary>Perubahan kesehatan (negatif = turun, positif = pulih), 2 desimal.</summary>
    public decimal Perubahan { get; set; }

    /// <summary>Kesehatan hutan setelah perubahan (0–100).</summary>
    public decimal KesehatanSetelah { get; set; }

    public string Keterangan { get; set; } = string.Empty;

    /// <summary>Waktu perubahan (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
