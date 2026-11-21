using System.Data;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Tests.Fakes;

/// <summary>
/// <see cref="IAdminQueryRepository"/> di memori. Filter dan paging meniru SQL <c>AdminQueryRepository</c>
/// (rentang setengah terbuka, kurir sama persis, kata kunci "mengandung" tanpa peka huruf besar/kecil).
/// </summary>
public sealed class InMemoryAdminQueryRepository : IAdminQueryRepository
{
    private int _nextId = 1;

    public List<AdminRecordDto> Records { get; } = [];

    public List<(string KodeKurir, string NamaKurir)> Couriers { get; } =
        [("jne", "JNE"), ("jnt", "J&T Express"), ("sicepat", "SiCepat")];

    /// <summary>Semua query yang diterima, sesuai urutan pemanggilan (nama method + query).</summary>
    public List<(string Method, AdminRecordQuery Query)> Calls { get; } = [];

    public AdminRecordDto Add(
        DateTimeOffset calculatedAt,
        decimal totalCo2eKg,
        string kodeKurir = "jne",
        string nama = "Budi",
        string email = "budi@test.id",
        int userId = 1)
    {
        var id = _nextId++;
        var record = new AdminRecordDto(
            id, id, userId, nama, email, $"RESI{id:D8}", kodeKurir,
            Couriers.First(c => c.KodeKurir == kodeKurir).NamaKurir,
            "Jakarta", "Medan", TransportMode.Pesawat, 1m, 1000m, totalCo2eKg, calculatedAt);
        Records.Add(record);
        return record;
    }

    public Task<IReadOnlyList<AdminRecordDto>> GetRecordsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((nameof(GetRecordsAsync), query));
        IReadOnlyList<AdminRecordDto> page = Filter(query)
            .OrderByDescending(r => r.CalculatedAt).ThenByDescending(r => r.RecordId)
            .Skip((int)query.Offset).Take(query.UkuranHalaman)
            .ToList();
        return Task.FromResult(page);
    }

    public Task<int> CountRecordsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((nameof(CountRecordsAsync), query));
        return Task.FromResult(Filter(query).Count());
    }

    public Task<AdminTotals> GetTotalsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((nameof(GetTotalsAsync), query));
        var rows = Filter(query).ToList();
        return Task.FromResult(new AdminTotals(
            rows.Select(r => r.UserId).Distinct().Count(), rows.Count, rows.Sum(r => r.TotalCo2eKg)));
    }

    public Task<IReadOnlyList<AdminCourierTotalDto>> GetCourierTotalsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((nameof(GetCourierTotalsAsync), query));
        var rows = Filter(query).ToList();
        IReadOnlyList<AdminCourierTotalDto> totals = Couriers
            .Where(c => query.KodeKurir is null || c.KodeKurir == query.KodeKurir)
            .Select(c =>
            {
                var own = rows.Where(r => r.KodeKurir == c.KodeKurir).ToList();
                return new AdminCourierTotalDto(c.KodeKurir, c.NamaKurir, own.Count, own.Sum(r => r.TotalCo2eKg));
            })
            .ToList();
        return Task.FromResult(totals);
    }

    private IEnumerable<AdminRecordDto> Filter(AdminRecordQuery q) => Records.Where(r =>
        (q.FromUtc is null || r.CalculatedAt >= q.FromUtc) &&
        (q.ToUtc is null || r.CalculatedAt < q.ToUtc) &&
        (q.KodeKurir is null || r.KodeKurir == q.KodeKurir) &&
        (q.KataKunci is null ||
         r.NamaPengguna.Contains(q.KataKunci, StringComparison.OrdinalIgnoreCase) ||
         r.Email.Contains(q.KataKunci, StringComparison.OrdinalIgnoreCase)));
}
