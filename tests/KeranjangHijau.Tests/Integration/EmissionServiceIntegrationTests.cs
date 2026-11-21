using System.Data;
using Dapper;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Rules;
using KeranjangHijau.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// Alur penuh hitung emisi ke database sungguhan: registrasi → login → resi mock → simpan.
/// API key dikosongkan agar tracker langsung memakai mock. Data test dihapus setelah selesai.
/// </summary>
public class EmissionServiceIntegrationTests
{
    private const string Password = "HutanHijau2026";

    [DatabaseFact]
    public async Task CalculateFromResi_MockResi_SavesShipmentAndRecord()
    {
        await using var provider = BuildProvider();
        var userId = await RegisterAndLoginAsync(provider);
        try
        {
            // mock-resi.json: jne JNE1001000003, Jakarta → Medan, YES, 0,8 kg → pesawat.
            var result = await provider.GetRequiredService<IEmissionService>()
                .CalculateFromResiAsync(new EmissionRequest("jne", "jne1001000003"));

            Assert.Equal("JNE1001000003", result.NomorResi);
            Assert.Equal("Jakarta", result.KotaAsal);
            Assert.Equal("Medan", result.KotaTujuan);
            Assert.Equal(TransportMode.Pesawat, result.ModaTransport);
            Assert.Equal(0.8m, result.BeratKg);
            Assert.Equal(WeightSource.Api, result.SumberBerat);
            Assert.Equal(ShipmentDataSource.Mock, result.SumberData);
            Assert.True(result.JarakKm > 1000m);
            Assert.True(result.TotalCo2eKg > 0m);

            var record = await provider.GetRequiredService<IEmissionRecordRepository>().GetByIdAsync(result.RecordId);
            Assert.NotNull(record);
            Assert.Equal(result.ShipmentId, record.ShipmentId);
            Assert.Equal(result.JarakKm, record.JarakKm);
            Assert.Equal(result.TotalCo2eKg, record.TotalCo2eKg);
            Assert.Equal(EmissionMethod.RumusLokal, record.Metode);

            // Konversi setara (M4.1): satu baris record_equivalences per aktivitas, nilai dari tabel equivalences.
            var repo = provider.GetRequiredService<IEquivalenceRepository>();
            var equivalences = await repo.GetAllAsync();
            var saved = await repo.GetByRecordIdAsync(result.RecordId);
            Assert.NotEmpty(equivalences);
            Assert.Equal(equivalences.Count, saved.Count);
            Assert.Equal(
                equivalences.Select(e => Math.Round(result.TotalCo2eKg / e.KgCo2ePerSatuan, 2, MidpointRounding.AwayFromZero)),
                saved.Select(v => v.NilaiSetara));
            Assert.Equal(saved.Select(v => v.ToDto()), result.KonversiSetara);

            // Hutan (M5.1): kesehatan turun total_co2e_kg × 10 poin dari 100, dan satu log emisi tertulis.
            var expectedHealth = Math.Max(0m, 100m - Math.Round(result.TotalCo2eKg * 10m, 2, MidpointRounding.AwayFromZero));
            var forest = await provider.GetRequiredService<IForestRepository>().GetByUserIdAsync(userId);
            Assert.NotNull(forest);
            Assert.Equal(expectedHealth, forest.Kesehatan);
            Assert.Equal(ForestStatusRule.GetStatus(expectedHealth).ToDbValue(), forest.Status);
            Assert.NotNull(result.Hutan);
            Assert.Equal(expectedHealth, result.Hutan.Kesehatan);
            Assert.Equal(ForestStatusRule.GetStatus(expectedHealth), result.Hutan.Status);
            Assert.Equal(expectedHealth - 100m, result.Hutan.Perubahan);

            var log = Assert.Single(await provider.GetRequiredService<IForestLogRepository>().GetByForestIdAsync(forest.ForestId, 10));
            Assert.Equal(result.RecordId, log.RecordId);
            Assert.Equal(result.Hutan.Perubahan, log.Perubahan);
            Assert.Equal(expectedHealth, log.KesehatanSetelah);
            Assert.Equal("Emisi paket JNE1001000003", log.Keterangan);
            Assert.True(forest.UpdatedAt >= result.CalculatedAt.AddSeconds(-1));

            await using var conn = await OpenAsync(provider);
            var shipment = await conn.QuerySingleAsync<(int UserId, string NomorResi, string Layanan, decimal BeratKg, string SumberData)>(
                "SELECT user_id, nomor_resi, layanan, berat_kg, sumber_data FROM shipments WHERE shipment_id = @Id",
                new { Id = result.ShipmentId });
            Assert.Equal(userId, shipment.UserId);
            Assert.Equal("JNE1001000003", shipment.NomorResi);
            Assert.Equal("YES", shipment.Layanan);
            Assert.Equal(0.8m, shipment.BeratKg);
            Assert.Equal("mock", shipment.SumberData);
        }
        finally
        {
            await DeleteUserAsync(provider, userId);
        }
    }

