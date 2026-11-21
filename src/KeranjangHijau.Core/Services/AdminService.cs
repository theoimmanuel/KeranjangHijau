using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Core.Services;

/// <summary>Implementasi <see cref="IAdminService"/> (M7.1). Hanya membaca lewat <see cref="IAdminQueryRepository"/>.</summary>
public sealed class AdminService : IAdminService
{
    public const string NotAdminMessage = "Hanya admin yang bisa membuka halaman ini.";

    /// <summary>Desimal rata-rata CO2e per paket (sama dengan pembulatan total emisi paket).</summary>
    public const int AverageDecimals = 4;

    private readonly ICurrentUserSession _session;
    private readonly IAdminQueryRepository _queries;

    public AdminService(ICurrentUserSession session, IAdminQueryRepository queries)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<PagedResult<AdminRecordDto>> GetRecordsAsync(
        AdminRecordFilter filter, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var query = AdminFilterRule.Normalize(filter);

        var total = await _queries.CountRecordsAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AdminRecordDto> items = total > query.Offset
            ? await _queries.GetRecordsAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false)
            : [];

        return new PagedResult<AdminRecordDto>(items, total, query.Halaman, query.UkuranHalaman);
    }

    public async Task<AdminSummaryDto> GetSummaryAsync(
        AdminRecordFilter filter, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var query = AdminFilterRule.Normalize(filter);

        var totals = await _queries.GetTotalsAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);
        var perKurir = await _queries.GetCourierTotalsAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new AdminSummaryDto(
            totals.TotalPengguna,
            totals.TotalPaket,
            totals.TotalCo2eKg,
            Average(totals.TotalCo2eKg, totals.TotalPaket),
            perKurir);
    }

    /// <summary>Rata-rata per paket, dibulatkan <see cref="AverageDecimals"/> desimal; 0 jika tidak ada paket.</summary>
    public static decimal Average(decimal totalCo2eKg, int totalPaket) =>
        totalPaket <= 0 ? 0m : Math.Round(totalCo2eKg / totalPaket, AverageDecimals, MidpointRounding.AwayFromZero);

    /// <summary>Dicek sebelum filter divalidasi dan sebelum database disentuh.</summary>
    private void EnsureAdmin()
    {
        if (!_session.IsAdmin)
        {
            throw new UnauthorizedException(NotAdminMessage);
        }
    }
}
