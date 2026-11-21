using System.Data;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Tests.Fakes;

/// <summary>
/// <see cref="ITransactionRunner"/> tanpa database. Mencatat apakah transaksi di-commit atau
/// di-rollback, dan memberikan <see cref="FakeDbTransaction"/> ke <c>work</c>.
/// </summary>
public sealed class FakeTransactionRunner : ITransactionRunner
{
    public int Commits { get; private set; }

    public int Rollbacks { get; private set; }

    /// <summary>Transaksi terakhir yang diberikan ke <c>work</c>.</summary>
    public FakeDbTransaction? LastTransaction { get; private set; }

    public async Task<T> ExecuteAsync<T>(
        Func<IDbTransaction, CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        var tx = new FakeDbTransaction();
        LastTransaction = tx;
        try
        {
            var result = await work(tx, cancellationToken);
            Commits++;
            return result;
        }
        catch
        {
            Rollbacks++;
            throw;
        }
    }
}

/// <summary>Transaksi palsu; hanya dipakai sebagai penanda oleh repository in-memory.</summary>
public sealed class FakeDbTransaction : IDbTransaction
{
    public IDbConnection? Connection => null;

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    public void Commit()
    {
    }

    public void Rollback()
    {
    }

    public void Dispose()
    {
    }
}
