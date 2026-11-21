using System.Data.Common;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>
/// Membuat koneksi database yang sudah terbuka. Pemanggil wajib men-dispose koneksi
/// (<c>await using var conn = await factory.OpenConnectionAsync();</c>).
/// </summary>
/// <remarks>
/// Memakai <see cref="DbConnection"/> dari BCL supaya Core tidak bergantung pada Npgsql.
/// Transaksi dibuat dengan <see cref="DbConnection.BeginTransactionAsync(CancellationToken)"/>.
/// </remarks>
public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
