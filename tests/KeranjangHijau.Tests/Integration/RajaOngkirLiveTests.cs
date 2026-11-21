using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Infrastructure;
using KeranjangHijau.Infrastructure.Tracking;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// Smoke test opsional ke API RajaOngkir asli (1 request = 1 kuota). Jalankan dengan:
/// <c>KH_RAJAONGKIR_LIVE=1 dotnet test --filter RajaOngkirLiveTests</c>.
/// </summary>
public class RajaOngkirLiveTests
{
    [RajaOngkirLiveFact]
    public async Task FakeResi_ReturnsNull_WithoutException()
    {
        await using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(BackendConfiguration.Build())
            .BuildServiceProvider();

        // Langsung ke ApiCourierTracker (bukan Composite) supaya hasil tidak tertutup data mock.
        var result = await provider.GetRequiredService<ApiCourierTracker>().TrackAsync("jne", "KHSMOKETEST0001");

        Assert.Null(result);
    }
}
