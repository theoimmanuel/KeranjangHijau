using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Bagikan laporan sebagai kartu Instagram Story (CLAUDE.md 7.9). Tidak ada integrasi API Instagram.</summary>
public interface IShareService
{
    /// <summary>
    /// Menyusun isi kartu dari laporan periode <paramref name="period"/> (lihat <see cref="IReportService.GetReportAsync"/>)
    /// untuk pengguna yang sedang login.
    /// </summary>
    /// <exception cref="Exceptions.UnauthorizedException">Belum login, atau yang login adalah admin.</exception>
    /// <exception cref="Exceptions.ValidationException">Belum ada paket di periode ini.</exception>
    Task<ShareCardData> BuildCardDataAsync(
        ReportPeriod period, DateOnly? referenceDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Menyusun isi kartu, menggambar PNG ke <paramref name="outputFolder"/> lewat <see cref="IShareCardRenderer"/>,
    /// lalu menyimpan riwayat di <c>shared_reports</c>.
    /// </summary>
    /// <returns>Path lengkap file PNG.</returns>
    /// <exception cref="Exceptions.UnauthorizedException">Belum login, atau yang login adalah admin.</exception>
    /// <exception cref="Exceptions.ValidationException">
    /// Belum ada paket di periode ini, atau folder tujuan kosong/tidak valid/tidak bisa dipakai.
    /// </exception>
    Task<string> ShareAsync(
        ReportPeriod period, string outputFolder, DateOnly? referenceDate = null, CancellationToken cancellationToken = default);
}
