using System.Data;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Fakes;

/// <summary>
/// <see cref="IReportRepository"/> di memori. Meniru query asli: filter pemilik, rentang [from, to),
/// urut terbaru dulu, total harian per tanggal WIB, dan semua aktivitas (0 jika tidak ada data).
/// </summary>
public sealed class InMemoryReportRepository : IReportRepository
{
    private readonly List<StoredShipment> _shipments = [];
    private int _nextId = 1;

    /// <summary>Isi tabel <c>equivalences</c>.</summary>
    public List<Equivalence> Activities { get; } = [];

    /// <summary>Setiap pemanggilan query: (nama method, userId, fromUtc, toUtc).</summary>
    public List<(string Method, int UserId, DateTimeOffset? FromUtc, DateTimeOffset ToUtc)> Calls { get; } = [];

    /// <summary>Menambah paket + record + nilai setara (key = <c>equivalence_id</c>).</summary>
    public ReportShipmentDto Add(
        int userId, DateTimeOffset createdAt, decimal totalCo2eKg, IReadOnlyDictionary<int, decimal>? nilaiSetara = null)
    {
        var id = _nextId++;
        var dto = new ReportShipmentDto(
            id,
            100 + id,
            $"JNE{id:D10}",
            "jne",
            "JNE",
            "Jakarta",
            "Medan",
            "REG",
            1m,
            1000m,
            KeranjangHijau.Core.Enums.TransportMode.Truk,
            totalCo2eKg,
            createdAt.ToUniversalTime());
        _shipments.Add(new StoredShipment(userId, dto, nilaiSetara ?? new Dictionary<int, decimal>()));
        return dto;
    }

    public Task<IReadOnlyList<ReportShipmentDto>> GetShipmentsAsync(
        int userId, DateTimeOffset? fromUtc, DateTimeOffset toUtc, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((nameof(GetShipmentsAsync), userId, fromUtc, toUtc));
        IReadOnlyList<ReportShipmentDto> result = Filter(userId, fromUtc, toUtc)
            .Select(s => s.Dto)
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.ShipmentId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DailyEmissionDto>> GetDailyTotalsAsync(
        int userId, DateTimeOffset? fromUtc, DateTimeOffset toUtc, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((nameof(GetDailyTotalsAsync), userId, fromUtc, toUtc));
        IReadOnlyList<DailyEmissionDto> result = Filter(userId, fromUtc, toUtc)
            .GroupBy(s => PeriodRangeRule.ToJakartaDate(s.Dto.CreatedAt))
            .OrderBy(g => g.Key)
            .Select(g => new DailyEmissionDto(g.Key, g.Count(), g.Sum(s => s.Dto.TotalCo2eKg)))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<EquivalenceValue>> GetEquivalenceTotalsAsync(
        int userId, DateTimeOffset? fromUtc, DateTimeOffset toUtc, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((nameof(GetEquivalenceTotalsAsync), userId, fromUtc, toUtc));
        var shipments = Filter(userId, fromUtc, toUtc).ToList();
        IReadOnlyList<EquivalenceValue> result = Activities
            .OrderBy(a => a.EquivalenceId)
            .Select(a => new EquivalenceValue(
                a,
                shipments.Sum(s => s.NilaiSetara.TryGetValue(a.EquivalenceId, out var v) ? v : 0m)))
            .ToList();
        return Task.FromResult(result);
    }

    private IEnumerable<StoredShipment> Filter(int userId, DateTimeOffset? fromUtc, DateTimeOffset toUtc) =>
        _shipments.Where(s =>
            s.UserId == userId
            && (fromUtc is null || s.Dto.CreatedAt >= fromUtc.Value)
            && s.Dto.CreatedAt < toUtc);

    private sealed record StoredShipment(int UserId, ReportShipmentDto Dto, IReadOnlyDictionary<int, decimal> NilaiSetara);
}
