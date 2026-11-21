using System.Data;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Core.Services;

/// <summary>Implementasi <see cref="IForestService"/> (CLAUDE.md 7.7).</summary>
public sealed class ForestService : IForestService
{
    public const string ForestNotFoundMessage = "Hutan tidak ditemukan.";

    /// <summary>Keterangan log pemulihan alami (<c>record_id = NULL</c>).</summary>
    public const string RecoveryLogDescription = "Pemulihan alami";

    /// <summary>Jumlah riwayat default untuk <see cref="GetForestHistoryAsync"/>.</summary>
    public const int DefaultHistoryLimit = 30;

    private readonly IForestRepository _forests;
    private readonly IForestLogRepository _logs;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IClock _clock;
    private readonly IOptions<ForestOptions> _options;

    public ForestService(
        IForestRepository forests,
        IForestLogRepository logs,
        ITransactionRunner transactionRunner,
        IClock clock,
        IOptions<ForestOptions> options)
    {
        _forests = forests ?? throw new ArgumentNullException(nameof(forests));
        _logs = logs ?? throw new ArgumentNullException(nameof(logs));
        _transactionRunner = transactionRunner ?? throw new ArgumentNullException(nameof(transactionRunner));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Keterangan log untuk emisi satu paket.</summary>
    public static string EmissionLogDescription(string nomorResi) => $"Emisi paket {nomorResi}";

    public async Task<ForestSnapshotDto> ApplyEmissionAsync(
        int userId,
        int recordId,
        string nomorResi,
        decimal totalCo2eKg,
        IDbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomorResi);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCo2eKg);
        ArgumentNullException.ThrowIfNull(transaction);

        // Pemulihan yang tertunda diterapkan lebih dulu, baru penurunan karena emisi (7.7).
        var forest = await RecoverAsync(userId, transaction, cancellationToken).ConfigureAwait(false);

        var damage = ForestDamageRule.Apply(forest.Kesehatan, totalCo2eKg, _options.Value.PointsPer100GramCo2e);
        var status = ForestStatusRule.GetStatus(damage.KesehatanBaru);
        var now = _clock.UtcNow;

        // updated_at = sekarang: hitungan pemulihan dimulai lagi dari paket terakhir.
        await _forests.UpdateHealthAsync(
            forest.ForestId, damage.KesehatanBaru, status.ToDbValue(), now, transaction, cancellationToken).ConfigureAwait(false);

        await _logs.InsertAsync(
            new ForestLog
            {
                ForestId = forest.ForestId,
                RecordId = recordId,
                Perubahan = damage.Perubahan,
                KesehatanSetelah = damage.KesehatanBaru,
                Keterangan = EmissionLogDescription(nomorResi),
                CreatedAt = now,
            },
            transaction,
            cancellationToken).ConfigureAwait(false);

        return new ForestSnapshotDto(damage.KesehatanBaru, status, damage.Perubahan);
    }

    public async Task<ForestSnapshotDto> ApplyRecoveryAsync(
        int userId, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var forest = await RecoverAsync(userId, transaction, cancellationToken).ConfigureAwait(false);
        return new ForestSnapshotDto(forest.Kesehatan, ForestStatusRule.GetStatus(forest.Kesehatan), forest.Perubahan);
    }

    public async Task<ForestDto> GetForestAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Pemulihan menulis ke forests + forest_logs, jadi wajib di dalam transaksi.
        var forest = await _transactionRunner.ExecuteAsync(
            (tx, ct) => RecoverAsync(userId, tx, ct),
            cancellationToken).ConfigureAwait(false);

        return ForestDto.FromModel(forest.ToModel());
    }

    public async Task<IReadOnlyList<ForestLogDto>> GetForestHistoryAsync(
        int userId, int limit = DefaultHistoryLimit, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        var forest = await _forests.GetByUserIdAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(ForestNotFoundMessage);

        var logs = await _logs.GetByForestIdAsync(forest.ForestId, limit, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return logs.Select(ForestLogDto.FromModel).ToList();
    }

    /// <summary>
    /// Membaca hutan dengan kunci baris, menerapkan pemulihan, dan menyimpan perubahan. Model dari repository
    /// tidak diubah; hasilnya dikembalikan sebagai salinan.
    /// </summary>
    private async Task<RecoveredForest> RecoverAsync(
        int userId, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        var forest = await _forests.GetByUserIdForUpdateAsync(userId, transaction, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(ForestNotFoundMessage);

        var now = _clock.UtcNow;
        var recovery = ForestRecoveryRule.Calculate(
            forest.Kesehatan, forest.UpdatedAt, now, _options.Value.RecoveryPointsPerDay);

        if (recovery.UpdatedAtBaru != forest.UpdatedAt || recovery.HasHealthChange)
        {
            var status = ForestStatusRule.GetStatus(recovery.KesehatanBaru);
            await _forests.UpdateHealthAsync(
                forest.ForestId, recovery.KesehatanBaru, status.ToDbValue(), recovery.UpdatedAtBaru, transaction, cancellationToken)
                .ConfigureAwait(false);
        }

        if (recovery.HasHealthChange)
        {
            await _logs.InsertAsync(
                new ForestLog
                {
                    ForestId = forest.ForestId,
                    RecordId = null,
                    Perubahan = recovery.Perubahan,
                    KesehatanSetelah = recovery.KesehatanBaru,
                    Keterangan = RecoveryLogDescription,
                    CreatedAt = now,
                },
                transaction,
                cancellationToken).ConfigureAwait(false);
        }

        return new RecoveredForest(
            forest.ForestId,
            forest.UserId,
            forest.NamaHutan,
            forest.JumlahPohonAwal,
            recovery.KesehatanBaru,
            recovery.Perubahan,
            recovery.UpdatedAtBaru);
    }

    /// <summary>Kondisi hutan setelah pemulihan (salinan, bukan objek milik repository).</summary>
    private sealed record RecoveredForest(
        int ForestId,
        int UserId,
        string NamaHutan,
        int JumlahPohonAwal,
        decimal Kesehatan,
        decimal Perubahan,
        DateTimeOffset UpdatedAt)
    {
        public Forest ToModel() => new()
        {
            ForestId = ForestId,
            UserId = UserId,
            NamaHutan = NamaHutan,
            JumlahPohonAwal = JumlahPohonAwal,
            Kesehatan = Kesehatan,
            Status = ForestStatusRule.GetStatus(Kesehatan).ToDbValue(),
            UpdatedAt = UpdatedAt,
        };
    }
}
