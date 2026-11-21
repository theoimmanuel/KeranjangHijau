using System.Data;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="ISharedReportRepository"/> di memori.</summary>
public sealed class InMemorySharedReportRepository : ISharedReportRepository
{
    private readonly List<SharedReport> _reports = [];
    private int _nextId = 1;

    public IReadOnlyList<SharedReport> Reports => _reports;

    /// <summary>Jika diisi, <see cref="InsertAsync"/> melempar exception ini (simulasi kegagalan DB).</summary>
    public Exception? FailOnInsert { get; set; }

    public Task<int> InsertAsync(SharedReport report, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        if (FailOnInsert is not null)
        {
            throw FailOnInsert;
        }

        var copy = new SharedReport
        {
            ShareId = _nextId++,
            UserId = report.UserId,
            PeriodeMulai = report.PeriodeMulai,
            PeriodeSelesai = report.PeriodeSelesai,
            JumlahPaket = report.JumlahPaket,
            TotalCo2eKg = report.TotalCo2eKg,
            FileGambar = report.FileGambar,
            CreatedAt = report.CreatedAt,
        };
        _reports.Add(copy);
        return Task.FromResult(copy.ShareId);
    }

    public Task<IReadOnlyList<SharedReport>> GetByUserIdAsync(
        int userId, int limit = 50, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        IReadOnlyList<SharedReport> result = _reports
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.ShareId)
            .Take(limit)
            .ToList();
        return Task.FromResult(result);
    }
}
