using Dapper;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Rules;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Infrastructure;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// Bagikan laporan (M6.2) dari data alur hitung emisi sungguhan, dengan <see cref="FakeShareCardRenderer"/>
/// dan <see cref="FakeClock"/>. Pengguna test dan file kartu dihapus setelah selesai.
/// </summary>
public class ShareServiceIntegrationTests
{
    private const string Password = "HutanHijau2026";

    [DatabaseFact]
    public async Task Share_SavesHistoryRow_AndWritesFile()
    {
        // 20 November 2026 09.30.15 WIB.
        var clock = new FakeClock(new DateTimeOffset(2026, 11, 20, 2, 30, 15, TimeSpan.Zero));
        var renderer = new FakeShareCardRenderer();
        using var folder = new TempFolder();
        await using var provider = BuildProvider(clock, renderer);
        var auth = provider.GetRequiredService<IAuthService>();
        var email = $"bagikan-{Guid.NewGuid():N}@keranjanghijau.test";
        var userId = (await auth.RegisterAsync("Pengguna Bagikan", email, Password)).UserId;
        try
        {
            await auth.LoginAsync(email, Password);
            var share = provider.GetRequiredService<IShareService>();

            // Belum ada paket → ditolak, tidak ada riwayat.
            var empty = await Assert.ThrowsAsync<ValidationException>(() => share.ShareAsync(ReportPeriod.Bulanan, folder.Path));
            Assert.Equal(ShareService.EmptyReportMessage, empty.Message);

            var emission = provider.GetRequiredService<IEmissionService>();
            var a = await emission.CalculateFromResiAsync(new EmissionRequest("jne", "JNE1001000001"));
            var b = await emission.CalculateFromResiAsync(new EmissionRequest("jne", "JNE1001000003"));

            var card = await share.BuildCardDataAsync(ReportPeriod.Bulanan);
            Assert.Equal("Pengguna Bagikan", card.NamaPengguna);
            Assert.Equal("November 2026", card.LabelPeriode);
            Assert.Equal(2, card.JumlahPaket);
            Assert.Equal(a.TotalCo2eKg + b.TotalCo2eKg, card.TotalCo2eKg);
            Assert.Equal(
                a.KonversiSetara[0].NilaiSetara + b.KonversiSetara[0].NilaiSetara,
                card.KonversiUtama!.NilaiSetara);
            Assert.Equal(b.Hutan!.Kesehatan, card.KesehatanHutan);
            Assert.Equal(b.Hutan.Status, card.StatusHutan);

            var path = await share.ShareAsync(ReportPeriod.Bulanan, folder.Path);

            Assert.Equal("keranjanghijau-bulanan-20261120093015.png", Path.GetFileName(path));
            Assert.Equal(FakeShareCardRenderer.PngSignature, await File.ReadAllBytesAsync(path));
            Assert.Equal(card, Assert.Single(renderer.Calls).Data);

            var row = Assert.Single(await provider.GetRequiredService<ISharedReportRepository>().GetByUserIdAsync(userId));
            Assert.Equal(PeriodRangeRule.StartOfJakartaDayUtc(new DateOnly(2026, 11, 1)), row.PeriodeMulai);
            Assert.Equal(PeriodRangeRule.StartOfJakartaDayUtc(new DateOnly(2026, 12, 1)), row.PeriodeSelesai);
            Assert.Equal(2, row.JumlahPaket);
            Assert.Equal(card.TotalCo2eKg, row.TotalCo2eKg);
            Assert.Equal(path, row.FileGambar);
            Assert.Equal(clock.UtcNow, row.CreatedAt);
        }
        finally
        {
            await using var conn = await provider.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM users WHERE user_id = @userId", new { userId });
        }
    }

    private static ServiceProvider BuildProvider(FakeClock clock, FakeShareCardRenderer renderer)
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
        services.AddSingleton<IClock>(clock);                   // pendaftaran terakhir menang
        services.AddSingleton<IShareCardRenderer>(renderer);    // seperti proyek WPF mendaftarkan renderer-nya
        return services.BuildServiceProvider();
    }
}
