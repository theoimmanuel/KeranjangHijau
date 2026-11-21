using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Tests.Fakes;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace KeranjangHijau.Tests.Services;

public class ForestServiceTests
{
    private const int UserId = 10;

    private readonly InMemoryForestRepository _forests = new();
    private readonly InMemoryForestLogRepository _logs = new();
    private readonly FakeTransactionRunner _runner = new();
    private readonly FakeClock _clock = new();
    private readonly FakeDbTransaction _transaction = new();
    private readonly ForestOptions _options = new();

    private ForestService CreateService() => new(_forests, _logs, _runner, _clock, MsOptions.Create(_options));

    /// <summary>Hutan dengan <c>updated_at</c> = <paramref name="updatedAt"/> (default: sekarang → tanpa pemulihan).</summary>
    private async Task<Forest> AddForestAsync(decimal kesehatan = 100m, DateTimeOffset? updatedAt = null, int pohon = 100)
    {
        await _forests.InsertAsync(new Forest
        {
            UserId = UserId,
            NamaHutan = "Hutan Budi",
            JumlahPohonAwal = pohon,
            Kesehatan = kesehatan,
            Status = KeranjangHijau.Core.Rules.ForestStatusRule.GetStatus(kesehatan).ToDbValue(),
            UpdatedAt = updatedAt ?? _clock.UtcNow,
        });
        return _forests.Forests.Single();
    }

    // =================================================================== ApplyEmissionAsync (M5.1)

    [Fact]
    public async Task ApplyEmission_ReducesHealth_UpdatesStatusAndTime()
    {
        var forest = await AddForestAsync(100m);

        var snapshot = await CreateService().ApplyEmissionAsync(UserId, 7, "JNE0000000001", 2.5m, _transaction);

        // 2,5 kg = 25 × 100 g → −25 poin
        Assert.Equal(new ForestSnapshotDto(75m, ForestStatus.Sehat, -25m), snapshot);
        Assert.Equal(75m, forest.Kesehatan);
        Assert.Equal("Sehat", forest.Status);
        Assert.Equal(_clock.UtcNow, forest.UpdatedAt);
    }

    [Fact]
    public async Task ApplyEmission_WritesLog_WithRecordIdAndDescription()
    {
        var forest = await AddForestAsync(100m);

        await CreateService().ApplyEmissionAsync(UserId, 7, "JNE0000000001", 0.123m, _transaction);

        var log = Assert.Single(_logs.Logs);
        Assert.Equal(forest.ForestId, log.ForestId);
        Assert.Equal(7, log.RecordId);
        Assert.Equal(-1.23m, log.Perubahan);
        Assert.Equal(98.77m, log.KesehatanSetelah);
        Assert.Equal("Emisi paket JNE0000000001", log.Keterangan);
        Assert.Equal(_clock.UtcNow, log.CreatedAt);
    }

    [Fact]
    public async Task ApplyEmission_ReadsWithRowLock_AndUsesGivenTransaction()
    {
        await AddForestAsync(90m);

        await CreateService().ApplyEmissionAsync(UserId, 7, "JNE0000000001", 1m, _transaction);

        Assert.Same(_transaction, Assert.Single(_forests.ForUpdateTransactions));
        Assert.Empty(_forests.GetTransactions); // tidak membaca tanpa kunci
        Assert.All(_forests.UpdateTransactions, t => Assert.Same(_transaction, t));
        Assert.Same(_transaction, Assert.Single(_logs.InsertTransactions));
        Assert.Equal(0, _runner.Commits); // memakai transaksi pemanggil, bukan transaksi baru
    }

    [Fact]
    public async Task ApplyEmission_HealthNeverBelowZero()
    {
        var forest = await AddForestAsync(15m);

        var snapshot = await CreateService().ApplyEmissionAsync(UserId, 7, "JNE0000000001", 5m, _transaction);

        Assert.Equal(new ForestSnapshotDto(0m, ForestStatus.Kritis, -15m), snapshot);
        Assert.Equal(0m, forest.Kesehatan);
        var log = Assert.Single(_logs.Logs);
        Assert.Equal(0m, log.KesehatanSetelah);
        Assert.Equal(-15m, log.Perubahan); // perubahan yang benar-benar terjadi
    }

