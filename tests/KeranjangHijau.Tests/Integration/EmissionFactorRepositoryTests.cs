using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Tests.Integration;

public class EmissionFactorRepositoryTests : RepositoryTestBase
{
    [DatabaseFact]
    public async Task EveryTransportMode_HasSeededFactor()
    {
        foreach (var mode in Enum.GetValues<TransportMode>())
        {
            var factor = await EmissionFactors.GetByModeAsync(mode, Transaction);

            Assert.NotNull(factor);
            Assert.Equal(mode, factor.ModaTransport);
            Assert.True(factor.FactorId > 0);
            Assert.True(factor.KgCo2ePerTonKm > 0);
            Assert.False(string.IsNullOrWhiteSpace(factor.Sumber));
        }
    }

    [DatabaseFact]
    public async Task Truk_MatchesSeedValue()
    {
        var factor = await EmissionFactors.GetByModeAsync(TransportMode.Truk, Transaction);

        Assert.NotNull(factor);
        Assert.Equal(0.1m, factor.KgCo2ePerTonKm);
    }

    [DatabaseFact]
    public async Task WorksWithoutTransaction()
    {
        Assert.NotNull(await EmissionFactors.GetByModeAsync(TransportMode.Kapal));
    }
}
