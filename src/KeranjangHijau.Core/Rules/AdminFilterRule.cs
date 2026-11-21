using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Exceptions;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Normalisasi filter halaman admin (M7.1): tanggal WIB → rentang UTC setengah terbuka, kode kurir
/// huruf kecil, kata kunci di-trim, serta halaman dan ukuran halaman dibatasi.
/// </summary>
/// <remarks>Kelas murni tanpa I/O.</remarks>
public static class AdminFilterRule
{
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> null.</exception>
    /// <exception cref="ValidationException">
    /// Tanggal di luar rentang yang didukung, atau tanggal mulai setelah tanggal selesai.
    /// </exception>
    public static AdminRecordQuery Normalize(AdminRecordFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        EnsureSupported(filter.TanggalMulai, nameof(AdminRecordFilter.TanggalMulai));
        EnsureSupported(filter.TanggalSelesai, nameof(AdminRecordFilter.TanggalSelesai));
        if (filter.TanggalMulai is { } mulai && filter.TanggalSelesai is { } selesai && mulai > selesai)
        {
            throw new ValidationException(
                "Tanggal mulai tidak boleh setelah tanggal selesai.", nameof(AdminRecordFilter.TanggalSelesai));
        }

        return new AdminRecordQuery(
            filter.TanggalMulai is { } from ? PeriodRangeRule.StartOfJakartaDayUtc(from) : null,
            filter.TanggalSelesai is { } to ? PeriodRangeRule.StartOfJakartaDayUtc(to.AddDays(1)) : null,
            NullIfBlank(filter.KodeKurir)?.ToLowerInvariant(),
            NullIfBlank(filter.KataKunci),
            Math.Max(1, filter.Halaman),
            NormalizePageSize(filter.UkuranHalaman));
    }

    /// <summary>≤ 0 → <see cref="AdminRecordFilter.DefaultPageSize"/>; lebih dari maksimum → <see cref="AdminRecordFilter.MaxPageSize"/>.</summary>
    public static int NormalizePageSize(int ukuranHalaman) => ukuranHalaman switch
    {
        <= 0 => AdminRecordFilter.DefaultPageSize,
        > AdminRecordFilter.MaxPageSize => AdminRecordFilter.MaxPageSize,
        _ => ukuranHalaman,
    };

    private static void EnsureSupported(DateOnly? date, string field)
    {
        if (date is { } d && (d < PeriodRangeRule.MinReferenceDate || d > PeriodRangeRule.MaxReferenceDate))
        {
            throw new ValidationException("Tanggal di luar rentang yang didukung.", field);
        }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
