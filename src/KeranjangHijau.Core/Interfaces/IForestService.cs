using System.Data;
using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Logika hutan virtual (CLAUDE.md 7.7).</summary>
public interface IForestService
{
    /// <summary>
    /// Menurunkan kesehatan hutan <paramref name="userId"/> karena emisi satu paket. Pemulihan alami yang tertunda
    /// diterapkan lebih dulu (lihat <see cref="ApplyRecoveryAsync"/>), lalu <c>forests</c> diperbarui (kesehatan,
    /// status, <c>updated_at</c> = sekarang) dan <c>forest_logs</c> ditulis dengan <c>record_id</c> terisi,
    /// semuanya di dalam <paramref name="transaction"/>.
    /// </summary>
    /// <returns>Kondisi hutan setelah emisi; <see cref="ForestSnapshotDto.Perubahan"/> hanya berisi penurunan karena emisi.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="totalCo2eKg"/> negatif.</exception>
    /// <exception cref="Exceptions.NotFoundException">Pengguna belum punya hutan.</exception>
    Task<ForestSnapshotDto> ApplyEmissionAsync(
        int userId,
        int recordId,
        string nomorResi,
        decimal totalCo2eKg,
        IDbTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Menerapkan pemulihan alami (+<c>RecoveryPointsPerDay</c> per hari penuh sejak <c>updated_at</c>, maks. 100)
    /// di dalam <paramref name="transaction"/>. Jika kesehatan naik, ditulis log <c>record_id = NULL</c> dengan
    /// keterangan "Pemulihan alami". <c>updated_at</c> dimajukan sebanyak hari penuh (sisa jam tetap terhitung);
    /// jika kesehatan sudah 100, <c>updated_at</c> menjadi sekarang tanpa log.
    /// </summary>
    /// <returns>Kondisi hutan setelah pemulihan; <see cref="ForestSnapshotDto.Perubahan"/> = 0 jika tidak ada pemulihan.</returns>
    /// <exception cref="Exceptions.NotFoundException">Pengguna belum punya hutan.</exception>
    Task<ForestSnapshotDto> ApplyRecoveryAsync(
        int userId, IDbTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Menjalankan pemulihan alami (dalam transaksi sendiri), lalu mengembalikan kondisi hutan.</summary>
    /// <exception cref="Exceptions.NotFoundException">Pengguna belum punya hutan.</exception>
    Task<ForestDto> GetForestAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Riwayat hutan terbaru lebih dulu (maks. <paramref name="limit"/> baris). Hanya membaca; tidak menjalankan
    /// pemulihan (panggil <see cref="GetForestAsync"/> lebih dulu agar pemulihan terbaru ikut tercatat).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> kurang dari 1.</exception>
    /// <exception cref="Exceptions.NotFoundException">Pengguna belum punya hutan.</exception>
    Task<IReadOnlyList<ForestLogDto>> GetForestHistoryAsync(
        int userId, int limit = 30, CancellationToken cancellationToken = default);
}
