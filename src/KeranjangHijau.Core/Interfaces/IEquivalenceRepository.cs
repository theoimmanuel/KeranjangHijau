using System.Data;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>equivalences</c> (hanya baca) dan <c>record_equivalences</c>.</summary>
/// <remarks>Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu.</remarks>
public interface IEquivalenceRepository
{
    /// <summary>Semua aktivitas pembanding, urut <c>equivalence_id</c>.</summary>
    Task<IReadOnlyList<Equivalence>> GetAllAsync(
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>Menyimpan nilai setara satu atau beberapa hasil emisi. Daftar kosong tidak menyentuh database.</summary>
    /// <returns>Jumlah baris yang disimpan.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Ada <see cref="RecordEquivalence.NilaiSetara"/> negatif.</exception>
    /// <exception cref="InvalidOperationException">Pasangan record + aktivitas sudah tersimpan.</exception>
    /// <exception cref="Exceptions.NotFoundException">Record emisi atau aktivitas tidak ada.</exception>
    Task<int> InsertRecordEquivalencesAsync(
        IReadOnlyCollection<RecordEquivalence> rows,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>Nilai setara milik satu hasil emisi (beserta data aktivitasnya), urut <c>equivalence_id</c>.</summary>
    /// <returns>Daftar kosong jika record tidak punya nilai setara.</returns>
    Task<IReadOnlyList<EquivalenceValue>> GetByRecordIdAsync(
        int recordId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
