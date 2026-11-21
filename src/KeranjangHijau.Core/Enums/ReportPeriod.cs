namespace KeranjangHijau.Core.Enums;

/// <summary>Periode laporan emisi (CLAUDE.md 7.8). Batas periode dihitung di zona <c>Asia/Jakarta</c>.</summary>
public enum ReportPeriod
{
    /// <summary>Senin 00.00 sampai Minggu 23.59 WIB, minggu yang memuat tanggal acuan.</summary>
    Mingguan,

    /// <summary>Tanggal 1 sampai akhir bulan kalender (WIB) yang memuat tanggal acuan.</summary>
    Bulanan,

    /// <summary>Semua paket sampai akhir tanggal acuan (WIB).</summary>
    Semua,
}
