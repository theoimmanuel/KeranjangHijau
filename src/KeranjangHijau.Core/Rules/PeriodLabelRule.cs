using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Label periode berbahasa Indonesia untuk kartu bagikan, mis. "November 2026", "17–23 November 2026",
/// atau "Sejak 10 Maret 2026". Nama bulan ditulis sendiri (bukan <c>CultureInfo("id-ID")</c>) agar aman
/// di mode invariant globalization.
/// </summary>
public static class PeriodLabelRule
{
    private static readonly string[] MonthNames =
    [
        "Januari", "Februari", "Maret", "April", "Mei", "Juni",
        "Juli", "Agustus", "September", "Oktober", "November", "Desember",
    ];

    /// <summary>Nama bulan Indonesia (1 = Januari).</summary>
    public static string MonthName(int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        return MonthNames[month - 1];
    }

    /// <summary>Tanggal lengkap, mis. "3 Maret 2025".</summary>
    public static string FormatDate(DateOnly date) => $"{date.Day} {MonthName(date.Month)} {date.Year}";

    /// <summary>Label periode dari hari pertama dan hari terakhir (keduanya inklusif, WIB).</summary>
    /// <exception cref="ArgumentException"><paramref name="tanggalSelesai"/> sebelum <paramref name="tanggalMulai"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> tidak dikenal.</exception>
    public static string Create(ReportPeriod period, DateOnly tanggalMulai, DateOnly tanggalSelesai)
    {
        if (tanggalSelesai < tanggalMulai)
        {
            throw new ArgumentException("Tanggal selesai tidak boleh sebelum tanggal mulai.", nameof(tanggalSelesai));
        }

        return period switch
        {
            ReportPeriod.Bulanan => $"{MonthName(tanggalMulai.Month)} {tanggalMulai.Year}",
            ReportPeriod.Mingguan => FormatRange(tanggalMulai, tanggalSelesai),
            ReportPeriod.Semua => $"Sejak {FormatDate(tanggalMulai)}",
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Periode laporan tidak dikenal."),
        };
    }

    /// <summary>"3–9 Maret 2025", "24 Februari – 2 Maret 2025", atau "29 Desember 2025 – 4 Januari 2026".</summary>
    public static string FormatRange(DateOnly first, DateOnly last)
    {
        if (first == last)
        {
            return FormatDate(first);
        }

        if (first.Year != last.Year)
        {
            return $"{FormatDate(first)} – {FormatDate(last)}";
        }

        return first.Month == last.Month
            ? $"{first.Day}–{FormatDate(last)}"
            : $"{first.Day} {MonthName(first.Month)} – {FormatDate(last)}";
    }
}
