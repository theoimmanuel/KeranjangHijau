using Dapper;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// Butuh PostgreSQL yang berjalan dan <c>KH_DB</c> di-set, mis.
/// <c>KH_DB=postgresql://user:pass@localhost:5432/keranjanghijau dotnet test</c>.
/// </summary>
public class DatabaseConnectionTests
{
    [DatabaseFact]
    public async Task OpenConnection_CanRunQuery()
    {
        await using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IDbConnectionFactory>();

        await using var conn = await factory.OpenConnectionAsync();
        var result = await conn.ExecuteScalarAsync<int>("SELECT 1");

        Assert.Equal(1, result);
    }

    [DatabaseFact]
    public async Task Dapper_MapsSnakeCaseColumnsToPascalCase()
    {
        await using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IDbConnectionFactory>();

        await using var conn = await factory.OpenConnectionAsync();
        var row = await conn.QuerySingleAsync<SampleRow>(
            "SELECT @Kode AS kode_kurir, 'JNE' AS nama_kurir", new { Kode = "jne" });

        Assert.Equal("jne", row.KodeKurir);
        Assert.Equal("JNE", row.NamaKurir);
    }

    [DatabaseFact]
    public async Task Transaction_CanBeRolledBack()
    {
        await using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IDbConnectionFactory>();

        await using var conn = await factory.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        await conn.ExecuteAsync("CREATE TEMP TABLE kh_tx_probe (id INT) ON COMMIT DROP", transaction: tx);
        await conn.ExecuteAsync("INSERT INTO kh_tx_probe VALUES (1)", transaction: tx);
        var inside = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM kh_tx_probe", transaction: tx);
        await tx.RollbackAsync();

        Assert.Equal(1, inside);
    }

    private static ServiceProvider BuildProvider() =>
        new ServiceCollection()
            .AddKeranjangHijauBackend(BackendConfiguration.Build())
            .BuildServiceProvider();

    private sealed class SampleRow
    {
        public string KodeKurir { get; set; } = string.Empty;

        public string NamaKurir { get; set; } = string.Empty;
    }
}
