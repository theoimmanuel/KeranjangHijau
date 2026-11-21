using System.Data;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>cities</c> (hanya baca). Dipakai untuk mencocokkan nama kota dari tracker.</summary>
/// <remarks>Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu.</remarks>
public interface ICityRepository
{
    /// <summary>
    /// Kota dengan nama tertentu, tidak peka huruf besar/kecil dan spasi di awal/akhir diabaikan.
    /// Jika ada beberapa kota bernama sama (beda provinsi), dikembalikan yang <c>city_id</c>-nya terkecil.
    /// </summary>
    /// <returns>Kota yang cocok, atau <c>null</c> jika tidak ada.</returns>
    Task<City?> GetByNameAsync(string namaKota, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>Semua kota, urut <c>city_id</c>. Dipakai <c>CityNameMatcher</c> untuk pencocokan nama yang toleran (M2.4).</summary>
    Task<IReadOnlyList<City>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<City?> GetByIdAsync(int cityId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
