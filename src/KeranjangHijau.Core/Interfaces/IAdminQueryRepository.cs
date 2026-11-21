using System.Data;
using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>
/// Query HANYA BACA untuk halaman admin (M7.1): hasil perhitungan emisi semua pengguna.
/// Semua method hanya menjalankan SELECT berparameter; sengaja tidak ada method tulis/hapus.
/// </summary>
/// <remarks>Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu.</remarks>
public interface IAdminQueryRepository
{
    /// <summary>
    /// Satu halaman hasil hitung yang cocok dengan <paramref name="query"/>, terbaru lebih dulu
    /// (<c>calculated_at DESC, record_id DESC</c>).
    /// </summary>
    Task<IReadOnlyList<AdminRecordDto>> GetRecordsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>Jumlah seluruh hasil hitung yang cocok dengan <paramref name="query"/> (paging diabaikan).</summary>
    Task<int> CountRecordsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>Total pengguna, paket, dan CO2e yang cocok dengan <paramref name="query"/> (paging diabaikan).</summary>
    Task<AdminTotals> GetTotalsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Total per kurir (paging diabaikan). Semua kurir dikembalikan (urut <c>courier_id</c>), atau hanya kurir
    /// <see cref="AdminRecordQuery.KodeKurir"/> jika diisi; kurir tanpa data bernilai 0.
    /// </summary>
    Task<IReadOnlyList<AdminCourierTotalDto>> GetCourierTotalsAsync(
        AdminRecordQuery query, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
