using System.Data;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Akses tabel <c>emission_factors</c> (hanya baca).</summary>
/// <remarks>Jika <c>transaction</c> diisi, query berjalan di koneksi milik transaksi itu.</remarks>
public interface IEmissionFactorRepository
{
    /// <summary>Faktor emisi untuk satu moda transport.</summary>
    /// <returns>Faktor yang cocok, atau <c>null</c> jika moda itu belum ada di tabel.</returns>
    Task<EmissionFactor?> GetByModeAsync(
        TransportMode mode, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
