using System.Data;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>emission_records</c>.</summary>
/// <remarks>Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu.</remarks>
public interface IEmissionRecordRepository
{
    /// <summary>
    /// Menyimpan hasil hitung emisi dan mengembalikan <c>record_id</c>. <see cref="EmissionRecord.RecordId"/> diabaikan.
    /// Jika <see cref="EmissionRecord.CalculatedAt"/> kosong (default), dipakai waktu dari <see cref="IClock"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Shipment itu sudah punya record (1 shipment = 1 record).</exception>
    /// <exception cref="Exceptions.NotFoundException">Shipment tidak ada.</exception>
    Task<int> InsertAsync(EmissionRecord record, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <returns>Record yang cocok, atau <c>null</c> jika tidak ada.</returns>
    Task<EmissionRecord?> GetByIdAsync(int recordId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
