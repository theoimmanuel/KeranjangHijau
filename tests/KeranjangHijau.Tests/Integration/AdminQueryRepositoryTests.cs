using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// Query admin (M7.1) ke database sungguhan. Semua perubahan di-rollback setelah test.
/// Setiap test memberi nama/email pengguna token unik dan selalu memfilter dengan token itu,
/// supaya data lain di database tidak ikut terhitung.
/// </summary>
public class AdminQueryRepositoryTests : RepositoryTestBase
{
    private readonly string _token = Guid.NewGuid().ToString("N");

    private IAdminQueryRepository Admin => Provider.GetRequiredService<IAdminQueryRepository>();

    private static DateTimeOffset Utc(int y, int mo, int d, int h = 0, int mi = 0, int s = 0) =>
        new(y, mo, d, h, mi, s, TimeSpan.Zero);

    private AdminRecordQuery Query(
        AdminRecordFilter? filter = null, string? keyword = null) =>
        AdminFilterRule.Normalize((filter ?? new AdminRecordFilter()) with { KataKunci = keyword ?? _token });

    private Task<int> NewUserAsync(string nama, string emailLocal) => Users.InsertAsync(
        new User
        {
            Nama = $"{nama} {_token}",
            Email = $"{emailLocal}.{_token}@keranjanghijau.test",
            PasswordHash = "$2a$04$abcdefghijklmnopqrstuuJ0fBk3Yc3D6Yb2c1tJz5w6XjFJ7Q8xa",
            CreatedAt = Utc(2026, 1, 2),
        },
        Transaction);

