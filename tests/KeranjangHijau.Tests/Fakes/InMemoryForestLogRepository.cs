using System.Data;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="IForestLogRepository"/> di memori.</summary>
public sealed class InMemoryForestLogRepository : IForestLogRepository
{
    private int _nextId = 1;

    public List<ForestLog> Logs { get; } = [];

    public List<IDbTransaction?> InsertTransactions { get; } = [];

    /// <summary>Jika diisi, <see cref="InsertAsync"/> melempar exception ini.</summary>
    public Exception? FailOnInsert { get; set; }

    public Task<int> InsertAsync(ForestLog log, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        InsertTransactions.Add(transaction);
        if (FailOnInsert is not null)
        {
            return Task.FromException<int>(FailOnInsert);
        }

        if (log.RecordId is not null && Logs.Any(l => l.RecordId == log.RecordId))
        {
            return Task.FromException<int>(new InvalidOperationException("Log hutan untuk record emisi ini sudah tersimpan."));
        }

        var copy = new ForestLog
        {
            LogId = _nextId++,
            ForestId = log.ForestId,
            RecordId = log.RecordId,
            Perubahan = log.Perubahan,
            KesehatanSetelah = log.KesehatanSetelah,
            Keterangan = log.Keterangan,
            CreatedAt = log.CreatedAt,
        };
        Logs.Add(copy);
        return Task.FromResult(copy.LogId);
    }

    public Task<IReadOnlyList<ForestLog>> GetByForestIdAsync(
        int forestId, int limit, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        IReadOnlyList<ForestLog> result = Logs
            .Where(l => l.ForestId == forestId)
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.LogId)
            .Take(limit)
            .ToList();
        return Task.FromResult(result);
    }
}
