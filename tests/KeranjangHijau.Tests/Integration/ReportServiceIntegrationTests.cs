using Dapper;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Infrastructure;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// Laporan (M6.1) dari data hasil alur hitung emisi sungguhan (resi mock, <see cref="FakeClock"/>).
/// Pengguna test dihapus setelah selesai.
/// </summary>
public class ReportServiceIntegrationTests
{
    private const string Password = "HutanHijau2026";

    [DatabaseFact]
    public async Task Report_MatchesRecordedEmissions_PerPeriod_AndOnlyForSessionUser()
    {
        // Selasa 10 Maret 2026 09.00 WIB.
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 10, 2, 0, 0, TimeSpan.Zero));
        await using var provider = BuildProvider(clock);
        var auth = provider.GetRequiredService<IAuthService>();
        var emission = provider.GetRequiredService<IEmissionService>();
        var reports = provider.GetRequiredService<IReportService>();

        var (userId, email) = await RegisterAsync(auth, "Pengguna Laporan");
        var (otherId, otherEmail) = await RegisterAsync(auth, "Pengguna Lain");
        try
        {
            await auth.LoginAsync(email, Password);
            var a = await emission.CalculateFromResiAsync(new EmissionRequest("jne", "JNE1001000001"));
            clock.Advance(TimeSpan.FromHours(3));
            var b = await emission.CalculateFromResiAsync(new EmissionRequest("jne", "JNE1001000003"));
            clock.Advance(TimeSpan.FromDays(7)); // minggu berikutnya, masih bulan Maret
            var c = await emission.CalculateFromResiAsync(new EmissionRequest("jne", "JNE1001000004"));

            var week1 = await reports.GetReportAsync(ReportPeriod.Mingguan, new DateOnly(2026, 3, 10));
            Assert.Equal(new DateOnly(2026, 3, 9), week1.TanggalMulai);
            Assert.Equal([b.ShipmentId, a.ShipmentId], week1.Paket.Select(p => p.ShipmentId));
            Assert.Equal(2, week1.JumlahPaket);
            Assert.Equal(a.TotalCo2eKg + b.TotalCo2eKg, week1.TotalCo2eKg);
            Assert.Equal(7, week1.DataHarian.Count);
            Assert.Equal(new DailyEmissionDto(new DateOnly(2026, 3, 10), 2, week1.TotalCo2eKg), week1.DataHarian[1]);
            AssertEquivalenceTotals(week1, a, b);

            // Default tanggal acuan = hari ini WIB (FakeClock: 17 Maret) → minggu kedua.
            var week2 = await reports.GetReportAsync(ReportPeriod.Mingguan);
            Assert.Equal(c.ShipmentId, Assert.Single(week2.Paket).ShipmentId);
            Assert.Equal(c.TotalCo2eKg, week2.TotalCo2eKg);
            Assert.Equal("Bandung", week1.Paket[1].KotaTujuan);
            Assert.Equal(TransportMode.Pesawat, week1.Paket[0].ModaTransport);

            var month = await reports.GetReportAsync(ReportPeriod.Bulanan);
            Assert.Equal(3, month.JumlahPaket);
            Assert.Equal(a.TotalCo2eKg + b.TotalCo2eKg + c.TotalCo2eKg, month.TotalCo2eKg);
            Assert.Equal(31, month.DataHarian.Count);
            Assert.Equal(month.TotalCo2eKg, month.DataHarian.Sum(d => d.TotalCo2eKg));
            AssertEquivalenceTotals(month, a, b, c);

            var all = await reports.GetReportAsync(ReportPeriod.Semua);
            Assert.Equal([c.ShipmentId, b.ShipmentId, a.ShipmentId], all.Paket.Select(p => p.ShipmentId));
            Assert.Equal(new DateOnly(2026, 3, 10), all.TanggalMulai);
            Assert.Equal(month.TotalCo2eKg, all.TotalCo2eKg);

            // Hutan = kondisi saat ini menurut ForestService.
            Assert.Equal(await provider.GetRequiredService<IForestService>().GetForestAsync(userId), all.Hutan);
            Assert.Equal(c.Hutan!.Kesehatan, all.Hutan.Kesehatan);

            // Bulan tanpa data → laporan kosong yang valid.
            var empty = await reports.GetReportAsync(ReportPeriod.Bulanan, new DateOnly(2026, 2, 1));
            Assert.True(empty.IsEmpty);
            Assert.Equal(0m, empty.TotalCo2eKg);
            Assert.Equal(28, empty.DataHarian.Count);
            Assert.All(empty.KonversiSetara, k => Assert.Equal(0m, k.NilaiSetara));

            // Pengguna lain tidak melihat paket pengguna pertama.
            await auth.LoginAsync(otherEmail, Password);
            var other = await reports.GetReportAsync(ReportPeriod.Semua);
            Assert.True(other.IsEmpty);
            Assert.Empty(other.Paket);
            Assert.Equal("Hutan Pengguna Lain", other.Hutan.NamaHutan);
        }
        finally
        {
            await DeleteUsersAsync(provider, userId, otherId);
        }
    }

    private static void AssertEquivalenceTotals(ReportDto report, params EmissionResultDto[] results)
    {
        Assert.NotEmpty(report.KonversiSetara);
        foreach (var k in report.KonversiSetara)
        {
            var expected = results.Sum(r => r.KonversiSetara.Single(x => x.Aktivitas == k.Aktivitas).NilaiSetara);
            Assert.Equal(expected, k.NilaiSetara);
        }
    }

    private static async Task<(int UserId, string Email)> RegisterAsync(IAuthService auth, string nama)
    {
        var email = $"laporan-{Guid.NewGuid():N}@keranjanghijau.test";
        var user = await auth.RegisterAsync(nama, email, Password);
        return (user.UserId, email);
    }

    private static async Task DeleteUsersAsync(ServiceProvider provider, params int[] userIds)
    {
        // ON DELETE CASCADE ikut menghapus hutan, paket, record, dan nilai setara milik pengguna.
        await using var conn = await provider.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM users WHERE user_id = ANY(@userIds)", new { userIds });
    }

    private static ServiceProvider BuildProvider(FakeClock clock)
    {
        var config = new ConfigurationBuilder()
            .AddKeranjangHijauSources(AppContext.BaseDirectory)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:AllowDuplicateResi"] = "true",
                ["CourierApi:ApiKey"] = "",
            })
            .Build();

        var services = new ServiceCollection().AddKeranjangHijauBackend(config);
        services.AddSingleton<IClock>(clock); // pendaftaran terakhir menang
        return services.BuildServiceProvider();
    }
}
