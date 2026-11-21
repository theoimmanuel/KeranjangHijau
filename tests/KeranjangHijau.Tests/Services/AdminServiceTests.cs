using System.Reflection;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Tests.Fakes;

namespace KeranjangHijau.Tests.Services;

public class AdminServiceTests
{
    private readonly InMemoryCurrentUserSession _session = new();
    private readonly InMemoryAdminQueryRepository _queries = new();

    private AdminService CreateService() => new(_session, _queries);

    private void LoginAs(UserRole role) =>
        _session.SignIn(new UserDto(role == UserRole.Admin ? 1 : 10, "Nama", "x@test.id", role, DateTimeOffset.UnixEpoch));

    private static DateTimeOffset Utc(int y, int mo, int d, int h = 0, int mi = 0, int s = 0) =>
        new(y, mo, d, h, mi, s, TimeSpan.Zero);

    // =================================================================== Hak akses

    public static TheoryData<string> Methods => [nameof(AdminService.GetRecordsAsync), nameof(AdminService.GetSummaryAsync)];

    private Task Call(string method, AdminRecordFilter filter) => method switch
    {
        nameof(AdminService.GetRecordsAsync) => CreateService().GetRecordsAsync(filter),
        nameof(AdminService.GetSummaryAsync) => CreateService().GetSummaryAsync(filter),
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task RegularUser_IsRejected_WithoutQuerying(string method)
    {
        LoginAs(UserRole.Pengguna);
        _queries.Add(Utc(2026, 3, 4), 1m);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => Call(method, new AdminRecordFilter()));

        Assert.Equal("Hanya admin yang bisa membuka halaman ini.", ex.Message);
        Assert.Equal(AdminService.NotAdminMessage, ex.Message);
        Assert.Empty(_queries.Calls);
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task NotLoggedIn_IsRejected_WithoutQuerying(string method)
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => Call(method, new AdminRecordFilter()));

