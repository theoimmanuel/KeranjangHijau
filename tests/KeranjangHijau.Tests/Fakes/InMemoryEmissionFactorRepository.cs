using System.Data;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="IEmissionFactorRepository"/> di memori, berisi faktor sementara dari seed.sql.</summary>
public sealed class InMemoryEmissionFactorRepository : IEmissionFactorRepository
{
    public Dictionary<TransportMode, EmissionFactor> Factors { get; } = new()
    {
        [TransportMode.Truk] = new() { FactorId = 1, ModaTransport = TransportMode.Truk, KgCo2ePerTonKm = 0.1m, Sumber = "PLACEHOLDER" },
        [TransportMode.Kapal] = new() { FactorId = 2, ModaTransport = TransportMode.Kapal, KgCo2ePerTonKm = 0.02m, Sumber = "PLACEHOLDER" },
        [TransportMode.Pesawat] = new() { FactorId = 3, ModaTransport = TransportMode.Pesawat, KgCo2ePerTonKm = 0.8m, Sumber = "PLACEHOLDER" },
    };

    public Task<EmissionFactor?> GetByModeAsync(TransportMode mode, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Factors.GetValueOrDefault(mode));
}
