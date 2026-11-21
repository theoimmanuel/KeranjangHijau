using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Laporan emisi per periode untuk pengguna yang sedang login (CLAUDE.md 7.8).</summary>
public interface IReportService
{
    /// <summary>
    /// Membuat laporan periode <paramref name="period"/> yang memuat <paramref name="referenceDate"/>
    /// (tanggal kalender WIB; default hari ini WIB). Hanya data milik pengguna di sesi login yang dibaca.
    /// Periode tanpa paket menghasilkan laporan kosong yang valid, bukan error.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> tidak dikenal atau tanggal acuan di luar rentang.</exception>
    /// <exception cref="Exceptions.UnauthorizedException">Belum login, atau yang login adalah admin.</exception>
    /// <exception cref="Exceptions.NotFoundException">Pengguna belum punya hutan.</exception>
    Task<ReportDto> GetReportAsync(
        ReportPeriod period, DateOnly? referenceDate = null, CancellationToken cancellationToken = default);
}
