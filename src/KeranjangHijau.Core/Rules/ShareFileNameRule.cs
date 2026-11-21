using System.Globalization;
using System.Text.RegularExpressions;
using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Nama file kartu bagikan: <c>keranjanghijau-&lt;periode&gt;-&lt;yyyyMMddHHmmss&gt;.png</c> (waktu WIB),
/// mis. <c>keranjanghijau-bulanan-20261120093015.png</c>. Jika nama sudah dipakai, ditambah akhiran
/// <c>-1</c>, <c>-2</c>, dst. Hanya huruf kecil ASCII, angka, dan tanda hubung, jadi aman di Windows maupun Linux.
/// </summary>
public static partial class ShareFileNameRule
{
    public const string Prefix = "keranjanghijau";
    public const string Extension = ".png";

    /// <summary>Teks periode di nama file.</summary>
    public static string ToSlug(ReportPeriod period) => period switch
    {
        ReportPeriod.Mingguan => "mingguan",
        ReportPeriod.Bulanan => "bulanan",
        ReportPeriod.Semua => "semua",
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Periode laporan tidak dikenal."),
    };

    /// <summary>Nama file untuk waktu <paramref name="now"/> (dikonversi ke WIB).</summary>
    /// <param name="duplicateIndex">0 = tanpa akhiran; n &gt; 0 = akhiran <c>-n</c> (nama sebelumnya sudah dipakai).</param>
    public static string Create(ReportPeriod period, DateTimeOffset now, int duplicateIndex = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(duplicateIndex);

        var wib = TimeZoneInfo.ConvertTime(now, PeriodRangeRule.JakartaTimeZone);
        var stamp = wib.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var suffix = duplicateIndex == 0 ? string.Empty : $"-{duplicateIndex.ToString(CultureInfo.InvariantCulture)}";
        return $"{Prefix}-{ToSlug(period)}-{stamp}{suffix}{Extension}";
    }

    /// <summary><c>true</c> jika <paramref name="fileName"/> (tanpa folder) mengikuti pola nama file kartu.</summary>
    public static bool IsValid(string? fileName) => fileName is not null && FileNamePattern().IsMatch(fileName);

    [GeneratedRegex(@"^keranjanghijau-(mingguan|bulanan|semua)-\d{14}(-[1-9]\d*)?\.png$", RegexOptions.CultureInvariant)]
    private static partial Regex FileNamePattern();
}
