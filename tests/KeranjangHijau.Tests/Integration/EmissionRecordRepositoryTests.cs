using Dapper;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Integration;

/// <summary>Repository shipments + emission_records. Semua perubahan di-rollback setelah test.</summary>
public class EmissionRecordRepositoryTests : RepositoryTestBase
{
    private static readonly DateTimeOffset Time = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

    private async Task<Shipment> InsertShipmentAsync(ShipmentDataSource source = ShipmentDataSource.Mock, string? layanan = " REG ")
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var courier = await Couriers.GetByKodeAsync("jne", Transaction);
        var origin = await Cities.GetByNameAsync("Jakarta", Transaction);
        var dest = await Cities.GetByNameAsync("Medan", Transaction);

        var shipment = new Shipment
        {
            UserId = userId,
            CourierId = courier!.CourierId,
            NomorResi = "jne 1001 000003",
            OriginCityId = origin!.CityId,
            DestCityId = dest!.CityId,
            Layanan = layanan,
            BeratKg = 0.8m,
            SumberData = source,
            CreatedAt = Time,
        };
        shipment.ShipmentId = await Shipments.InsertAsync(shipment, Transaction);
        return shipment;
    }

    [DatabaseFact]
    public async Task InsertShipment_StoresNormalizedValues()
    {
        var shipment = await InsertShipmentAsync(ShipmentDataSource.Manual);

        var row = await Transaction.Connection!.QuerySingleAsync<(string NomorResi, string Layanan, decimal BeratKg, string SumberData, DateTime CreatedAt)>(
            "SELECT nomor_resi, layanan, berat_kg, sumber_data, created_at FROM shipments WHERE shipment_id = @Id",
            new { Id = shipment.ShipmentId },
            Transaction);

        Assert.True(shipment.ShipmentId > 0);
        Assert.Equal("JNE1001000003", row.NomorResi);
        Assert.Equal("REG", row.Layanan);
        Assert.Equal(0.8m, row.BeratKg);
        Assert.Equal("manual", row.SumberData);
        Assert.True(await Shipments.ExistsAsync(shipment.CourierId, "JNE1001000003", Transaction));
    }

    [DatabaseFact]
    public async Task InsertShipment_EmptyLayanan_IsStoredAsNull()
    {
        var shipment = await InsertShipmentAsync(layanan: "   ");

        var layanan = await Transaction.Connection!.ExecuteScalarAsync<string?>(
            "SELECT layanan FROM shipments WHERE shipment_id = @Id", new { Id = shipment.ShipmentId }, Transaction);

        Assert.Null(layanan);
    }

    [DatabaseFact]
    public async Task InsertRecord_ThenGetById_RoundTrips()
    {
        var shipment = await InsertShipmentAsync();
        var factor = await EmissionFactors.GetByModeAsync(TransportMode.Pesawat, Transaction);

        var recordId = await EmissionRecords.InsertAsync(
            new EmissionRecord
            {
                ShipmentId = shipment.ShipmentId,
                FactorId = factor!.FactorId,
                JarakKm = 1849.12m,
                TotalCo2eKg = 1.1834m,
                Metode = EmissionMethod.RumusLokal,
                CalculatedAt = Time,
            },
            Transaction);

        var record = await EmissionRecords.GetByIdAsync(recordId, Transaction);

        Assert.NotNull(record);
        Assert.Equal(recordId, record.RecordId);
        Assert.Equal(shipment.ShipmentId, record.ShipmentId);
        Assert.Equal(factor.FactorId, record.FactorId);
        Assert.Equal(1849.12m, record.JarakKm);
        Assert.Equal(1.1834m, record.TotalCo2eKg);
        Assert.Equal(EmissionMethod.RumusLokal, record.Metode);
        Assert.Equal(Time, record.CalculatedAt);
    }

    [DatabaseFact]
    public async Task GetById_Unknown_ReturnsNull()
    {
        Assert.Null(await EmissionRecords.GetByIdAsync(-1, Transaction));
    }

    [DatabaseFact]
    public async Task InsertShipment_UnknownUser_ThrowsNotFound()
    {
        var courier = await Couriers.GetByKodeAsync("jne", Transaction);
        var city = await Cities.GetByNameAsync("Jakarta", Transaction);

        await Assert.ThrowsAsync<NotFoundException>(() => Shipments.InsertAsync(
            new Shipment
            {
                UserId = -1,
                CourierId = courier!.CourierId,
                NomorResi = "JNE1001000003",
                OriginCityId = city!.CityId,
                DestCityId = city.CityId,
                BeratKg = 1m,
                SumberData = ShipmentDataSource.Mock,
            },
            Transaction));
    }
}
