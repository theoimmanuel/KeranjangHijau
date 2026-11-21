using System.Data;
using Dapper;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="ISharedReportRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class SharedReportRepository : DapperRepositoryBase, ISharedReportRepository
{
    public const string UserNotFoundMessage = "Pengguna tidak ditemukan.";

    private readonly IClock _clock;

    public SharedReportRepository(IDbConnectionFactory connectionFactory, IClock clock)
        : base(connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public async Task<int> InsertAsync(
        SharedReport report, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentOutOfRangeException.ThrowIfNegative(report.JumlahPaket, nameof(report.JumlahPaket));
        ArgumentOutOfRangeException.ThrowIfNegative(report.TotalCo2eKg, nameof(report.TotalCo2eKg));
        if (report.PeriodeSelesai < report.PeriodeMulai)
        {
            throw new ArgumentException("Akhir periode tidak boleh sebelum awal periode.", nameof(report.PeriodeSelesai));
        }

        var fileGambar = string.IsNullOrWhiteSpace(report.FileGambar) ? null : report.FileGambar.Trim();
        if (fileGambar is { Length: > SharedReport.MaxFileGambarLength })
        {
            throw new ArgumentException(
                $"Path file gambar maksimal {SharedReport.MaxFileGambarLength} karakter.", nameof(report.FileGambar));
        }

        const string sql = """
            INSERT INTO shared_reports
                (user_id, periode_mulai, periode_selesai, jumlah_paket, total_co2e_kg, file_gambar, created_at)
            VALUES
                (@UserId, @PeriodeMulai, @PeriodeSelesai, @JumlahPaket, @TotalCo2eKg, @FileGambar, @CreatedAt)
            RETURNING share_id
            """;

        var parameters = new
        {
            report.UserId,
            PeriodeMulai = report.PeriodeMulai.ToUniversalTime(),
            PeriodeSelesai = report.PeriodeSelesai.ToUniversalTime(),
            report.JumlahPaket,
            report.TotalCo2eKg,
            FileGambar = fileGambar,
            CreatedAt = (report.CreatedAt == default ? _clock.UtcNow : report.CreatedAt).ToUniversalTime(),
        };

        try
        {
            return await RunAsync(
                transaction,
                conn => conn.ExecuteScalarAsync<int>(Command(sql, parameters, transaction, cancellationToken)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (PostgresErrors.IsForeignKeyViolation(ex, "fk_shared_reports_user"))
        {
            throw new NotFoundException(UserNotFoundMessage);
        }
    }

    public async Task<IReadOnlyList<SharedReport>> GetByUserIdAsync(
        int userId, int limit = 50, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        const string sql = """
            SELECT share_id, user_id, periode_mulai, periode_selesai, jumlah_paket, total_co2e_kg, file_gambar, created_at
            FROM shared_reports
            WHERE user_id = @UserId
            ORDER BY created_at DESC, share_id DESC
            LIMIT @Limit
            """;

        var rows = await RunAsync(
            transaction,
            conn => conn.QueryAsync<SharedReport>(Command(sql, new { UserId = userId, Limit = limit }, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return rows.ToList();
    }
}
