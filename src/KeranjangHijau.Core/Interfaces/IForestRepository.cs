using System.Data;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>forests</c>. Aturan <c>transaction</c> sama dengan <see cref="IUserRepository"/>.</summary>
public interface IForestRepository
{
    Task<Forest?> GetByUserIdAsync(int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sama dengan <see cref="GetByUserIdAsync"/>, tetapi mengunci baris hutan (<c>SELECT ... FOR UPDATE</c>) sampai
    /// <paramref name="transaction"/> selesai. Dipakai sebelum mengubah kesehatan (pemulihan, emisi), supaya dua proses
    /// yang berjalan bersamaan tidak menerapkan pemulihan dua kali atau menimpa perubahan satu sama lain.
    /// </summary>
    Task<Forest?> GetByUserIdForUpdateAsync(int userId, IDbTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Menyimpan hutan baru dan mengembalikan <c>forest_id</c>. <see cref="Forest.ForestId"/> diabaikan.
    /// Jika <see cref="Forest.UpdatedAt"/> kosong (default), dipakai waktu dari <see cref="IClock"/>.
    /// </summary>
    /// <exception cref="Exceptions.ValidationException">Pengguna sudah punya hutan.</exception>
    Task<int> InsertAsync(Forest forest, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>Mengubah kesehatan, status, dan <c>updated_at</c> hutan.</summary>
    /// <exception cref="Exceptions.NotFoundException">Hutan dengan <paramref name="forestId"/> tidak ada.</exception>
    Task UpdateHealthAsync(
        int forestId,
        decimal kesehatan,
        string status,
        DateTimeOffset updatedAt,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
