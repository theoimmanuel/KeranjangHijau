using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Infrastructure.Data;

namespace KeranjangHijau.Tests.Integration;

/// <summary>Repository forest_logs. Semua perubahan di-rollback setelah test.</summary>
public class ForestLogRepositoryTests : RepositoryTestBase
{
    private static readonly DateTimeOffset T0 = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    private async Task<(int UserId, int ForestId)> InsertForestAsync()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var forestId = await Forests.InsertAsync(
            new Forest { UserId = userId, NamaHutan = "Hutan Test", JumlahPohonAwal = 100, Kesehatan = 100m, Status = "Lestari", UpdatedAt = T0 },
            Transaction);
        return (userId, forestId);
    }

    private async Task<int> InsertRecordAsync(int userId, string resi = "JNE1001000003")
    {
        var courier = await Couriers.GetByKodeAsync("jne", Transaction);
        var origin = await Cities.GetByNameAsync("Jakarta", Transaction);
        var dest = await Cities.GetByNameAsync("Medan", Transaction);
        var factor = await EmissionFactors.GetByModeAsync(TransportMode.Pesawat, Transaction);

        var shipmentId = await Shipments.InsertAsync(
            new Shipment
            {
                UserId = userId, CourierId = courier!.CourierId, NomorResi = resi, OriginCityId = origin!.CityId,
                DestCityId = dest!.CityId, Layanan = "YES", BeratKg = 0.8m, SumberData = ShipmentDataSource.Mock, CreatedAt = T0,
            },
            Transaction);

        return await EmissionRecords.InsertAsync(
            new EmissionRecord
            {
                ShipmentId = shipmentId, FactorId = factor!.FactorId, JarakKm = 1849.12m, TotalCo2eKg = 1.1834m,
                Metode = EmissionMethod.RumusLokal, CalculatedAt = T0,
            },
            Transaction);
    }

    private static ForestLog Log(int forestId, int? recordId, decimal perubahan, decimal setelah, DateTimeOffset at, string keterangan = "Emisi paket JNE1001000003") => new()
    {
        ForestId = forestId,
        RecordId = recordId,
        Perubahan = perubahan,
        KesehatanSetelah = setelah,
        Keterangan = keterangan,
        CreatedAt = at,
    };

    [DatabaseFact]
    public async Task Insert_EmissionLog_ThenGetByForestId_ReturnsSameLog()
    {
        var (userId, forestId) = await InsertForestAsync();
        var recordId = await InsertRecordAsync(userId);

        var logId = await ForestLogs.InsertAsync(Log(forestId, recordId, -11.83m, 88.17m, T0), Transaction);
        var log = Assert.Single(await ForestLogs.GetByForestIdAsync(forestId, 10, Transaction));

        Assert.True(logId > 0);
        Assert.Equal(logId, log.LogId);
        Assert.Equal(forestId, log.ForestId);
        Assert.Equal(recordId, log.RecordId);
        Assert.Equal(-11.83m, log.Perubahan);
        Assert.Equal(88.17m, log.KesehatanSetelah);
        Assert.Equal("Emisi paket JNE1001000003", log.Keterangan);
        Assert.Equal(T0, log.CreatedAt);
    }

    [DatabaseFact]
    public async Task Insert_RecoveryLog_WithNullRecord_IsAllowedManyTimes()
    {
        var (_, forestId) = await InsertForestAsync();

        await ForestLogs.InsertAsync(Log(forestId, null, 1m, 90m, T0, "Pemulihan alami"), Transaction);
        await ForestLogs.InsertAsync(Log(forestId, null, 2m, 92m, T0.AddDays(2), "Pemulihan alami"), Transaction);

        var logs = await ForestLogs.GetByForestIdAsync(forestId, 10, Transaction);
        Assert.Equal(2, logs.Count);
        Assert.All(logs, l => Assert.Null(l.RecordId));
    }

    [DatabaseFact]
    public async Task GetByForestId_NewestFirst_AndRespectsLimit()
    {
        var (_, forestId) = await InsertForestAsync();
        await ForestLogs.InsertAsync(Log(forestId, null, 1m, 91m, T0, "satu"), Transaction);
        await ForestLogs.InsertAsync(Log(forestId, null, 1m, 93m, T0.AddDays(2), "tiga"), Transaction);
        await ForestLogs.InsertAsync(Log(forestId, null, 1m, 92m, T0.AddDays(1), "dua"), Transaction);
        await ForestLogs.InsertAsync(Log(forestId, null, 1m, 94m, T0.AddDays(2), "empat"), Transaction); // waktu sama → log_id lebih besar dulu

        var all = await ForestLogs.GetByForestIdAsync(forestId, 10, Transaction);
        var limited = await ForestLogs.GetByForestIdAsync(forestId, 2, Transaction);

        Assert.Equal(["empat", "tiga", "dua", "satu"], all.Select(l => l.Keterangan));
        Assert.Equal(["empat", "tiga"], limited.Select(l => l.Keterangan));
    }

    [DatabaseFact]
    public async Task GetByForestId_OtherForestOrNoLogs_ReturnsEmpty()
    {
        var (_, forestA) = await InsertForestAsync();
        var (_, forestB) = await InsertForestAsync();
        await ForestLogs.InsertAsync(Log(forestA, null, 1m, 90m, T0, "Pemulihan alami"), Transaction);

        Assert.Empty(await ForestLogs.GetByForestIdAsync(forestB, 10, Transaction));
    }

    [DatabaseFact]
    public async Task Insert_SameRecordTwice_ThrowsInvalidOperation()
    {
        var (userId, forestId) = await InsertForestAsync();
        var recordId = await InsertRecordAsync(userId);
        await ForestLogs.InsertAsync(Log(forestId, recordId, -1m, 99m, T0), Transaction);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ForestLogs.InsertAsync(Log(forestId, recordId, -1m, 98m, T0), Transaction));
    }

    [DatabaseFact]
    public async Task Insert_UnknownForest_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => ForestLogs.InsertAsync(Log(-1, null, 1m, 90m, T0, "Pemulihan alami"), Transaction));

        Assert.Equal(ForestLogRepository.ForestNotFoundMessage, ex.Message);
    }

    [DatabaseFact]
    public async Task Insert_UnknownRecord_ThrowsNotFound()
    {
        var (_, forestId) = await InsertForestAsync();

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => ForestLogs.InsertAsync(Log(forestId, -1, -1m, 99m, T0), Transaction));

        Assert.Equal(ForestLogRepository.RecordNotFoundMessage, ex.Message);
    }
}
