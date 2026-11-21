using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>Query laporan (M6.1) ke database sungguhan. Semua perubahan di-rollback setelah test.</summary>
public class ReportRepositoryTests : RepositoryTestBase
{
    private IReportRepository Reports => Provider.GetRequiredService<IReportRepository>();

    private static DateTimeOffset Utc(int y, int mo, int d, int h = 0, int mi = 0, int s = 0) =>
        new(y, mo, d, h, mi, s, TimeSpan.Zero);

    private Task<int> NewUserAsync() => Users.InsertAsync(NewUser(), Transaction);

    /// <summary>Menyimpan shipment + record (+ nilai setara opsional, urut <c>equivalence_id</c>).</summary>
    private async Task<(int ShipmentId, int RecordId)> AddPackageAsync(
        int userId,
        DateTimeOffset createdAt,
        decimal totalCo2eKg,
        string kurir = "jne",
        string asal = "Jakarta",
        string tujuan = "Medan",
        TransportMode moda = TransportMode.Pesawat,
        params decimal[] nilaiSetara)
    {
        var courier = await Couriers.GetByKodeAsync(kurir, Transaction);
        var origin = await Cities.GetByNameAsync(asal, Transaction);
        var dest = await Cities.GetByNameAsync(tujuan, Transaction);
        var factor = await EmissionFactors.GetByModeAsync(moda, Transaction);

        var shipmentId = await Shipments.InsertAsync(
            new Shipment
            {
                UserId = userId,
                CourierId = courier!.CourierId,
                NomorResi = $"TEST{Random.Shared.NextInt64(1_000_000_000, 9_999_999_999)}",
                OriginCityId = origin!.CityId,
                DestCityId = dest!.CityId,
                Layanan = "REG",
                BeratKg = 1.5m,
                SumberData = ShipmentDataSource.Mock,
                CreatedAt = createdAt,
            },
            Transaction);

        var recordId = await EmissionRecords.InsertAsync(
            new EmissionRecord
            {
                ShipmentId = shipmentId,
                FactorId = factor!.FactorId,
                JarakKm = 1234.56m,
                TotalCo2eKg = totalCo2eKg,
                Metode = EmissionMethod.RumusLokal,
                CalculatedAt = createdAt,
            },
            Transaction);

        if (nilaiSetara.Length > 0)
        {
            var activities = await Equivalences.GetAllAsync(Transaction);
            await Equivalences.InsertRecordEquivalencesAsync(
                activities.Zip(nilaiSetara, (a, v) => new RecordEquivalence
                {
                    RecordId = recordId,
                    EquivalenceId = a.EquivalenceId,
                    NilaiSetara = v,
                }).ToList(),
                Transaction);
        }

        return (shipmentId, recordId);
    }

    [DatabaseFact]
    public async Task GetShipments_FiltersOwnerAndHalfOpenRange_NewestFirst()
    {
        var userId = await NewUserAsync();
        var otherId = await NewUserAsync();
        var range = PeriodRangeRule.Calculate(ReportPeriod.Mingguan, new DateOnly(2026, 3, 4)); // 2–8 Maret 2026

        await AddPackageAsync(userId, range.MulaiUtc!.Value.AddSeconds(-1), 1m);   // Minggu 23.59.59 WIB minggu lalu
        var atStart = await AddPackageAsync(userId, range.MulaiUtc.Value, 2m);     // Senin 00.00 WIB
        var middle = await AddPackageAsync(userId, Utc(2026, 3, 4, 5), 3m);
        var beforeEnd = await AddPackageAsync(userId, range.SelesaiUtc.AddSeconds(-1), 4m);
        await AddPackageAsync(userId, range.SelesaiUtc, 5m);                        // Senin berikutnya 00.00 WIB
        await AddPackageAsync(otherId, Utc(2026, 3, 4, 5), 6m);                     // milik pengguna lain

        var paket = await Reports.GetShipmentsAsync(userId, range.MulaiUtc, range.SelesaiUtc, Transaction);

        Assert.Equal([beforeEnd.ShipmentId, middle.ShipmentId, atStart.ShipmentId], paket.Select(p => p.ShipmentId));
        Assert.Equal([4m, 3m, 2m], paket.Select(p => p.TotalCo2eKg));
        Assert.Equal(range.MulaiUtc.Value, paket[^1].CreatedAt);
    }

    [DatabaseFact]
    public async Task GetShipments_JoinsCourierCitiesFactorAndRecord()
    {
        var userId = await NewUserAsync();
        var created = Utc(2026, 3, 4, 5, 6, 7);
        var package = await AddPackageAsync(userId, created, 0.1234m, "sicepat", "Surabaya", "Makassar", TransportMode.Kapal);
        var courier = await Couriers.GetByKodeAsync("sicepat", Transaction);

        var item = Assert.Single(await Reports.GetShipmentsAsync(userId, null, Utc(2027, 1, 1), Transaction));

        Assert.Equal(package.ShipmentId, item.ShipmentId);
        Assert.Equal(package.RecordId, item.RecordId);
        Assert.StartsWith("TEST", item.NomorResi);
        Assert.Equal("sicepat", item.KodeKurir);
        Assert.Equal(courier!.NamaKurir, item.NamaKurir);
        Assert.Equal("Surabaya", item.KotaAsal);
        Assert.Equal("Makassar", item.KotaTujuan);
        Assert.Equal("REG", item.Layanan);
        Assert.Equal(1.5m, item.BeratKg);
        Assert.Equal(1234.56m, item.JarakKm);
        Assert.Equal(TransportMode.Kapal, item.ModaTransport);
        Assert.Equal(0.1234m, item.TotalCo2eKg);
        Assert.Equal(created, item.CreatedAt);
    }