        Assert.Equal(AdminService.NotAdminMessage, ex.Message);
        Assert.Empty(_queries.Calls);
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task AdminLoggedOut_IsRejectedAgain(string method)
    {
        LoginAs(UserRole.Admin);
        await Call(method, new AdminRecordFilter());
        _session.SignOut();

        await Assert.ThrowsAsync<UnauthorizedException>(() => Call(method, new AdminRecordFilter()));
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task AuthorizationIsChecked_BeforeFilterValidation(string method)
    {
        LoginAs(UserRole.Pengguna);
        var invalid = new AdminRecordFilter { TanggalMulai = new DateOnly(2026, 3, 9), TanggalSelesai = new DateOnly(2026, 3, 1) };

        await Assert.ThrowsAsync<UnauthorizedException>(() => Call(method, invalid));
        await Assert.ThrowsAsync<UnauthorizedException>(() => Call(method, null!));
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task Admin_InvalidDateRange_ThrowsValidation_WithoutQuerying(string method)
    {
        LoginAs(UserRole.Admin);
        var invalid = new AdminRecordFilter { TanggalMulai = new DateOnly(2026, 3, 9), TanggalSelesai = new DateOnly(2026, 3, 1) };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Call(method, invalid));

        Assert.Equal("TanggalSelesai", ex.Field);
        Assert.Empty(_queries.Calls);
    }

    [Fact]
    public void AdminContracts_HaveNoWriteOrDeleteMethods()
    {
        string[] allowedPrefixes = ["Get", "Count"];
        var methods = new[] { typeof(IAdminService), typeof(IAdminQueryRepository), typeof(AdminService) }
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(m => m.Name)
            .ToList();

        Assert.NotEmpty(methods);
        Assert.All(methods, name => Assert.Contains(allowedPrefixes, p => name.StartsWith(p, StringComparison.Ordinal)));
    }

    // =================================================================== Filter

    [Fact]
    public async Task DateFilter_UsesInclusiveJakartaDays()
    {
        LoginAs(UserRole.Admin);
        _queries.Add(Utc(2026, 3, 1, 16, 59, 59), 1m); // 1 Maret 23.59.59 WIB → di luar
        var first = _queries.Add(Utc(2026, 3, 1, 17), 2m);        // 2 Maret 00.00 WIB
        var last = _queries.Add(Utc(2026, 3, 3, 16, 59, 59), 3m); // 3 Maret 23.59.59 WIB
        _queries.Add(Utc(2026, 3, 3, 17), 4m);          // 4 Maret 00.00 WIB → di luar

        var page = await CreateService().GetRecordsAsync(new AdminRecordFilter
        {
            TanggalMulai = new DateOnly(2026, 3, 2),
            TanggalSelesai = new DateOnly(2026, 3, 3),
        });

        Assert.Equal([last, first], page.Items);
        Assert.Equal(2, page.TotalBaris);
    }

    [Fact]
    public async Task CourierFilter_IsCaseInsensitive()
    {
        LoginAs(UserRole.Admin);
        var jne = _queries.Add(Utc(2026, 3, 4), 1m, "jne");
        _queries.Add(Utc(2026, 3, 5), 2m, "jnt");

        var page = await CreateService().GetRecordsAsync(new AdminRecordFilter { KodeKurir = " JNE " });

        Assert.Equal([jne], page.Items);
        Assert.Equal("jne", _queries.Calls[0].Query.KodeKurir);
    }

    [Theory]
    [InlineData("siti")]     // nama, beda huruf besar/kecil
    [InlineData("RAHMA")]    // potongan nama
    [InlineData("s.rahma@")] // potongan email
    [InlineData(" kampus.ac.id ")]
    public async Task KeywordFilter_MatchesNameOrEmail(string keyword)
    {
        LoginAs(UserRole.Admin);
        var siti = _queries.Add(Utc(2026, 3, 4), 1m, nama: "Siti Rahmawati", email: "s.rahma@kampus.ac.id", userId: 2);
        _queries.Add(Utc(2026, 3, 5), 2m, nama: "Budi", email: "budi@test.id", userId: 3);

        var page = await CreateService().GetRecordsAsync(new AdminRecordFilter { KataKunci = keyword });

        Assert.Equal([siti], page.Items);
    }

    [Fact]
    public async Task Filters_AreCombined()
    {
        LoginAs(UserRole.Admin);
        var match = _queries.Add(Utc(2026, 3, 4), 1m, "jnt", "Siti", "siti@test.id", 2);
        _queries.Add(Utc(2026, 3, 4), 1m, "jne", "Siti", "siti@test.id", 2);   // kurir lain
        _queries.Add(Utc(2026, 4, 4), 1m, "jnt", "Siti", "siti@test.id", 2);   // tanggal lain
        _queries.Add(Utc(2026, 3, 4), 1m, "jnt", "Budi", "budi@test.id", 3);   // pengguna lain

        var page = await CreateService().GetRecordsAsync(new AdminRecordFilter
        {
            TanggalMulai = new DateOnly(2026, 3, 1),
            TanggalSelesai = new DateOnly(2026, 3, 31),
            KodeKurir = "jnt",
            KataKunci = "siti",
        });

        Assert.Equal([match], page.Items);
    }

    // =================================================================== Paging

    [Fact]
    public async Task Paging_ReturnsCorrectSlices_NewestFirst_WithTotalRows()
    {
        LoginAs(UserRole.Admin);
        var all = Enumerable.Range(0, 45).Select(i => _queries.Add(Utc(2026, 3, 1).AddHours(i), i)).ToList();
        var newestFirst = all.AsEnumerable().Reverse().ToList();
        var service = CreateService();

        var page1 = await service.GetRecordsAsync(new AdminRecordFilter { Halaman = 1 });
        var page3 = await service.GetRecordsAsync(new AdminRecordFilter { Halaman = 3 });
        var page4 = await service.GetRecordsAsync(new AdminRecordFilter { Halaman = 4 });

        Assert.Equal(newestFirst.Take(20), page1.Items);
        Assert.Equal((45, 1, 20, 3), (page1.TotalBaris, page1.Halaman, page1.UkuranHalaman, page1.TotalHalaman));
        Assert.False(page1.AdaHalamanSebelumnya);
        Assert.True(page1.AdaHalamanBerikutnya);

        Assert.Equal(newestFirst.Skip(40), page3.Items);
        Assert.True(page3.AdaHalamanSebelumnya);
        Assert.False(page3.AdaHalamanBerikutnya);

        Assert.Empty(page4.Items);
        Assert.Equal(45, page4.TotalBaris);
    }

    [Fact]
    public async Task Paging_PagesDoNotOverlap_AndCoverEverything()
    {
        LoginAs(UserRole.Admin);
        for (var i = 0; i < 23; i++)
        {
            _queries.Add(Utc(2026, 3, 4), i); // waktu sama: urutan ditentukan record_id
        }

        var service = CreateService();
        var ids = new List<int>();
        for (var halaman = 1; halaman <= 3; halaman++)
        {
            ids.AddRange((await service.GetRecordsAsync(new AdminRecordFilter { Halaman = halaman, UkuranHalaman = 10 }))
                .Items.Select(r => r.RecordId));
        }

        Assert.Equal(Enumerable.Range(1, 23).Reverse(), ids);
    }

    [Fact]
    public async Task PageSize_IsCappedAt100()
    {
        LoginAs(UserRole.Admin);
        for (var i = 0; i < 150; i++)
        {
            _queries.Add(Utc(2026, 3, 4).AddMinutes(i), 1m);
        }

        var page = await CreateService().GetRecordsAsync(new AdminRecordFilter { UkuranHalaman = 1000 });

        Assert.Equal(100, page.Items.Count);
        Assert.Equal(100, page.UkuranHalaman);
        Assert.Equal(150, page.TotalBaris);
        Assert.Equal(2, page.TotalHalaman);
        Assert.All(_queries.Calls, c => Assert.Equal(100, c.Query.UkuranHalaman));
    }

    [Fact]
    public async Task DefaultPageSize_Is20_AndInvalidPageMeansFirstPage()
    {
        LoginAs(UserRole.Admin);
        for (var i = 0; i < 30; i++)
        {
            _queries.Add(Utc(2026, 3, 4).AddMinutes(i), 1m);
        }

        var page = await CreateService().GetRecordsAsync(new AdminRecordFilter { Halaman = 0, UkuranHalaman = 0 });

        Assert.Equal(20, page.Items.Count);
        Assert.Equal((1, 20), (page.Halaman, page.UkuranHalaman));
    }

    [Fact]
    public async Task NoData_ReturnsEmptyPage_WithZeroPages()
    {
        LoginAs(UserRole.Admin);

        var page = await CreateService().GetRecordsAsync(new AdminRecordFilter());

        Assert.Empty(page.Items);
        Assert.Equal((0, 0), (page.TotalBaris, page.TotalHalaman));
        Assert.False(page.AdaHalamanBerikutnya);
    }

    // =================================================================== Ringkasan

    [Fact]
    public async Task Summary_TotalsAverageAndPerCourier()
    {
        LoginAs(UserRole.Admin);
        _queries.Add(Utc(2026, 3, 4), 1.0000m, "jne", userId: 2);
        _queries.Add(Utc(2026, 3, 5), 0.5000m, "jne", userId: 2);
        _queries.Add(Utc(2026, 3, 6), 0.2500m, "jnt", userId: 3);

        var summary = await CreateService().GetSummaryAsync(new AdminRecordFilter());

        Assert.Equal(2, summary.TotalPengguna);
        Assert.Equal(3, summary.TotalPaket);
        Assert.Equal(1.75m, summary.TotalCo2eKg);
        Assert.Equal(0.5833m, summary.RataRataCo2ePerPaketKg); // 1,75 / 3 = 0,58333...
        Assert.Equal(
            [("jne", 2, 1.5m), ("jnt", 1, 0.25m), ("sicepat", 0, 0m)],
            summary.PerKurir.Select(k => (k.KodeKurir, k.JumlahPaket, k.TotalCo2eKg)));
    }

    [Fact]
    public async Task Summary_FollowsFilter_AndIgnoresPaging()
    {
        LoginAs(UserRole.Admin);
        _queries.Add(Utc(2026, 3, 4), 1m, "jne", "Siti", "siti@test.id", 2);
        _queries.Add(Utc(2026, 3, 5), 3m, "jne", "Siti", "siti@test.id", 2);
        _queries.Add(Utc(2026, 3, 5), 9m, "jne", "Budi", "budi@test.id", 3);

        var summary = await CreateService().GetSummaryAsync(
            new AdminRecordFilter { KataKunci = "siti", KodeKurir = "JNE", Halaman = 5, UkuranHalaman = 1 });

        Assert.Equal((1, 2, 4m, 2m), (summary.TotalPengguna, summary.TotalPaket, summary.TotalCo2eKg, summary.RataRataCo2ePerPaketKg));
        var kurir = Assert.Single(summary.PerKurir);
        Assert.Equal(("jne", 2, 4m), (kurir.KodeKurir, kurir.JumlahPaket, kurir.TotalCo2eKg));
    }

    [Fact]
    public async Task Summary_NoData_AverageIsZero()
    {
        LoginAs(UserRole.Admin);

        var summary = await CreateService().GetSummaryAsync(new AdminRecordFilter());

        Assert.Equal((0, 0, 0m, 0m), (summary.TotalPengguna, summary.TotalPaket, summary.TotalCo2eKg, summary.RataRataCo2ePerPaketKg));
        Assert.All(summary.PerKurir, k => Assert.Equal(0, k.JumlahPaket));
    }

    [Theory]
    [InlineData("0", 0, "0")]
    [InlineData("1", 3, "0.3333")]
    [InlineData("2", 3, "0.6667")]
    [InlineData("0.00005", 1, "0.0001")] // AwayFromZero
    public void Average_RoundsToFourDecimals(string total, int paket, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
            AdminService.Average(decimal.Parse(total, System.Globalization.CultureInfo.InvariantCulture), paket));
    }

    [Fact]
    public void Constructor_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AdminService(null!, _queries));
        Assert.Throws<ArgumentNullException>(() => new AdminService(_session, null!));
    }
}