    private async Task<int> AddPackageAsync(
        int userId,
        DateTimeOffset calculatedAt,
        decimal totalCo2eKg,
        string kurir = "jne",
        string asal = "Jakarta",
        string tujuan = "Medan",
        TransportMode moda = TransportMode.Pesawat)
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
                NomorResi = $"ADM{Random.Shared.NextInt64(1_000_000_000, 9_999_999_999)}",
                OriginCityId = origin!.CityId,
                DestCityId = dest!.CityId,
                Layanan = "REG",
                BeratKg = 2.5m,
                SumberData = ShipmentDataSource.Mock,
                CreatedAt = calculatedAt,
            },
            Transaction);

        return await EmissionRecords.InsertAsync(
            new EmissionRecord
            {
                ShipmentId = shipmentId,
                FactorId = factor!.FactorId,
                JarakKm = 1500.25m,
                TotalCo2eKg = totalCo2eKg,
                Metode = EmissionMethod.RumusLokal,
                CalculatedAt = calculatedAt,
            },
            Transaction);
    }

    [DatabaseFact]
    public async Task GetRecords_JoinsAllTables()
    {
        var userId = await NewUserAsync("Siti", "siti");
        var calculated = Utc(2026, 3, 4, 5, 6, 7);
        var recordId = await AddPackageAsync(userId, calculated, 0.1234m, "sicepat", "Surabaya", "Makassar", TransportMode.Kapal);
        var courier = await Couriers.GetByKodeAsync("sicepat", Transaction);

        var item = Assert.Single(await Admin.GetRecordsAsync(Query(), Transaction));

        Assert.Equal(recordId, item.RecordId);
        Assert.Equal(userId, item.UserId);
        Assert.Equal($"Siti {_token}", item.NamaPengguna);
        Assert.Equal($"siti.{_token}@keranjanghijau.test", item.Email);
        Assert.StartsWith("ADM", item.NomorResi);
        Assert.Equal(("sicepat", courier!.NamaKurir), (item.KodeKurir, item.NamaKurir));
        Assert.Equal("Surabaya → Makassar", item.Rute);
        Assert.Equal(TransportMode.Kapal, item.ModaTransport);
        Assert.Equal((2.5m, 1500.25m, 0.1234m), (item.BeratKg, item.JarakKm, item.TotalCo2eKg));
        Assert.Equal(calculated, item.CalculatedAt);
    }

    [DatabaseFact]
    public async Task DateFilter_InclusiveJakartaDays()
    {
        var userId = await NewUserAsync("Budi", "budi");
        await AddPackageAsync(userId, Utc(2026, 3, 1, 16, 59, 59), 1m);         // 1 Maret 23.59.59 WIB
        var first = await AddPackageAsync(userId, Utc(2026, 3, 1, 17), 2m);    // 2 Maret 00.00 WIB
        var last = await AddPackageAsync(userId, Utc(2026, 3, 3, 16, 59, 59), 3m);
        await AddPackageAsync(userId, Utc(2026, 3, 3, 17), 4m);                 // 4 Maret 00.00 WIB

        var query = Query(new AdminRecordFilter { TanggalMulai = new DateOnly(2026, 3, 2), TanggalSelesai = new DateOnly(2026, 3, 3) });

        Assert.Equal([last, first], (await Admin.GetRecordsAsync(query, Transaction)).Select(r => r.RecordId));
        Assert.Equal(2, await Admin.CountRecordsAsync(query, Transaction));

        var fromOnly = Query(new AdminRecordFilter { TanggalMulai = new DateOnly(2026, 3, 3) });
        var toOnly = Query(new AdminRecordFilter { TanggalSelesai = new DateOnly(2026, 3, 1) });
        Assert.Equal(2, await Admin.CountRecordsAsync(fromOnly, Transaction));
        Assert.Equal(1, await Admin.CountRecordsAsync(toOnly, Transaction));
    }

    [DatabaseFact]
    public async Task CourierFilter_MatchesCode()
    {
        var userId = await NewUserAsync("Budi", "budi");
        var jnt = await AddPackageAsync(userId, Utc(2026, 3, 4), 1m, "jnt");
        await AddPackageAsync(userId, Utc(2026, 3, 5), 2m, "jne");

        var records = await Admin.GetRecordsAsync(Query(new AdminRecordFilter { KodeKurir = "JNT" }), Transaction);
        var unknown = await Admin.CountRecordsAsync(Query(new AdminRecordFilter { KodeKurir = "tidakada" }), Transaction);

        Assert.Equal([jnt], records.Select(r => r.RecordId));
        Assert.Equal(0, unknown);
    }

    [DatabaseFact]
    public async Task KeywordFilter_MatchesNameOrEmail_CaseInsensitive()
    {
        var siti = await NewUserAsync("Siti", "siti");
        var budi = await NewUserAsync("Budi", "budi");
        var sitiRecord = await AddPackageAsync(siti, Utc(2026, 3, 4), 1m);
        var budiRecord = await AddPackageAsync(budi, Utc(2026, 3, 5), 2m);

        var byName = await Admin.GetRecordsAsync(Query(keyword: $"SITI {_token.ToUpperInvariant()}"), Transaction);
        var byEmail = await Admin.GetRecordsAsync(Query(keyword: $"budi.{_token}@KeranjangHijau"), Transaction);
        var both = await Admin.GetRecordsAsync(Query(keyword: _token), Transaction);

        Assert.Equal([sitiRecord], byName.Select(r => r.RecordId));
        Assert.Equal([budiRecord], byEmail.Select(r => r.RecordId));
        Assert.Equal([budiRecord, sitiRecord], both.Select(r => r.RecordId));
    }

    [DatabaseFact]
    public async Task KeywordFilter_TreatsLikeWildcardsAsLiteralText()
    {
        var underscore = await NewUserAsync("A_b", "u");  // nama "A_b <token>"
        var letter = await NewUserAsync("Axb", "x");      // tanpa escape, "A_b" juga cocok dengan "Axb"
        var percent = await NewUserAsync("100%", "p");
        var plain = await NewUserAsync("1000", "q");
        var r1 = await AddPackageAsync(underscore, Utc(2026, 3, 4), 1m);
        await AddPackageAsync(letter, Utc(2026, 3, 4), 1m);
        var r2 = await AddPackageAsync(percent, Utc(2026, 3, 4), 1m);
        await AddPackageAsync(plain, Utc(2026, 3, 4), 1m);

        Assert.Equal([r1], (await Admin.GetRecordsAsync(Query(keyword: $"A_b {_token}"), Transaction)).Select(r => r.RecordId));
        Assert.Equal([r2], (await Admin.GetRecordsAsync(Query(keyword: $"100% {_token}"), Transaction)).Select(r => r.RecordId));
    }

    [DatabaseFact]
    public async Task Paging_LimitOffset_NewestFirst_AndCountIgnoresPaging()
    {
        var userId = await NewUserAsync("Budi", "budi");
        var ids = new List<int>();
        for (var i = 0; i < 7; i++)
        {
            ids.Add(await AddPackageAsync(userId, Utc(2026, 3, 1).AddHours(i), i));
        }

        ids.Reverse();
        var page1 = await Admin.GetRecordsAsync(Query(new AdminRecordFilter { Halaman = 1, UkuranHalaman = 3 }), Transaction);
        var page3 = await Admin.GetRecordsAsync(Query(new AdminRecordFilter { Halaman = 3, UkuranHalaman = 3 }), Transaction);
        var page4 = await Admin.GetRecordsAsync(Query(new AdminRecordFilter { Halaman = 4, UkuranHalaman = 3 }), Transaction);

        Assert.Equal(ids.Take(3), page1.Select(r => r.RecordId));
        Assert.Equal(ids.Skip(6), page3.Select(r => r.RecordId));
        Assert.Empty(page4);
        Assert.Equal(7, await Admin.CountRecordsAsync(Query(new AdminRecordFilter { Halaman = 3, UkuranHalaman = 3 }), Transaction));
    }

    [DatabaseFact]
    public async Task PageSizeAbove100_ReturnsAtMost100Rows()
    {
        var userId = await NewUserAsync("Budi", "budi");
        for (var i = 0; i < 105; i++)
        {
            await AddPackageAsync(userId, Utc(2026, 3, 1).AddMinutes(i), 0.01m);
        }

        var query = Query(new AdminRecordFilter { UkuranHalaman = 500 });

        Assert.Equal(100, (await Admin.GetRecordsAsync(query, Transaction)).Count);
        Assert.Equal(105, await Admin.CountRecordsAsync(query, Transaction));
    }

    [DatabaseFact]
    public async Task Totals_AndCourierTotals_FollowFilter()
    {
        var siti = await NewUserAsync("Siti", "siti");
        var budi = await NewUserAsync("Budi", "budi");
        await AddPackageAsync(siti, Utc(2026, 3, 4), 1.5m, "jne");
        await AddPackageAsync(siti, Utc(2026, 3, 5), 0.25m, "jnt");
        await AddPackageAsync(budi, Utc(2026, 3, 6), 2m, "jne");
        await AddPackageAsync(budi, Utc(2026, 4, 6), 9m, "jne"); // di luar rentang tanggal

        var march = new AdminRecordFilter { TanggalMulai = new DateOnly(2026, 3, 1), TanggalSelesai = new DateOnly(2026, 3, 31) };
        var totals = await Admin.GetTotalsAsync(Query(march), Transaction);
        var perKurir = await Admin.GetCourierTotalsAsync(Query(march), Transaction);
        var allCouriers = await Couriers.GetAllAsync(Transaction);

        Assert.Equal(new AdminTotals(2, 3, 3.75m), totals);
        Assert.Equal(allCouriers.OrderBy(c => c.CourierId).Select(c => c.KodeKurir), perKurir.Select(k => k.KodeKurir));
        var byCode = perKurir.ToDictionary(k => k.KodeKurir, k => (k.JumlahPaket, k.TotalCo2eKg));
        Assert.Equal((2, 3.5m), byCode["jne"]);
        Assert.Equal((1, 0.25m), byCode["jnt"]);
        Assert.All(perKurir.Where(k => k.KodeKurir is not ("jne" or "jnt")), k => Assert.Equal((0, 0m), (k.JumlahPaket, k.TotalCo2eKg)));

        var onlyJnt = await Admin.GetCourierTotalsAsync(Query(march with { KodeKurir = "jnt" }), Transaction);
        Assert.Equal("jnt", Assert.Single(onlyJnt).KodeKurir);
        Assert.Equal(new AdminTotals(1, 1, 0.25m), await Admin.GetTotalsAsync(Query(march with { KodeKurir = "jnt" }), Transaction));
    }

    [DatabaseFact]
    public async Task Totals_NoMatch_AreZero()
    {
        Assert.Equal(new AdminTotals(0, 0, 0m), await Admin.GetTotalsAsync(Query(), Transaction));
        Assert.Empty(await Admin.GetRecordsAsync(Query(), Transaction));
        Assert.Equal(0, await Admin.CountRecordsAsync(Query(), Transaction));
    }
}
