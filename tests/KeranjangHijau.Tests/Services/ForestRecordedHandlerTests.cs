using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Tests.Fakes;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace KeranjangHijau.Tests.Services;

public class ForestRecordedHandlerTests
{
    private static readonly UserDto Pengguna = new(10, "Budi", "budi@contoh.id", UserRole.Pengguna, DateTimeOffset.UnixEpoch);

    private readonly InMemoryForestRepository _forests = new();
    private readonly InMemoryForestLogRepository _logs = new();
    private readonly FakeDbTransaction _transaction = new();

    private ForestRecordedHandler CreateHandler() =>
        new(new ForestService(_forests, _logs, new FakeTransactionRunner(), new FakeClock(), MsOptions.Create(new ForestOptions())));

    private static EmissionRecordedContext Context(int recordId = 7, decimal totalCo2eKg = 1.5m)
    {
        var shipment = new Shipment { ShipmentId = 3, UserId = Pengguna.UserId, NomorResi = "JNE0000000001" };
        var record = new EmissionRecord { RecordId = recordId, ShipmentId = 3, TotalCo2eKg = totalCo2eKg };
        var result = new EmissionResultDto(
            recordId, 3, "JNE0000000001", "jne", "JNE", "Jakarta", "Bandung", "REG", 1m,
            WeightSource.Api, ShipmentDataSource.Mock, 150m, TransportMode.Truk, totalCo2eKg,
            EmissionMethod.RumusLokal, DateTimeOffset.UnixEpoch)
        {
            KonversiSetara = [new EquivalenceDto("Aktivitas A", "km", 6m, null, true)],
        };
        return new EmissionRecordedContext(Pengguna, shipment, record, result);
    }

    private Task AddForestAsync(decimal kesehatan = 100m) => _forests.InsertAsync(new Forest
    {
        UserId = Pengguna.UserId, NamaHutan = "Hutan Budi", JumlahPohonAwal = 100, Kesehatan = kesehatan, Status = "Lestari",
        UpdatedAt = new FakeClock().UtcNow, // sama dengan jam service → tanpa pemulihan
    });

    [Fact]
    public async Task AppliesEmissionToUsersForest_AndAddsSnapshotToResult()
    {
        await AddForestAsync(100m);
        var context = Context(recordId: 7, totalCo2eKg: 1.5m);
        var original = context.Result;

        await CreateHandler().HandleAsync(context, _transaction);

        Assert.Equal(new ForestSnapshotDto(85m, ForestStatus.Lestari, -15m), context.Result.Hutan);
        Assert.Null(original.Hutan);
        Assert.Same(original.KonversiSetara, context.Result.KonversiSetara); // hasil handler sebelumnya tetap ada
        Assert.Equal(85m, _forests.Forests.Single().Kesehatan);

        var log = Assert.Single(_logs.Logs);
        Assert.Equal(7, log.RecordId);
        Assert.Equal("Emisi paket JNE0000000001", log.Keterangan);
        Assert.Same(_transaction, Assert.Single(_logs.InsertTransactions));
    }

    [Fact]
    public async Task ForestMissing_Propagates()
    {
        await Assert.ThrowsAsync<KeranjangHijau.Core.Exceptions.NotFoundException>(
            () => CreateHandler().HandleAsync(Context(), _transaction));
    }

    [Fact]
    public async Task NullArguments_Throw()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(null!, _transaction));
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(Context(), null!));
        Assert.Throws<ArgumentNullException>(() => new ForestRecordedHandler(null!));
    }
}
