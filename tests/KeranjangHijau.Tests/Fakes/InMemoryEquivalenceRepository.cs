using System.Data;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="IEquivalenceRepository"/> di memori. Isi <see cref="Equivalences"/> sendiri di setiap test.</summary>
public sealed class InMemoryEquivalenceRepository : IEquivalenceRepository
{
    public List<Equivalence> Equivalences { get; } = [];

    public List<RecordEquivalence> Rows { get; } = [];

    public List<IDbTransaction?> GetAllTransactions { get; } = [];

    public List<IDbTransaction?> InsertTransactions { get; } = [];

    /// <summary>Jika diisi, <see cref="InsertRecordEquivalencesAsync"/> melempar exception ini.</summary>
    public Exception? FailOnInsert { get; set; }

    public Task<IReadOnlyList<Equivalence>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        GetAllTransactions.Add(transaction);
        return Task.FromResult<IReadOnlyList<Equivalence>>(Equivalences.OrderBy(e => e.EquivalenceId).ToList());
    }

    public Task<int> InsertRecordEquivalencesAsync(
        IReadOnlyCollection<RecordEquivalence> rows, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        InsertTransactions.Add(transaction);
        if (FailOnInsert is not null)
        {
            return Task.FromException<int>(FailOnInsert);
        }

        Rows.AddRange(rows);
        return Task.FromResult(rows.Count);
    }

    public Task<IReadOnlyList<EquivalenceValue>> GetByRecordIdAsync(
        int recordId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EquivalenceValue> values = Rows
            .Where(r => r.RecordId == recordId)
            .Join(Equivalences, r => r.EquivalenceId, e => e.EquivalenceId, (r, e) => new EquivalenceValue(e, r.NilaiSetara))
            .OrderBy(v => v.Equivalence.EquivalenceId)
            .ToList();
        return Task.FromResult(values);
    }
}
