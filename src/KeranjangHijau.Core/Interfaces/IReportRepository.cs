using System.Data;
using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>
/// Query laporan emisi milik SATU pengguna (CLAUDE.md 7.8). Semua method wajib memfilter <c>user_id</c>,
/// dan rentang waktu memakai <c>shipments.created_at</c> dalam bentuk setengah terbuka [<c>fromUtc</c>, <c>toUtc</c>).
/// </summary>
/// <remarks>Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu.</remarks>
public interface IReportRepository
{
    /// <summary>Paket milik <paramref name="userId"/> dalam rentang, terbaru lebih dulu.</summary>
    /// <param name="fromUtc">Batas awal (inklusif); <c>null</c> = tanpa batas awal.</param>
    /// <param name="toUtc">Batas akhir (eksklusif).</param>
    /// <exception cref="ArgumentException"><paramref name="fromUtc"/> setelah <paramref name="toUtc"/>.</exception>
    Task<IReadOnlyList<ReportShipmentDto>> GetShipmentsAsync(
        int userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset toUtc,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Jumlah paket dan total CO2e per tanggal kalender WIB, urut tanggal. Hanya hari yang ada paketnya.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="fromUtc"/> setelah <paramref name="toUtc"/>.</exception>
    Task<IReadOnlyList<DailyEmissionDto>> GetDailyTotalsAsync(
        int userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset toUtc,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Total nilai setara per aktivitas untuk paket dalam rentang. Semua baris <c>equivalences</c> dikembalikan
    /// (urut <c>equivalence_id</c>); aktivitas tanpa data bernilai 0.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="fromUtc"/> setelah <paramref name="toUtc"/>.</exception>
    Task<IReadOnlyList<EquivalenceValue>> GetEquivalenceTotalsAsync(
        int userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset toUtc,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
