using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Integration;

/// <summary>Repository equivalences + record_equivalences. Semua perubahan di-rollback setelah test.</summary>
public class EquivalenceRepositoryTests : RepositoryTestBase
{
    private async Task<int> InsertRecordAsync()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var courier = await Couriers.GetByKodeAsync("jne", Transaction);
        var city = await Cities.GetByNameAsync("Jakarta", Transaction);
        var factor = await EmissionFactors.GetByModeAsync(TransportMode.Truk, Transaction);

        var shipmentId = await Shipments.InsertAsync(
            new Shipment
            {
                UserId = userId,
                CourierId = courier!.CourierId,
                NomorResi = "JNE1001000001",
                OriginCityId = city!.CityId,
                DestCityId = city.CityId,
                BeratKg = 1m,
                SumberData = ShipmentDataSource.Mock,
            },
            Transaction);

        return await EmissionRecords.InsertAsync(
            new EmissionRecord
            {
                ShipmentId = shipmentId,
                FactorId = factor!.FactorId,
                JarakKm = 10m,
                TotalCo2eKg = 0.001m,
                Metode = EmissionMethod.RumusLokal,
            },
            Transaction);
    }

    [DatabaseFact]
    public async Task GetAll_ReturnsSeededMotorAndMobil_OrderedById()
    {
        var all = await Equivalences.GetAllAsync(Transaction);

        Assert.Contains(all, e => e.Aktivitas == "Naik motor");
        Assert.Contains(all, e => e.Aktivitas == "Naik mobil");
        Assert.Equal(all.OrderBy(e => e.EquivalenceId).Select(e => e.EquivalenceId), all.Select(e => e.EquivalenceId));
        Assert.All(all, e =>
        {
            Assert.True(e.EquivalenceId > 0);
            Assert.True(e.KgCo2ePerSatuan > 0);
            Assert.False(string.IsNullOrWhiteSpace(e.Satuan));
            Assert.False(string.IsNullOrWhiteSpace(e.Sumber));
            Assert.Equal(e.Sumber.StartsWith(Equivalence.PlaceholderPrefix, StringComparison.OrdinalIgnoreCase), e.IsPlaceholder);
        });
    }

    [DatabaseFact]
    public async Task InsertRecordEquivalences_ThenGetByRecordId_RoundTrips()
    {
        var recordId = await InsertRecordAsync();
        var all = await Equivalences.GetAllAsync(Transaction);
        var rows = all.Select((e, i) => new RecordEquivalence
        {
            RecordId = recordId,
            EquivalenceId = e.EquivalenceId,
            NilaiSetara = 1.25m + i,
        }).Reverse().ToList();

        var inserted = await Equivalences.InsertRecordEquivalencesAsync(rows, Transaction);
        var values = await Equivalences.GetByRecordIdAsync(recordId, Transaction);

        Assert.Equal(all.Count, inserted);
        Assert.Equal(all.Select(e => e.EquivalenceId), values.Select(v => v.Equivalence.EquivalenceId));
        Assert.Equal(all.Select((_, i) => 1.25m + i), values.Select(v => v.NilaiSetara));
        Assert.Equal(all.Select(e => e.Aktivitas), values.Select(v => v.Equivalence.Aktivitas));
        Assert.Equal(all.Select(e => e.IsPlaceholder), values.Select(v => v.ToDto().IsPlaceholder));
    }

    [DatabaseFact]
    public async Task InsertRecordEquivalences_EmptyList_ReturnsZero()
    {
        Assert.Equal(0, await Equivalences.InsertRecordEquivalencesAsync([], Transaction));
    }

    [DatabaseFact]
    public async Task GetByRecordId_WithoutRows_ReturnsEmpty()
    {
        Assert.Empty(await Equivalences.GetByRecordIdAsync(-1, Transaction));
    }

    [DatabaseFact]
    public async Task InsertRecordEquivalences_NegativeValue_ThrowsBeforeDatabase()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Equivalences.InsertRecordEquivalencesAsync(
            [new RecordEquivalence { RecordId = 1, EquivalenceId = 1, NilaiSetara = -1m }], Transaction));
    }

    [DatabaseFact]
    public async Task InsertRecordEquivalences_UnknownRecord_ThrowsNotFound()
    {
        var equivalence = (await Equivalences.GetAllAsync(Transaction))[0];

        await Assert.ThrowsAsync<NotFoundException>(() => Equivalences.InsertRecordEquivalencesAsync(
            [new RecordEquivalence { RecordId = -1, EquivalenceId = equivalence.EquivalenceId, NilaiSetara = 1m }],
            Transaction));
    }

    [DatabaseFact]
    public async Task InsertRecordEquivalences_Duplicate_ThrowsInvalidOperation()
    {
        var recordId = await InsertRecordAsync();
        var equivalence = (await Equivalences.GetAllAsync(Transaction))[0];
        var row = new RecordEquivalence { RecordId = recordId, EquivalenceId = equivalence.EquivalenceId, NilaiSetara = 1m };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Equivalences.InsertRecordEquivalencesAsync([row, row], Transaction));
    }

    [DatabaseFact]
    public async Task GetAll_WorksWithoutTransaction()
    {
        Assert.NotEmpty(await Equivalences.GetAllAsync());
    }
}