    [DatabaseFact]
    public async Task CalculateFromResi_ManualWeight_StoredAsManual_AndDuplicatesAllowed()
    {
        await using var provider = BuildProvider();
        var userId = await RegisterAndLoginAsync(provider);
        try
        {
            var service = provider.GetRequiredService<IEmissionService>();

            // mock-resi.json: jne JNE1001000002, Surabaya → Makassar, REG, tanpa berat → kapal.
            var first = await service.CalculateFromResiAsync(new EmissionRequest("jne", "JNE1001000002", 2.5m));
            var second = await service.CalculateFromResiAsync(new EmissionRequest("jne", "JNE1001000002", 2.5m));

            Assert.Equal(TransportMode.Kapal, first.ModaTransport);
            Assert.Equal(ShipmentDataSource.Manual, first.SumberData);
            Assert.NotEqual(first.ShipmentId, second.ShipmentId);
            Assert.Equal(2, await CountShipmentsAsync(provider, userId));
        }
        finally
        {
            await DeleteUserAsync(provider, userId);
        }
    }

    [DatabaseFact]
    public async Task HandlerFailure_RollsBackShipmentAndRecord()
    {
        await using var provider = BuildProvider(services =>
            services.AddSingleton<IEmissionRecordedHandler, FailingHandler>());
        var userId = await RegisterAndLoginAsync(provider);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRequiredService<IEmissionService>()
                .CalculateFromResiAsync(new EmissionRequest("jne", "JNE1001000001")));

            Assert.Equal(0, await CountShipmentsAsync(provider, userId));

            // Perubahan hutan (handler M5.1, dipanggil sebelum handler yang gagal) ikut dibatalkan.
            var forest = await provider.GetRequiredService<IForestRepository>().GetByUserIdAsync(userId);
            Assert.Equal(100m, forest!.Kesehatan);
            Assert.Empty(await provider.GetRequiredService<IForestLogRepository>().GetByForestIdAsync(forest.ForestId, 10));
        }
        finally
        {
            await DeleteUserAsync(provider, userId);
        }
    }

    private static async Task<int> RegisterAndLoginAsync(ServiceProvider provider)
    {
        var auth = provider.GetRequiredService<IAuthService>();
        var email = $"emisi-{Guid.NewGuid():N}@keranjanghijau.test";
        var user = await auth.RegisterAsync("Pengguna Emisi", email, Password);
        await auth.LoginAsync(email, Password);
        return user.UserId;
    }

    private static async Task<System.Data.Common.DbConnection> OpenAsync(ServiceProvider provider) =>
        await provider.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync();

    private static async Task<int> CountShipmentsAsync(ServiceProvider provider, int userId)
    {
        await using var conn = await OpenAsync(provider);
        return await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM shipments WHERE user_id = @userId", new { userId });
    }

    private static async Task DeleteUserAsync(ServiceProvider provider, int userId)
    {
        // ON DELETE CASCADE ikut menghapus forests, shipments, emission_records, record_equivalences, dan forest_logs milik pengguna.
        await using var conn = await OpenAsync(provider);
        await conn.ExecuteAsync("DELETE FROM users WHERE user_id = @userId", new { userId });
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
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
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private sealed class FailingHandler : IEmissionRecordedHandler
    {
        public Task HandleAsync(EmissionRecordedContext context, IDbTransaction transaction, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulasi handler gagal.");
    }
}
