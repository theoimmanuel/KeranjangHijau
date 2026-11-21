using System.Data;
using Dapper;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="IEmissionFactorRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class EmissionFactorRepository : DapperRepositoryBase, IEmissionFactorRepository
{
    public EmissionFactorRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task<EmissionFactor?> GetByModeAsync(
        TransportMode mode, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT factor_id, moda_transport, kg_co2e_per_ton_km, sumber
            FROM emission_factors
            WHERE moda_transport = @ModaTransport
            """;

        var row = await RunAsync(
            transaction,
            conn => conn.QuerySingleOrDefaultAsync<EmissionFactorRow>(Command(
                sql,
                new { ModaTransport = mode.ToDbValue() },
                transaction,
                cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return row?.ToModel();
    }

    /// <summary>Bentuk mentah baris <c>emission_factors</c>; <c>moda_transport</c> masih teks.</summary>
    private sealed class EmissionFactorRow
    {
        public int FactorId { get; init; }

        public string ModaTransport { get; init; } = string.Empty;

        public decimal KgCo2ePerTonKm { get; init; }

        public string Sumber { get; init; } = string.Empty;

        public EmissionFactor ToModel() => new()
        {
            FactorId = FactorId,
            ModaTransport = TransportModeExtensions.ParseTransportMode(ModaTransport),
            KgCo2ePerTonKm = KgCo2ePerTonKm,
            Sumber = Sumber,
        };
    }
}