    [DatabaseFact]
    public async Task GetShipments_NullFrom_IncludesAllOlderPackages()
    {
        var userId = await NewUserAsync();
        await AddPackageAsync(userId, Utc(2020, 1, 1), 1m);
        await AddPackageAsync(userId, Utc(2026, 3, 4), 2m);
        await AddPackageAsync(userId, Utc(2026, 3, 5), 3m);

        var paket = await Reports.GetShipmentsAsync(userId, null, Utc(2026, 3, 5), Transaction);

        Assert.Equal([2m, 1m], paket.Select(p => p.TotalCo2eKg));
    }

    [DatabaseFact]
    public async Task GetDailyTotals_GroupsByJakartaDate_AndSumsRecords()
    {
        var userId = await NewUserAsync();
        var otherId = await NewUserAsync();
        await AddPackageAsync(userId, Utc(2026, 3, 1, 16, 59, 59), 0.5m); // 1 Maret 23.59.59 WIB
        await AddPackageAsync(userId, Utc(2026, 3, 1, 17), 1.25m);        // 2 Maret 00.00 WIB (1 Maret di UTC)
        await AddPackageAsync(userId, Utc(2026, 3, 2, 10), 0.75m);        // 2 Maret WIB
        await AddPackageAsync(otherId, Utc(2026, 3, 2, 10), 9m);

        var daily = await Reports.GetDailyTotalsAsync(userId, Utc(2026, 2, 28, 17), Utc(2026, 3, 31, 17), Transaction);

        Assert.Collection(
            daily,
            d => Assert.Equal((new DateOnly(2026, 3, 1), 1, 0.5m), (d.Tanggal, d.JumlahPaket, d.TotalCo2eKg)),
            d => Assert.Equal((new DateOnly(2026, 3, 2), 2, 2m), (d.Tanggal, d.JumlahPaket, d.TotalCo2eKg)));
    }

    [DatabaseFact]
    public async Task GetEquivalenceTotals_SumsPerActivity_OnlyOwnDataInRange()
    {
        var userId = await NewUserAsync();
        var otherId = await NewUserAsync();
        var activities = await Equivalences.GetAllAsync(Transaction);
        Assert.True(activities.Count >= 2, "Seed equivalences belum dijalankan.");
        var from = Utc(2026, 3, 1, 17);
        var to = Utc(2026, 3, 8, 17);

        await AddPackageAsync(userId, Utc(2026, 3, 2), 1m, nilaiSetara: [14.29m, 5.88m]);
        await AddPackageAsync(userId, Utc(2026, 3, 3), 0.5m, nilaiSetara: [7.14m, 2.94m]);
        await AddPackageAsync(userId, Utc(2026, 3, 3), 0.2m);                                 // tanpa nilai setara
        await AddPackageAsync(userId, Utc(2026, 3, 9), 2m, nilaiSetara: [28.57m, 11.76m]);   // di luar rentang
        await AddPackageAsync(otherId, Utc(2026, 3, 2), 3m, nilaiSetara: [42.86m, 17.65m]);  // pengguna lain

        var totals = await Reports.GetEquivalenceTotalsAsync(userId, from, to, Transaction);

        Assert.Equal(activities.Select(a => a.EquivalenceId), totals.Select(t => t.Equivalence.EquivalenceId));
        Assert.Equal(activities.Select(a => a.Aktivitas), totals.Select(t => t.Equivalence.Aktivitas));
        Assert.Equal(21.43m, totals[0].NilaiSetara);
        Assert.Equal(8.82m, totals[1].NilaiSetara);
        Assert.All(totals.Skip(2), t => Assert.Equal(0m, t.NilaiSetara));
    }

    [DatabaseFact]
    public async Task EmptyRange_ReturnsEmptyListsAndZeroActivities()
    {
        var userId = await NewUserAsync();
        await AddPackageAsync(userId, Utc(2026, 1, 15), 1m, nilaiSetara: [14.29m, 5.88m]);
        var range = PeriodRangeRule.Calculate(ReportPeriod.Bulanan, new DateOnly(2026, 2, 10));

        var paket = await Reports.GetShipmentsAsync(userId, range.MulaiUtc, range.SelesaiUtc, Transaction);
        var daily = await Reports.GetDailyTotalsAsync(userId, range.MulaiUtc, range.SelesaiUtc, Transaction);
        var totals = await Reports.GetEquivalenceTotalsAsync(userId, range.MulaiUtc, range.SelesaiUtc, Transaction);

        Assert.Empty(paket);
        Assert.Empty(daily);
        Assert.Equal((await Equivalences.GetAllAsync(Transaction)).Count, totals.Count);
        Assert.All(totals, t => Assert.Equal(0m, t.NilaiSetara));
    }

    [DatabaseFact]
    public async Task FromAfterTo_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => Reports.GetShipmentsAsync(1, Utc(2026, 3, 2), Utc(2026, 3, 1), Transaction));
    }
}
