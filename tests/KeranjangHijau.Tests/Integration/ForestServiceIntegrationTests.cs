using Dapper;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Infrastructure;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// Pemulihan hutan ke database sungguhan dengan <see cref="FakeClock"/>. Pengguna test dihapus setelah selesai.
/// </summary>
public class ForestServiceIntegrationTests
{
    private const string Password = "HutanHijau2026";

    [DatabaseFact]
    public async Task GetForest_AfterIdleDays_RecoversAndWritesLog_ThenHistoryShowsIt()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 10, 1, 0, 0, TimeSpan.Zero));
        await using var provider = BuildProvider(clock);
        var userId = await RegisterAsync(provider);
        try
        {
            // Hutan diatur ke 50 dengan updated_at 3,5 hari lalu.
            var updatedAt = clock.UtcNow.AddDays(-3.5);
            await using (var conn = await provider.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync())
            {
                await conn.ExecuteAsync(
                    "UPDATE forests SET kesehatan = 50, status = 'Menipis', updated_at = @UpdatedAt WHERE user_id = @userId",
                    new { UpdatedAt = updatedAt.UtcDateTime, userId });
            }

            var service = provider.GetRequiredService<IForestService>();
            var dto = await service.GetForestAsync(userId);

            Assert.Equal(53m, dto.Kesehatan);
            Assert.Equal(ForestStatus.Menipis, dto.Status);
            Assert.Equal(53, dto.PerkiraanPohonTersisa);

            var forest = await provider.GetRequiredService<IForestRepository>().GetByUserIdAsync(userId);
            Assert.Equal(53m, forest!.Kesehatan);
            Assert.Equal(updatedAt.AddDays(3), forest.UpdatedAt);

            // Panggilan kedua di waktu yang sama tidak memulihkan lagi.
            Assert.Equal(53m, (await service.GetForestAsync(userId)).Kesehatan);

            var history = await service.GetForestHistoryAsync(userId);
            var log = Assert.Single(history);
            Assert.Null(log.RecordId);
            Assert.Equal(3m, log.Perubahan);
            Assert.Equal(53m, log.KesehatanSetelah);
            Assert.Equal("Pemulihan alami", log.Keterangan);
            Assert.Equal(clock.UtcNow, log.CreatedAt);
        }
        finally
        {
            await using var conn = await provider.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM users WHERE user_id = @userId", new { userId });
        }
    }

    private static async Task<int> RegisterAsync(ServiceProvider provider)
    {
        var email = $"hutan-{Guid.NewGuid():N}@keranjanghijau.test";
        return (await provider.GetRequiredService<IAuthService>().RegisterAsync("Pengguna Hutan", email, Password)).UserId;
    }

    private static ServiceProvider BuildProvider(FakeClock clock)
    {
        var config = new ConfigurationBuilder().AddKeranjangHijauSources(AppContext.BaseDirectory).Build();
        var services = new ServiceCollection().AddKeranjangHijauBackend(config);
        services.AddSingleton<IClock>(clock); // pendaftaran terakhir menang
        return services.BuildServiceProvider();
    }
}
