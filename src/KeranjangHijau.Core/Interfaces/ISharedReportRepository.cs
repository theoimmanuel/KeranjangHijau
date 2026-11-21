using System.Data;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>shared_reports</c> (riwayat bagikan).</summary>
/// <remarks>Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu.</remarks>
public interface ISharedReportRepository
{
    /// <summary>Menyimpan satu riwayat bagikan. <c>CreatedAt</c> default → waktu sekarang.</summary>
    /// <returns><c>share_id</c> baru.</returns>
    /// <exception cref="ArgumentException">
    /// Jumlah paket atau total negatif, periode selesai sebelum mulai, atau <c>FileGambar</c> lebih dari 260 karakter.
    /// </exception>
    /// <exception cref="Exceptions.NotFoundException">Pengguna tidak ada.</exception>
    Task<int> InsertAsync(
        SharedReport report, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>Riwayat bagikan milik <paramref name="userId"/>, terbaru lebih dulu (maks. <paramref name="limit"/> baris).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> kurang dari 1.</exception>
    Task<IReadOnlyList<SharedReport>> GetByUserIdAsync(
        int userId, int limit = 50, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
