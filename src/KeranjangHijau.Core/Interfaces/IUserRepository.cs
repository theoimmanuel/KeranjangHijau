using System.Data;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>users</c>.</summary>
/// <remarks>
/// Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu
/// (koneksi tidak ditutup oleh repository). Jika null, repository membuka koneksi sendiri.
/// Email selalu dinormalisasi (trim + huruf kecil) sebelum disimpan atau dicari.
/// </remarks>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Menyimpan user baru dan mengembalikan <c>user_id</c>. <see cref="User.UserId"/> diabaikan.
    /// Jika <see cref="User.CreatedAt"/> kosong (default), dipakai waktu dari <see cref="IClock"/>.
    /// </summary>
    /// <exception cref="Exceptions.ValidationException">Email sudah terdaftar.</exception>
    Task<int> InsertAsync(User user, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
