using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Tests.Fakes;

namespace KeranjangHijau.Tests.Services;

public class ReportServiceTests
{
    private const int UserId = 10;
    private const int OtherUserId = 20;
    private const int MotorId = 1;
    private const int MobilId = 2;

    private readonly InMemoryCurrentUserSession _session = new();
    private readonly InMemoryReportRepository _reports = new();
    private readonly FakeForestService _forests = new();

    // Default FakeClock: Senin 3 Maret 2025 08.00 WIB.
    private readonly FakeClock _clock = new();

    public ReportServiceTests()
    {
        _reports.Activities.Add(new Equivalence { EquivalenceId = MotorId, Aktivitas = "Naik motor", Satuan = "km", KgCo2ePerSatuan = 0.1m, Ikon = "motor", Sumber = "PLACEHOLDER - x" });
        _reports.Activities.Add(new Equivalence { EquivalenceId = MobilId, Aktivitas = "Naik mobil", Satuan = "km", KgCo2ePerSatuan = 0.2m, Ikon = "mobil", Sumber = "Jurnal X" });
        _forests.Forests[UserId] = FakeForestService.Sample();
        _forests.Forests[OtherUserId] = FakeForestService.Sample("Hutan Lain", 10m);
    }

    private ReportService CreateService() => new(_session, _reports, _forests, _clock);

    private void LoginAs(int userId = UserId, UserRole role = UserRole.Pengguna) =>
        _session.SignIn(new UserDto(userId, "Budi", $"u{userId}@test.id", role, _clock.UtcNow));

    private static DateTimeOffset Utc(int y, int mo, int d, int h = 0, int mi = 0) => new(y, mo, d, h, mi, 0, TimeSpan.Zero);

    private static Dictionary<int, decimal> Setara(decimal motor, decimal mobil) => new() { [MotorId] = motor, [MobilId] = mobil };

    // =================================================================== Sesi dan pemilik data

