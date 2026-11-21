using System.Data;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>couriers</c> (hanya baca).</summary>
/// <remarks>
/// Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu.
/// Kode kurir dicari tanpa peka huruf besar/kecil dan spasi di awal/akhir diabaikan.
/// </remarks>
public interface ICourierRepository
{
    /// <summary>Semua kurir, diurutkan berdasarkan nama (untuk pilihan di UI).</summary>
    Task<IReadOnlyList<Courier>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>Kurir dengan kode tertentu, atau <c>null</c> jika tidak ada.</summary>
    Task<Courier?> GetByKodeAsync(string kodeKurir, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
