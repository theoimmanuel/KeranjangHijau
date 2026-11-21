using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>
/// Layanan admin (CLAUDE.md bagian 2, M7.1): melihat dan memfilter hasil perhitungan emisi semua pengguna.
/// <b>Hanya baca</b> — tidak ada method ubah atau hapus. Setiap method memeriksa
/// <see cref="ICurrentUserSession.IsAdmin"/> lebih dulu.
/// </summary>
public interface IAdminService
{
    /// <summary>Satu halaman hasil perhitungan emisi sesuai <paramref name="filter"/> + jumlah seluruh baris.</summary>
    /// <exception cref="Exceptions.UnauthorizedException">Belum login atau bukan admin.</exception>
    /// <exception cref="Exceptions.ValidationException">Rentang tanggal tidak valid.</exception>
    Task<PagedResult<AdminRecordDto>> GetRecordsAsync(
        AdminRecordFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Ringkasan (total pengguna, paket, CO2e, rata-rata, per kurir) sesuai filter; paging diabaikan.</summary>
    /// <exception cref="Exceptions.UnauthorizedException">Belum login atau bukan admin.</exception>
    /// <exception cref="Exceptions.ValidationException">Rentang tanggal tidak valid.</exception>
    Task<AdminSummaryDto> GetSummaryAsync(
        AdminRecordFilter filter, CancellationToken cancellationToken = default);
}
