using System.Data;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="IForestRepository"/> di memori.</summary>
public sealed class InMemoryForestRepository : IForestRepository
{
    private readonly List<Forest> _forests = [];
    private int _nextId = 1;

    public IReadOnlyList<Forest> Forests => _forests;

    public List<IDbTransaction?> InsertTransactions { get; } = [];

    public List<IDbTransaction?> GetTransactions { get; } = [];

    public List<IDbTransaction?> UpdateTransactions { get; } = [];

    /// <summary>Transaksi yang dipakai <see cref="GetByUserIdForUpdateAsync"/> (baca dengan kunci baris).</summary>
    public List<IDbTransaction?> ForUpdateTransactions { get; } = [];

    /// <summary>Jika diisi, <see cref="InsertAsync"/> melempar exception ini (simulasi kegagalan DB).</summary>
    public Exception? FailOnInsert { get; set; }

    public Task<Forest?> GetByUserIdAsync(int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        GetTransactions.Add(transaction);
        return Task.FromResult(_forests.SingleOrDefault(f => f.UserId == userId));
    }

    public Task<Forest?> GetByUserIdForUpdateAsync(int userId, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        ForUpdateTransactions.Add(transaction);
        return Task.FromResult(_forests.SingleOrDefault(f => f.UserId == userId));
    }

    public Task<int> InsertAsync(Forest forest, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        InsertTransactions.Add(transaction);
        if (FailOnInsert is not null)
        {
            throw FailOnInsert;
        }

        if (_forests.Any(f => f.UserId == forest.UserId))
        {
            throw new ValidationException("Pengguna ini sudah memiliki hutan.");
        }

        var copy = new Forest
        {
            ForestId = _nextId++,
            UserId = forest.UserId,
            NamaHutan = forest.NamaHutan,
            JumlahPohonAwal = forest.JumlahPohonAwal,
            Kesehatan = forest.Kesehatan,
            Status = forest.Status,
            UpdatedAt = forest.UpdatedAt,
        };
        _forests.Add(copy);
        return Task.FromResult(copy.ForestId);
    }

    public Task UpdateHealthAsync(
        int forestId,
        decimal kesehatan,
        string status,
        DateTimeOffset updatedAt,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        UpdateTransactions.Add(transaction);
        var forest = _forests.SingleOrDefault(f => f.ForestId == forestId)
            ?? throw new NotFoundException("Hutan tidak ditemukan.");
        forest.Kesehatan = kesehatan;
        forest.Status = status;
        forest.UpdatedAt = updatedAt;
        return Task.CompletedTask;
    }
}
