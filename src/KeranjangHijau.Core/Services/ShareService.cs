using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Core.Services;

/// <summary>Implementasi <see cref="IShareService"/> (CLAUDE.md 7.9).</summary>
/// <remarks>
/// Urutan <see cref="ShareAsync"/>: cek sesi → cek folder → laporan (M6.1) → tolak jika kosong → buat folder →
/// pilih nama file unik → render PNG → simpan <c>shared_reports</c>. Jika penyimpanan gagal, file PNG yang
/// baru dibuat dihapus agar tidak ada gambar tanpa riwayat.
/// </remarks>
public sealed class ShareService : IShareService
{
    public const string EmptyReportMessage = "Belum ada paket di periode ini untuk dibagikan.";
    public const string OutputFolderField = "OutputFolder";
    public const string OutputFolderRequiredMessage = "Folder tujuan wajib diisi.";
    public const string OutputFolderInvalidMessage = "Folder tujuan tidak valid.";
    public const string OutputFolderUnavailableMessage = "Folder tujuan tidak bisa dibuat atau diakses.";
    public const string OutputPathTooLongMessage = "Lokasi folder tujuan terlalu panjang. Pilih folder lain.";

    /// <summary>Batas percobaan akhiran nama file (<c>-1</c>, <c>-2</c>, ...) jika nama sudah dipakai.</summary>
    public const int MaxFileNameAttempts = 100;

    private readonly ICurrentUserSession _session;
    private readonly IReportService _reports;
    private readonly IShareCardRenderer _renderer;
    private readonly ISharedReportRepository _sharedReports;
    private readonly IClock _clock;

    public ShareService(
        ICurrentUserSession session,
        IReportService reports,
        IShareCardRenderer renderer,
        ISharedReportRepository sharedReports,
        IClock clock)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _sharedReports = sharedReports ?? throw new ArgumentNullException(nameof(sharedReports));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ShareCardData> BuildCardDataAsync(
        ReportPeriod period, DateOnly? referenceDate = null, CancellationToken cancellationToken = default)
    {
        var user = EnsurePengguna();
        var (_, card) = await PrepareAsync(user, period, referenceDate, cancellationToken).ConfigureAwait(false);
        return card;
    }

    public async Task<string> ShareAsync(
        ReportPeriod period, string outputFolder, DateOnly? referenceDate = null, CancellationToken cancellationToken = default)
    {
        var user = EnsurePengguna();
        var folder = ResolveFolder(outputFolder);
        var (report, card) = await PrepareAsync(user, period, referenceDate, cancellationToken).ConfigureAwait(false);

        var now = _clock.UtcNow;
        CreateFolder(folder);
        var outputPath = NextFreePath(folder, period, now);
        if (outputPath.Length > SharedReport.MaxFileGambarLength)
        {
            throw new ValidationException(OutputPathTooLongMessage, OutputFolderField);
        }

        var renderedPath = await _renderer.RenderAsync(card, outputPath, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(renderedPath))
        {
            throw new InvalidOperationException("Renderer kartu tidak mengembalikan path file.");
        }

        try
        {
            await _sharedReports.InsertAsync(
                new SharedReport
                {
                    UserId = user.UserId,
                    PeriodeMulai = report.PeriodeMulai,
                    PeriodeSelesai = report.PeriodeSelesai,
                    JumlahPaket = report.JumlahPaket,
                    TotalCo2eKg = report.TotalCo2eKg,
                    FileGambar = renderedPath,
                    CreatedAt = now,
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            TryDelete(renderedPath);
            throw;
        }

        return renderedPath;
    }

    /// <summary>Isi kartu dari laporan; laporan kosong ditolak.</summary>
    private async Task<(ReportDto Report, ShareCardData Card)> PrepareAsync(
        UserDto user, ReportPeriod period, DateOnly? referenceDate, CancellationToken cancellationToken)
    {
        var report = await _reports.GetReportAsync(period, referenceDate, cancellationToken).ConfigureAwait(false);
        if (report.IsEmpty)
        {
            throw new ValidationException(EmptyReportMessage);
        }

        var card = new ShareCardData(
            user.Nama,
            report.Periode,
            PeriodLabelRule.Create(report.Periode, report.TanggalMulai, report.TanggalSelesai),
            report.JumlahPaket,
            report.TotalCo2eKg,
            report.KonversiSetara.FirstOrDefault(),
            report.Hutan.NamaHutan,
            report.Hutan.Status,
            report.Hutan.Kesehatan,
            ShareCardData.DefaultKalimatAjakan);

        return (report, card);
    }

    private UserDto EnsurePengguna()
    {
        var user = _session.CurrentUser ?? throw new UnauthorizedException(ReportService.NotLoggedInMessage);
        if (user.Role != UserRole.Pengguna)
        {
            throw new UnauthorizedException(ReportService.NotPenggunaMessage);
        }

        return user;
    }

    private static string ResolveFolder(string? outputFolder)
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            throw new ValidationException(OutputFolderRequiredMessage, OutputFolderField);
        }

        try
        {
            return Path.GetFullPath(outputFolder.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ValidationException(OutputFolderInvalidMessage, OutputFolderField);
        }
    }

    private static void CreateFolder(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new ValidationException(OutputFolderUnavailableMessage, OutputFolderField);
        }
    }

    /// <summary>Nama file pertama yang belum dipakai di <paramref name="folder"/> (tanpa akhiran, lalu -1, -2, ...).</summary>
    private static string NextFreePath(string folder, ReportPeriod period, DateTimeOffset now)
    {
        for (var i = 0; i < MaxFileNameAttempts; i++)
        {
            var path = Path.Combine(folder, ShareFileNameRule.Create(period, now, i));
            if (!File.Exists(path))
            {
                return path;
            }
        }

        throw new InvalidOperationException("Tidak menemukan nama file kartu yang belum dipakai.");
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Pembersihan sebisanya; exception asli tetap dilempar ulang oleh pemanggil.
        }
    }
}