    [Fact]
    public async Task NotLoggedIn_ThrowsUnauthorized_WithoutQuerying()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().GetReportAsync(ReportPeriod.Mingguan));

        Assert.Equal(ReportService.NotLoggedInMessage, ex.Message);
        Assert.Empty(_reports.Calls);
        Assert.Empty(_forests.GetForestUserIds);
    }

    [Fact]
    public async Task Admin_ThrowsUnauthorized()
    {
        LoginAs(1, UserRole.Admin);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().GetReportAsync(ReportPeriod.Semua));

        Assert.Equal(ReportService.NotPenggunaMessage, ex.Message);
        Assert.Empty(_reports.Calls);
    }

    [Fact]
    public async Task OnlySessionUsersData_IsReturned()
    {
        LoginAs(UserId);
        var mine = _reports.Add(UserId, Utc(2025, 3, 3, 2), 1.5m, Setara(15m, 7.5m));
        _reports.Add(OtherUserId, Utc(2025, 3, 3, 3), 9m, Setara(90m, 45m));

        var report = await CreateService().GetReportAsync(ReportPeriod.Mingguan);

        Assert.Equal(mine, Assert.Single(report.Paket));
        Assert.Equal(1.5m, report.TotalCo2eKg);
        Assert.Equal([15m, 7.5m], report.KonversiSetara.Select(k => k.NilaiSetara));
        Assert.Equal(1.5m, report.DataHarian.Sum(d => d.TotalCo2eKg));
        Assert.All(_reports.Calls, c => Assert.Equal(UserId, c.UserId));
        Assert.Equal([UserId], _forests.GetForestUserIds);
        Assert.Equal("Hutan Budi", report.Hutan.NamaHutan);
    }

    [Fact]
    public async Task UsesCurrentSession_AfterSwitchingUser()
    {
        _reports.Add(UserId, Utc(2025, 3, 3, 2), 1m);
        _reports.Add(OtherUserId, Utc(2025, 3, 3, 2), 2m);
        var service = CreateService();

        LoginAs(UserId);
        var first = await service.GetReportAsync(ReportPeriod.Semua);
        LoginAs(OtherUserId);
        var second = await service.GetReportAsync(ReportPeriod.Semua);

        Assert.Equal(1m, first.TotalCo2eKg);
        Assert.Equal(2m, second.TotalCo2eKg);
        Assert.Equal("Hutan Lain", second.Hutan.NamaHutan);
    }

    // =================================================================== Periode

    [Fact]
    public async Task Weekly_DefaultReference_IsTodayInJakarta()
    {
        // Minggu 2 Maret 17.30 UTC = Senin 3 Maret 00.30 WIB → minggu 3–9 Maret.
        _clock.UtcNow = Utc(2025, 3, 2, 17, 30);
        LoginAs();

        var report = await CreateService().GetReportAsync(ReportPeriod.Mingguan);

        Assert.Equal(new DateOnly(2025, 3, 3), report.TanggalMulai);
        Assert.Equal(new DateOnly(2025, 3, 9), report.TanggalSelesai);
        Assert.Equal(Utc(2025, 3, 2, 17), report.PeriodeMulai);
        Assert.Equal(Utc(2025, 3, 9, 17), report.PeriodeSelesai);
        Assert.All(_reports.Calls, c =>
        {
            Assert.Equal(Utc(2025, 3, 2, 17), c.FromUtc);
            Assert.Equal(Utc(2025, 3, 9, 17), c.ToUtc);
        });
    }

    [Fact]
    public async Task Weekly_BoundaryPackages_FollowWibWeek()
    {
        LoginAs();
        _reports.Add(UserId, Utc(2025, 3, 2, 16, 59), 1m);  // Minggu 23.59 WIB → minggu lalu
        var first = _reports.Add(UserId, Utc(2025, 3, 2, 17), 2m);    // Senin 00.00 WIB
        var last = _reports.Add(UserId, Utc(2025, 3, 9, 16, 59), 3m); // Minggu 23.59 WIB
        _reports.Add(UserId, Utc(2025, 3, 9, 17), 4m);      // Senin berikutnya

        var report = await CreateService().GetReportAsync(ReportPeriod.Mingguan, new DateOnly(2025, 3, 5));

        Assert.Equal([last, first], report.Paket); // terbaru dulu
        Assert.Equal(2, report.JumlahPaket);
        Assert.Equal(5m, report.TotalCo2eKg);
    }

    [Fact]
    public async Task Weekly_DailyData_HasSevenDays_WithZeroDays()
    {
        LoginAs();
        _reports.Add(UserId, Utc(2025, 3, 2, 17, 30), 1.25m); // Senin 3 Maret WIB
        _reports.Add(UserId, Utc(2025, 3, 3, 1), 0.75m);      // Senin 3 Maret WIB
        _reports.Add(UserId, Utc(2025, 3, 5, 18), 2m);        // Kamis 6 Maret WIB (Rabu di UTC)

        var report = await CreateService().GetReportAsync(ReportPeriod.Mingguan, new DateOnly(2025, 3, 5));

        Assert.Equal(7, report.DataHarian.Count);
        Assert.Equal(
            Enumerable.Range(0, 7).Select(i => new DateOnly(2025, 3, 3).AddDays(i)),
            report.DataHarian.Select(d => d.Tanggal));
        Assert.Equal(new DailyEmissionDto(new DateOnly(2025, 3, 3), 2, 2m), report.DataHarian[0]);
        Assert.Equal(new DailyEmissionDto(new DateOnly(2025, 3, 4), 0, 0m), report.DataHarian[1]);
        Assert.Equal(new DailyEmissionDto(new DateOnly(2025, 3, 6), 1, 2m), report.DataHarian[3]);
        Assert.Equal(report.TotalCo2eKg, report.DataHarian.Sum(d => d.TotalCo2eKg));
        Assert.Equal(report.JumlahPaket, report.DataHarian.Sum(d => d.JumlahPaket));
    }

    [Fact]
    public async Task Monthly_Feb_UsesWibMonthBoundaries_AndHas28Days()
    {
        LoginAs();
        _reports.Add(UserId, Utc(2025, 1, 31, 16, 59), 1m);   // 31 Jan 23.59 WIB → Januari
        var start = _reports.Add(UserId, Utc(2025, 1, 31, 17), 2m); // 1 Feb 00.00 WIB → Februari
        var end = _reports.Add(UserId, Utc(2025, 2, 28, 16, 59), 3m); // 28 Feb 23.59 WIB
        _reports.Add(UserId, Utc(2025, 2, 28, 17), 4m);       // 1 Mar 00.00 WIB (masih Februari di UTC)

        var report = await CreateService().GetReportAsync(ReportPeriod.Bulanan, new DateOnly(2025, 2, 14));

        Assert.Equal(new DateOnly(2025, 2, 1), report.TanggalMulai);
        Assert.Equal(new DateOnly(2025, 2, 28), report.TanggalSelesai);
        Assert.Equal(Utc(2025, 1, 31, 17), report.PeriodeMulai);
        Assert.Equal(Utc(2025, 2, 28, 17), report.PeriodeSelesai);
        Assert.Equal([end, start], report.Paket);
        Assert.Equal(5m, report.TotalCo2eKg);
        Assert.Equal(28, report.DataHarian.Count);
        Assert.Equal(new DailyEmissionDto(new DateOnly(2025, 2, 1), 1, 2m), report.DataHarian[0]);
        Assert.Equal(new DailyEmissionDto(new DateOnly(2025, 2, 28), 1, 3m), report.DataHarian[^1]);
    }

    [Fact]
    public async Task Monthly_DefaultReference_LastUtcEveningOfMonth_IsNextMonth()
    {
        _clock.UtcNow = Utc(2025, 3, 31, 18); // 1 April 01.00 WIB
        LoginAs();

        var report = await CreateService().GetReportAsync(ReportPeriod.Bulanan);

        Assert.Equal(new DateOnly(2025, 4, 1), report.TanggalMulai);
        Assert.Equal(30, report.DataHarian.Count);
    }

    [Fact]
    public async Task All_IncludesEverythingUntilReferenceDay_StartsAtOldestPackage()
    {
        LoginAs();
        var oldest = _reports.Add(UserId, Utc(2024, 12, 31, 18), 1m); // 1 Jan 2025 WIB
        var middle = _reports.Add(UserId, Utc(2025, 2, 10), 2m);
        _reports.Add(UserId, Utc(2025, 3, 5, 17), 4m);                 // 6 Maret WIB, setelah tanggal acuan

        var report = await CreateService().GetReportAsync(ReportPeriod.Semua, new DateOnly(2025, 3, 5));

        Assert.Equal([middle, oldest], report.Paket);
        Assert.Equal(3m, report.TotalCo2eKg);
        Assert.Equal(new DateOnly(2025, 1, 1), report.TanggalMulai);
        Assert.Equal(Utc(2024, 12, 31, 17), report.PeriodeMulai);
        Assert.Equal(new DateOnly(2025, 3, 5), report.TanggalSelesai);
        Assert.Equal(Utc(2025, 3, 5, 17), report.PeriodeSelesai);
        Assert.Null(_reports.Calls[0].FromUtc);

        // Untuk "Semua", data harian hanya berisi hari yang ada paketnya.
        Assert.Equal([new DateOnly(2025, 1, 1), new DateOnly(2025, 2, 10)], report.DataHarian.Select(d => d.Tanggal));
    }

    // =================================================================== Laporan kosong

    [Theory]
    [InlineData(ReportPeriod.Mingguan, 7)]
    [InlineData(ReportPeriod.Bulanan, 31)]
    [InlineData(ReportPeriod.Semua, 0)]
    public async Task EmptyPeriod_ReturnsValidEmptyReport(ReportPeriod period, int expectedDays)
    {
        LoginAs();
        _reports.Add(UserId, Utc(2024, 1, 10), 5m); // di luar periode mingguan/bulanan
        _reports.Add(OtherUserId, Utc(2025, 3, 3, 2), 5m);
        var reference = period == ReportPeriod.Semua ? new DateOnly(2023, 12, 31) : new DateOnly(2025, 3, 5);

        var report = await CreateService().GetReportAsync(period, reference);

        Assert.True(report.IsEmpty);
        Assert.Equal(0, report.JumlahPaket);
        Assert.Equal(0m, report.TotalCo2eKg);
        Assert.Empty(report.Paket);
        Assert.Equal(expectedDays, report.DataHarian.Count);
        Assert.All(report.DataHarian, d => Assert.Equal((0, 0m), (d.JumlahPaket, d.TotalCo2eKg)));
        Assert.Equal(["Naik motor", "Naik mobil"], report.KonversiSetara.Select(k => k.Aktivitas));
        Assert.All(report.KonversiSetara, k => Assert.Equal(0m, k.NilaiSetara));
        Assert.Equal(FakeForestService.Sample(), report.Hutan);
        Assert.True(report.PeriodeSelesai > report.PeriodeMulai);
    }

    [Fact]
    public async Task EmptyAll_StartsAtReferenceDate()
    {
        LoginAs();

        var report = await CreateService().GetReportAsync(ReportPeriod.Semua, new DateOnly(2025, 3, 5));

        Assert.Equal(new DateOnly(2025, 3, 5), report.TanggalMulai);
        Assert.Equal(new DateOnly(2025, 3, 5), report.TanggalSelesai);
        Assert.Equal(TimeSpan.FromDays(1), report.PeriodeSelesai - report.PeriodeMulai);
    }

    [Fact]
    public async Task NoEquivalenceActivities_GivesEmptyList()
    {
        _reports.Activities.Clear();
        LoginAs();
        _reports.Add(UserId, Utc(2025, 3, 3, 2), 1m);

        var report = await CreateService().GetReportAsync(ReportPeriod.Mingguan);

        Assert.Empty(report.KonversiSetara);
        Assert.Equal(1, report.JumlahPaket);
    }

    // =================================================================== Total

    [Fact]
    public async Task Totals_MatchSumOfRecords_AndEquivalenceSums()
    {
        LoginAs();
        decimal[] totals = [0.1234m, 1.5m, 0m, 2.0001m, 0.375m];
        for (var i = 0; i < totals.Length; i++)
        {
            _reports.Add(UserId, Utc(2025, 3, 3, i), totals[i], Setara(totals[i] * 10m, totals[i] * 5m));
        }

        var report = await CreateService().GetReportAsync(ReportPeriod.Mingguan);

        Assert.Equal(totals.Length, report.JumlahPaket);
        Assert.Equal(totals.Length, report.Paket.Count);
        Assert.Equal(totals.Sum(), report.TotalCo2eKg);
        Assert.Equal(report.Paket.Sum(p => p.TotalCo2eKg), report.TotalCo2eKg);
        Assert.Equal(report.TotalCo2eKg, report.DataHarian.Sum(d => d.TotalCo2eKg));
        Assert.Equal([totals.Sum() * 10m, totals.Sum() * 5m], report.KonversiSetara.Select(k => k.NilaiSetara));

        var motor = report.KonversiSetara[0];
        Assert.Equal(new EquivalenceDto("Naik motor", "km", totals.Sum() * 10m, "motor", true), motor);
        Assert.False(report.KonversiSetara[1].IsPlaceholder);
    }

    // =================================================================== Argumen salah

    [Fact]
    public async Task UnknownPeriod_Throws()
    {
        LoginAs();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CreateService().GetReportAsync((ReportPeriod)42));
        Assert.Empty(_reports.Calls);
    }

    [Fact]
    public async Task MissingForest_ThrowsNotFound()
    {
        LoginAs(99);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetReportAsync(ReportPeriod.Mingguan));
    }
}
