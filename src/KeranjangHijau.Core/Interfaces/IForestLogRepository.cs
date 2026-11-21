using System.Data;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>forest_logs</c>. Aturan <c>transaction</c> sama dengan <see cref="IUserRepository"/>.</summary>
public interface IForestLogRepository
{
    /// <summary>
    /// Menyimpan log baru dan mengembalikan <c>log_id</c>. <see cref="ForestLog.LogId"/> diabaikan.
    /// Jika <see cref="ForestLog.CreatedAt"/> kosong (default), dipakai waktu dari <see cref="IClock"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Keterangan kosong atau lebih dari 200 karakter.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Kesehatan setelah di luar 0–100.</exception>
    /// <exception cref="InvalidOperationException">Record emisi ini sudah punya log.</exception>
    /// <exception cref="Exceptions.NotFoundException">Hutan atau record emisi tidak ada.</exception>
    Task<int> InsertAsync(ForestLog log, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>Log satu hutan, terbaru lebih dulu (<c>created_at</c>, lalu <c>log_id</c> menurun).</summary>
    /// <param name="forestId">ID hutan.</param>
    /// <param name="limit">Jumlah log maksimum (≥ 1).</param>
    /// <param name="transaction">Transaksi opsional.</param>
    /// <param name="cancellationToken">Token pembatalan.</param>
    /// <returns>Daftar kosong jika hutan belum punya log atau tidak ada.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> kurang dari 1.</exception>
    Task<IReadOnlyList<ForestLog>> GetByForestIdAsync(
        int forestId, int limit, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
