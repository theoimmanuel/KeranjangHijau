using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Batas periode laporan (CLAUDE.md 7.8): <see cref="ReportPeriod.Mingguan"/> = Senin–Minggu,
/// <see cref="ReportPeriod.Bulanan"/> = bulan kalender, <see cref="ReportPeriod.Semua"/> = tanpa batas awal.
/// Tanggal dihitung di zona <c>Asia/Jakarta</c>, lalu batasnya diubah ke UTC untuk query.
/// </summary>
/// <remarks>Kelas murni: tidak membaca jam sistem; tanggal acuan selalu dikirim pemanggil.</remarks>
public static class PeriodRangeRule
{
    /// <summary>ID zona IANA, juga dipakai PostgreSQL (<c>AT TIME ZONE</c>) untuk mengelompokkan per hari.</summary>
    public const string TimeZoneId = "Asia/Jakarta";

    /// <summary>ID zona Windows untuk WIB, dipakai jika ID IANA tidak dikenali (Windows tanpa ICU).</summary>
    public const string WindowsTimeZoneId = "SE Asia Standard Time";

    /// <summary>Selisih WIB terhadap UTC (tetap +7 jam, tanpa daylight saving).</summary>
    public static readonly TimeSpan JakartaUtcOffset = TimeSpan.FromHours(7);

    /// <summary>Tanggal acuan terkecil yang diterima.</summary>
    public static readonly DateOnly MinReferenceDate = new(1970, 1, 1);

    /// <summary>Tanggal acuan terbesar yang diterima (batas akhir bulan berikutnya masih bisa dihitung).</summary>
    public static readonly DateOnly MaxReferenceDate = new(9998, 12, 31);

    /// <summary>Zona waktu WIB. Jika tidak ada di sistem, dipakai zona buatan dengan offset tetap +07:00.</summary>
    public static TimeZoneInfo JakartaTimeZone { get; } = ResolveJakartaTimeZone();

    /// <summary>Tanggal kalender WIB dari suatu waktu (offset berapa pun).</summary>
    public static DateOnly ToJakartaDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, JakartaTimeZone).DateTime);

    /// <summary>Pukul 00.00 WIB pada <paramref name="date"/>, dinyatakan dalam UTC (offset nol).</summary>
    public static DateTimeOffset StartOfJakartaDayUtc(DateOnly date)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMidnight, JakartaTimeZone), TimeSpan.Zero);
    }

    /// <summary>Senin pada minggu yang memuat <paramref name="date"/> (minggu dimulai Senin).</summary>
    public static DateOnly StartOfWeek(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7; // Senin = 0, Minggu = 6
        return date.AddDays(-daysSinceMonday);
    }

    /// <summary>Batas periode yang memuat tanggal kalender WIB <paramref name="referenceDate"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="period"/> tidak dikenal, atau <paramref name="referenceDate"/> di luar
    /// <see cref="MinReferenceDate"/>–<see cref="MaxReferenceDate"/>.
    /// </exception>
    public static PeriodRange Calculate(ReportPeriod period, DateOnly referenceDate)
    {
        if (referenceDate < MinReferenceDate || referenceDate > MaxReferenceDate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceDate), referenceDate, "Tanggal acuan laporan di luar rentang yang didukung.");
        }

        (DateOnly? start, DateOnly endExclusive) = period switch
        {
            ReportPeriod.Mingguan => (StartOfWeek(referenceDate), StartOfWeek(referenceDate).AddDays(7)),
            ReportPeriod.Bulanan => (FirstOfMonth(referenceDate), FirstOfMonth(referenceDate).AddMonths(1)),
            ReportPeriod.Semua => ((DateOnly?)null, referenceDate.AddDays(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Periode laporan tidak dikenal."),
        };

        return new PeriodRange(
            period,
            start,
            endExclusive.AddDays(-1),
            start is { } s ? StartOfJakartaDayUtc(s) : null,
            StartOfJakartaDayUtc(endExclusive));
    }

    /// <summary>Batas periode yang memuat waktu <paramref name="referenceInstant"/> (dikonversi ke tanggal WIB).</summary>
    public static PeriodRange Calculate(ReportPeriod period, DateTimeOffset referenceInstant) =>
        Calculate(period, ToJakartaDate(referenceInstant));

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private static TimeZoneInfo ResolveJakartaTimeZone()
    {
        foreach (var id in new[] { TimeZoneId, WindowsTimeZoneId })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // WIB tidak memakai daylight saving sejak 1964, jadi offset tetap sudah benar.
        return TimeZoneInfo.CreateCustomTimeZone(TimeZoneId, JakartaUtcOffset, "WIB", "WIB");
    }
}

/// <summary>Batas satu periode laporan.</summary>
/// <param name="Period">Jenis periode.</param>
/// <param name="TanggalMulai">Hari pertama (WIB, inklusif); <c>null</c> untuk <see cref="ReportPeriod.Semua"/>.</param>
/// <param name="TanggalSelesai">Hari terakhir (WIB, inklusif), mis. hari Minggu untuk periode mingguan.</param>
/// <param name="MulaiUtc">00.00 WIB hari pertama dalam UTC (inklusif); <c>null</c> = tanpa batas awal.</param>
/// <param name="SelesaiUtc">00.00 WIB sehari setelah <paramref name="TanggalSelesai"/> dalam UTC (<b>eksklusif</b>).</param>
public sealed record PeriodRange(
    ReportPeriod Period,
    DateOnly? TanggalMulai,
    DateOnly TanggalSelesai,
    DateTimeOffset? MulaiUtc,
    DateTimeOffset SelesaiUtc)
{
    /// <summary><c>true</c> jika <paramref name="instant"/> ada di [<see cref="MulaiUtc"/>, <see cref="SelesaiUtc"/>).</summary>
    public bool Contains(DateTimeOffset instant) =>
        (MulaiUtc is null || instant >= MulaiUtc.Value) && instant < SelesaiUtc;
}
