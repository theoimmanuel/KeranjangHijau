namespace KeranjangHijau.Core.Models;

/// <summary>Satu kali laporan dibagikan sebagai kartu Instagram Story (tabel <c>shared_reports</c>).</summary>
public sealed class SharedReport
{
    /// <summary>Panjang maksimum <see cref="FileGambar"/> (<c>VARCHAR(260)</c>, sama dengan MAX_PATH Windows).</summary>
    public const int MaxFileGambarLength = 260;

    public int ShareId { get; set; }

    public int UserId { get; set; }

    /// <summary>Awal periode laporan (UTC, inklusif) = 00.00 WIB hari pertama.</summary>
    public DateTimeOffset PeriodeMulai { get; set; }

    /// <summary>Akhir periode laporan (UTC, eksklusif) = 00.00 WIB sehari setelah hari terakhir.</summary>
    public DateTimeOffset PeriodeSelesai { get; set; }

    public int JumlahPaket { get; set; }

    public decimal TotalCo2eKg { get; set; }

    /// <summary>Path file PNG kartu; <c>null</c> jika tidak ada.</summary>
    public string? FileGambar { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
