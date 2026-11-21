using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Core.Services;

/// <summary>Implementasi <see cref="IReportService"/> (CLAUDE.md 7.8).</summary>
/// <remarks>
/// ID pengguna selalu diambil dari <see cref="ICurrentUserSession"/>, tidak pernah dari parameter,
/// jadi pengguna hanya bisa melihat datanya sendiri.
/// </remarks>
public sealed class ReportService : IReportService
{
    public const string NotLoggedInMessage = "Silakan login terlebih dahulu.";
    public const string NotPenggunaMessage = "Hanya pengguna yang dapat melihat laporan emisi.";

    private readonly ICurrentUserSession _session;
    private readonly IReportRepository _reports;
    private readonly IForestService _forests;
    private readonly IClock _clock;

    public ReportService(
        ICurrentUserSession session,
        IReportRepository reports,
        IForestService forests,
        IClock clock)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _forests = forests ?? throw new ArgumentNullException(nameof(forests));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ReportDto> GetReportAsync(
        ReportPeriod period, DateOnly? referenceDate = null, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(period))
        {
            throw new ArgumentOutOfRangeException(nameof(period), period, "Periode laporan tidak dikenal.");
        }

        var user = EnsurePengguna();
        var reference = referenceDate ?? PeriodRangeRule.ToJakartaDate(_clock.UtcNow);
        var range = PeriodRangeRule.Calculate(period, reference);

        var paket = await _reports
            .GetShipmentsAsync(user.UserId, range.MulaiUtc, range.SelesaiUtc, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var harian = await _reports
            .GetDailyTotalsAsync(user.UserId, range.MulaiUtc, range.SelesaiUtc, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var setara = await _reports
            .GetEquivalenceTotalsAsync(user.UserId, range.MulaiUtc, range.SelesaiUtc, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var hutan = await _forests.GetForestAsync(user.UserId, cancellationToken).ConfigureAwait(false);

        // "Semua" tidak punya batas awal: tampilkan sejak hari paket tertua (atau tanggal acuan jika kosong).
        var tanggalMulai = range.TanggalMulai
            ?? (paket.Count > 0 ? PeriodRangeRule.ToJakartaDate(paket.Min(p => p.CreatedAt)) : reference);
        var periodeMulai = range.MulaiUtc ?? PeriodRangeRule.StartOfJakartaDayUtc(tanggalMulai);

        var dataHarian = range.TanggalMulai is { } mulai
            ? FillMissingDays(harian, mulai, range.TanggalSelesai)
            : harian.OrderBy(d => d.Tanggal).ToList();

        return new ReportDto(
            period,
            periodeMulai,
            range.SelesaiUtc,
            tanggalMulai,
            range.TanggalSelesai,
            paket.Count,
            paket.Sum(p => p.TotalCo2eKg),
            paket,
            dataHarian,
            setara.Select(v => v.ToDto()).ToList(),
            hutan);
    }

    /// <summary>Satu baris per hari dari <paramref name="first"/> sampai <paramref name="last"/>; hari tanpa data bernilai 0.</summary>
    private static List<DailyEmissionDto> FillMissingDays(
        IReadOnlyList<DailyEmissionDto> totals, DateOnly first, DateOnly last)
    {
        var byDate = totals.ToDictionary(t => t.Tanggal);
        var result = new List<DailyEmissionDto>(last.DayNumber - first.DayNumber + 1);
        for (var day = first; day <= last; day = day.AddDays(1))
        {
            result.Add(byDate.TryGetValue(day, out var total) ? total : new DailyEmissionDto(day, 0, 0m));
        }

        return result;
    }

    private UserDto EnsurePengguna()
    {
        var user = _session.CurrentUser ?? throw new UnauthorizedException(NotLoggedInMessage);
        if (user.Role != UserRole.Pengguna)
        {
            throw new UnauthorizedException(NotPenggunaMessage);
        }

        return user;
    }
}
