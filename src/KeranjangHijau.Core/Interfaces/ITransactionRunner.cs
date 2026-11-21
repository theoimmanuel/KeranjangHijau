using System.Data;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>
/// Menjalankan beberapa operasi repository dalam SATU transaksi database.
/// Commit jika <c>work</c> selesai tanpa error; rollback lalu exception diteruskan jika gagal.
/// </summary>
/// <remarks>
/// Teruskan <see cref="IDbTransaction"/> yang diterima ke setiap method repository di dalam <c>work</c>.
/// Abstraksi ini membuat service di Core bisa dites dengan fake tanpa database.
/// </remarks>
public interface ITransactionRunner
{
    Task<T> ExecuteAsync<T>(
        Func<IDbTransaction, CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default);
}
