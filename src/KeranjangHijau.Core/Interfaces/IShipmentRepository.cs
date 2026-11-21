using System.Data;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>shipments</c>.</summary>
/// <remarks>Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu.</remarks>
public interface IShipmentRepository
{
    /// <summary>
    /// <c>true</c> jika sudah ada shipment dengan kurir dan nomor resi yang sama.
    /// Nomor resi dinormalisasi dulu (lihat <c>ResiNormalizer</c>).
    /// </summary>
    Task<bool> ExistsAsync(int courierId, string nomorResi, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Menyimpan shipment baru dan mengembalikan <c>shipment_id</c>. <see cref="Shipment.ShipmentId"/> diabaikan.
    /// Nomor resi dinormalisasi; jika <see cref="Shipment.CreatedAt"/> kosong (default), dipakai waktu dari <see cref="IClock"/>.
    /// </summary>
    /// <exception cref="Exceptions.DuplicateResiException">
    /// Melanggar <c>uq_shipments_courier_resi</c> (hanya setelah <c>enable-unique-resi.sql</c> dijalankan).
    /// </exception>
    /// <exception cref="Exceptions.NotFoundException">Pengguna tidak ada.</exception>
    Task<int> InsertAsync(Shipment shipment, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
