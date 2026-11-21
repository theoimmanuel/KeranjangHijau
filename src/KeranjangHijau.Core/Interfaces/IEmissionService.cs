using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Alur hitung emisi dari nomor resi (CLAUDE.md 7.1–7.6, 7.10).</summary>
public interface IEmissionService
{
    /// <summary>
    /// Memvalidasi resi, mengambil data paket dari tracker, menghitung emisi, lalu menyimpan
    /// <c>shipments</c> + <c>emission_records</c> dalam satu transaksi.
    /// </summary>
    /// <exception cref="Exceptions.UnauthorizedException">Belum login, atau yang login adalah admin.</exception>
    /// <exception cref="Exceptions.ValidationException">Resi/kurir/berat tidak valid, atau kota belum terdaftar.</exception>
    /// <exception cref="Exceptions.DuplicateResiException">Resi sudah pernah dicatat (jika duplikat tidak diizinkan).</exception>
    /// <exception cref="Exceptions.NotFoundException">Data resi tidak ditemukan, atau faktor emisi moda belum ada.</exception>
    Task<EmissionResultDto> CalculateFromResiAsync(EmissionRequest request, CancellationToken cancellationToken = default);
}