    [Fact]
    public async Task ApplyEmission_UsesConfiguredPoints()
    {
        _options.PointsPer100GramCo2e = 2m;
        await AddForestAsync(100m);

        var snapshot = await CreateService().ApplyEmissionAsync(UserId, 7, "JNE0000000001", 1m, _transaction);

        Assert.Equal(new ForestSnapshotDto(80m, ForestStatus.Lestari, -20m), snapshot);
    }

    [Fact]
    public async Task ApplyEmission_TwoPackages_Accumulate()
    {
        var forest = await AddForestAsync(100m);
        var service = CreateService();

        await service.ApplyEmissionAsync(UserId, 7, "JNE0000000001", 3m, _transaction);
        var second = await service.ApplyEmissionAsync(UserId, 8, "JNE0000000002", 4.5m, _transaction);

        Assert.Equal(new ForestSnapshotDto(25m, ForestStatus.Terancam, -45m), second);
        Assert.Equal(25m, forest.Kesehatan);
        Assert.Equal([70m, 25m], _logs.Logs.Select(l => l.KesehatanSetelah));
        Assert.Equal([7, 8], _logs.Logs.Select(l => l.RecordId!.Value));
    }

    [Fact]
    public async Task ApplyEmission_ForestMissing_ThrowsNotFound_AndWritesNothing()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().ApplyEmissionAsync(UserId, 7, "JNE0000000001", 1m, _transaction));

        Assert.Equal(ForestService.ForestNotFoundMessage, ex.Message);
        Assert.Empty(_logs.Logs);
    }

    [Fact]
    public async Task ApplyEmission_LogFailure_Propagates()
    {
        await AddForestAsync();
        _logs.FailOnInsert = new InvalidOperationException("gagal simpan");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().ApplyEmissionAsync(UserId, 7, "JNE0000000001", 1m, _transaction));
    }

    [Fact]
    public async Task ApplyEmission_InvalidArguments_Throw()
    {
        await AddForestAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.ApplyEmissionAsync(UserId, 7, "JNE0000000001", -1m, _transaction));
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ApplyEmissionAsync(UserId, 7, " ", 1m, _transaction));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.ApplyEmissionAsync(UserId, 7, "JNE0000000001", 1m, null!));
        Assert.Empty(_logs.Logs);
    }

    // =================================================================== Pemulihan lalu emisi (M5.2)

    [Fact]
    public async Task ApplyEmission_AfterIdleDays_RecoversFirst_ThenReduces_SameDay()
    {
        var forest = await AddForestAsync(50m, _clock.UtcNow.AddDays(-3).AddHours(-6));

        var snapshot = await CreateService().ApplyEmissionAsync(UserId, 7, "JNE0000000001", 1m, _transaction);

        // 50 + 3 (pemulihan) = 53, lalu −10 (emisi) = 43
        Assert.Equal(new ForestSnapshotDto(43m, ForestStatus.Menipis, -10m), snapshot);
        Assert.Equal(43m, forest.Kesehatan);
        Assert.Equal("Menipis", forest.Status);
        Assert.Equal(_clock.UtcNow, forest.UpdatedAt); // emisi memulai hitungan pemulihan dari awal

        Assert.Collection(
            _logs.Logs,
            l =>
            {
                Assert.Null(l.RecordId);
                Assert.Equal((3m, 53m, "Pemulihan alami"), (l.Perubahan, l.KesehatanSetelah, l.Keterangan));
            },
            l =>
            {
                Assert.Equal(7, l.RecordId);
                Assert.Equal((-10m, 43m, "Emisi paket JNE0000000001"), (l.Perubahan, l.KesehatanSetelah, l.Keterangan));
            });

        // Riwayat: emisi (log_id lebih besar, waktu sama) tampil lebih dulu.
        var history = await CreateService().GetForestHistoryAsync(UserId);
        Assert.Equal([7, (int?)null], history.Select(h => h.RecordId));
    }

    [Fact]
    public async Task ApplyEmission_ThenRecoveryLaterSameDay_NoRecovery()
    {
        var forest = await AddForestAsync(80m);
        var service = CreateService();

        await service.ApplyEmissionAsync(UserId, 7, "JNE0000000001", 1m, _transaction);
        _clock.Advance(TimeSpan.FromHours(23));
        var snapshot = await service.ApplyRecoveryAsync(UserId, _transaction);

        Assert.Equal(new ForestSnapshotDto(70m, ForestStatus.Sehat, 0m), snapshot);
        Assert.Single(_logs.Logs);
        Assert.Equal(70m, forest.Kesehatan);
    }

    // =================================================================== ApplyRecoveryAsync (M5.2)

    [Fact]
    public async Task ApplyRecovery_ZeroFullDays_NoChange_NoLog_NoWrite()
    {
        var updatedAt = _clock.UtcNow.AddHours(-23).AddMinutes(-59);
        var forest = await AddForestAsync(50m, updatedAt);

        var snapshot = await CreateService().ApplyRecoveryAsync(UserId, _transaction);

        Assert.Equal(new ForestSnapshotDto(50m, ForestStatus.Menipis, 0m), snapshot);
        Assert.Empty(_logs.Logs);
        Assert.Empty(_forests.UpdateTransactions);
        Assert.Equal(updatedAt, forest.UpdatedAt);
    }

    [Fact]
    public async Task ApplyRecovery_OneFullDay_AddsOnePoint_AndWritesRecoveryLog()
    {
        var updatedAt = _clock.UtcNow.AddDays(-1);
        var forest = await AddForestAsync(50m, updatedAt);

        var snapshot = await CreateService().ApplyRecoveryAsync(UserId, _transaction);

        Assert.Equal(new ForestSnapshotDto(51m, ForestStatus.Menipis, 1m), snapshot);
        Assert.Equal(51m, forest.Kesehatan);
        Assert.Equal(_clock.UtcNow, forest.UpdatedAt); // tepat 1 hari → updated_at + 1 hari = sekarang

        var log = Assert.Single(_logs.Logs);
        Assert.Equal(forest.ForestId, log.ForestId);
        Assert.Null(log.RecordId);
        Assert.Equal(1m, log.Perubahan);
        Assert.Equal(51m, log.KesehatanSetelah);
        Assert.Equal(ForestService.RecoveryLogDescription, log.Keterangan);
        Assert.Equal("Pemulihan alami", log.Keterangan);
        Assert.Equal(_clock.UtcNow, log.CreatedAt);
        Assert.Same(_transaction, Assert.Single(_logs.InsertTransactions));
        Assert.Same(_transaction, Assert.Single(_forests.ForUpdateTransactions));
    }

    [Fact]
    public async Task ApplyRecovery_ThreeAndHalfDays_AddsThreePoints_AndKeepsHalfDay()
    {
        var updatedAt = _clock.UtcNow.AddDays(-3.5);
        var forest = await AddForestAsync(50m, updatedAt);
        var service = CreateService();

        var first = await service.ApplyRecoveryAsync(UserId, _transaction);

        Assert.Equal(53m, first.Kesehatan);
        Assert.Equal(3m, first.Perubahan);
        Assert.Equal(updatedAt.AddDays(3), forest.UpdatedAt); // dimajukan 3 hari, bukan ke sekarang
        Assert.Equal(_clock.UtcNow.AddHours(-12), forest.UpdatedAt);

        // Sisa 0,5 hari tetap terhitung: 12 jam kemudian sudah genap 1 hari lagi.
        _clock.Advance(TimeSpan.FromHours(12));
        var second = await service.ApplyRecoveryAsync(UserId, _transaction);

        Assert.Equal(new ForestSnapshotDto(54m, ForestStatus.Menipis, 1m), second);
        Assert.Equal(_clock.UtcNow, forest.UpdatedAt);
        Assert.Equal([3m, 1m], _logs.Logs.Select(l => l.Perubahan));
    }

    [Fact]
    public async Task ApplyRecovery_AlreadyFull_SetsUpdatedAtToNow_WithoutLog()
    {
        var forest = await AddForestAsync(100m, _clock.UtcNow.AddDays(-10).AddHours(-5));

        var snapshot = await CreateService().ApplyRecoveryAsync(UserId, _transaction);

        Assert.Equal(new ForestSnapshotDto(100m, ForestStatus.Lestari, 0m), snapshot);
        Assert.Equal(100m, forest.Kesehatan);
        Assert.Equal(_clock.UtcNow, forest.UpdatedAt);
        Assert.Empty(_logs.Logs);
    }

    [Fact]
    public async Task ApplyRecovery_StopsAt100_AndLogsActualIncrease()
    {
        var forest = await AddForestAsync(98.5m, _clock.UtcNow.AddDays(-5));

        var snapshot = await CreateService().ApplyRecoveryAsync(UserId, _transaction);

        Assert.Equal(new ForestSnapshotDto(100m, ForestStatus.Lestari, 1.5m), snapshot);
        Assert.Equal(100m, forest.Kesehatan);
        Assert.Equal(1.5m, Assert.Single(_logs.Logs).Perubahan);
    }

    [Fact]
    public async Task ApplyRecovery_UpdatesStatus_WhenCrossingBoundary()
    {
        var forest = await AddForestAsync(79m, _clock.UtcNow.AddDays(-2));

        await CreateService().ApplyRecoveryAsync(UserId, _transaction);

        Assert.Equal(81m, forest.Kesehatan);
        Assert.Equal("Lestari", forest.Status);
    }

    [Fact]
    public async Task ApplyRecovery_UsesConfiguredPointsPerDay()
    {
        _options.RecoveryPointsPerDay = 2.5m;
        await AddForestAsync(10m, _clock.UtcNow.AddDays(-4));

        var snapshot = await CreateService().ApplyRecoveryAsync(UserId, _transaction);

        Assert.Equal(new ForestSnapshotDto(20m, ForestStatus.Terancam, 10m), snapshot);
    }

    [Fact]
    public async Task ApplyRecovery_ForestMissing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().ApplyRecoveryAsync(UserId, _transaction));
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateService().ApplyRecoveryAsync(UserId, null!));
    }

    // =================================================================== GetForestAsync (M5.2)

    [Fact]
    public async Task GetForest_RunsRecoveryInOwnTransaction_ThenReturnsDto()
    {
        await AddForestAsync(60.4m, _clock.UtcNow.AddDays(-2).AddHours(-1), pohon: 150);

        var dto = await CreateService().GetForestAsync(UserId);

        // 60,4 + 2 = 62,4 → 150 × 62,4 / 100 = 93,6 → 94 pohon
        Assert.Equal(new ForestDto("Hutan Budi", 62.4m, ForestStatus.Sehat, 150, 94), dto);
        Assert.Equal(1, _runner.Commits);
        Assert.Same(_runner.LastTransaction, Assert.Single(_forests.ForUpdateTransactions));
        Assert.Same(_runner.LastTransaction, Assert.Single(_logs.InsertTransactions));
        Assert.Equal("Pemulihan alami", Assert.Single(_logs.Logs).Keterangan);
    }

    [Fact]
    public async Task GetForest_NoFullDay_ReturnsCurrentState_WithoutLog()
    {
        await AddForestAsync(35m, _clock.UtcNow.AddHours(-5));

        var dto = await CreateService().GetForestAsync(UserId);

        Assert.Equal(new ForestDto("Hutan Budi", 35m, ForestStatus.Terancam, 100, 35), dto);
        Assert.Empty(_logs.Logs);
    }

    [Fact]
    public async Task GetForest_CalledTwice_DoesNotRecoverTwice()
    {
        await AddForestAsync(40m, _clock.UtcNow.AddDays(-1.5));
        var service = CreateService();

        var first = await service.GetForestAsync(UserId);
        var second = await service.GetForestAsync(UserId);

        Assert.Equal(41m, first.Kesehatan);
        Assert.Equal(41m, second.Kesehatan);
        Assert.Single(_logs.Logs);
    }

    [Fact]
    public async Task GetForest_ForestMissing_ThrowsNotFound_AndRollsBack()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetForestAsync(UserId));

        Assert.Equal(ForestService.ForestNotFoundMessage, ex.Message);
        Assert.Equal(1, _runner.Rollbacks);
    }

    // =================================================================== GetForestHistoryAsync (M5.2)

    [Fact]
    public async Task GetHistory_NewestFirst_DefaultLimit30()
    {
        var forest = await AddForestAsync(50m);
        for (var i = 1; i <= 35; i++)
        {
            await _logs.InsertAsync(new ForestLog
            {
                ForestId = forest.ForestId, RecordId = null, Perubahan = 1m, KesehatanSetelah = 50m,
                Keterangan = $"log {i}", CreatedAt = _clock.UtcNow.AddMinutes(i),
            });
        }

        var history = await CreateService().GetForestHistoryAsync(UserId);

        Assert.Equal(ForestService.DefaultHistoryLimit, history.Count);
        Assert.Equal("log 35", history[0].Keterangan);
        Assert.Equal("log 6", history[^1].Keterangan);
        Assert.All(history, h => Assert.True(h.IsPemulihan));
    }

    [Fact]
    public async Task GetHistory_RespectsLimit_AndMapsFields()
    {
        var forest = await AddForestAsync(100m);
        await _logs.InsertAsync(new ForestLog
        {
            ForestId = forest.ForestId, RecordId = 9, Perubahan = -2.5m, KesehatanSetelah = 97.5m,
            Keterangan = "Emisi paket JNE0000000009", CreatedAt = _clock.UtcNow,
        });
        await _logs.InsertAsync(new ForestLog
        {
            ForestId = forest.ForestId, RecordId = null, Perubahan = 1m, KesehatanSetelah = 98.5m,
            Keterangan = "Pemulihan alami", CreatedAt = _clock.UtcNow.AddDays(1),
        });

        var history = await CreateService().GetForestHistoryAsync(UserId, limit: 1);

        var item = Assert.Single(history);
        Assert.Equal(new ForestLogDto(2, null, 1m, 98.5m, "Pemulihan alami", _clock.UtcNow.AddDays(1)), item);
        Assert.True(item.IsPemulihan);
    }

    [Fact]
    public async Task GetHistory_IsReadOnly_DoesNotRunRecovery()
    {
        await AddForestAsync(50m, _clock.UtcNow.AddDays(-3));

        var history = await CreateService().GetForestHistoryAsync(UserId);

        Assert.Empty(history);
        Assert.Empty(_forests.UpdateTransactions);
        Assert.Equal(0, _runner.Commits);
    }

    [Fact]
    public async Task GetHistory_InvalidLimit_OrMissingForest_Throws()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetForestHistoryAsync(UserId));
        await AddForestAsync();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CreateService().GetForestHistoryAsync(UserId, 0));
    }

    // =================================================================== Konstruktor

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        var options = MsOptions.Create(_options);
        Assert.Throws<ArgumentNullException>(() => new ForestService(null!, _logs, _runner, _clock, options));
        Assert.Throws<ArgumentNullException>(() => new ForestService(_forests, null!, _runner, _clock, options));
        Assert.Throws<ArgumentNullException>(() => new ForestService(_forests, _logs, null!, _clock, options));
        Assert.Throws<ArgumentNullException>(() => new ForestService(_forests, _logs, _runner, null!, options));
        Assert.Throws<ArgumentNullException>(() => new ForestService(_forests, _logs, _runner, _clock, null!));
    }
}
