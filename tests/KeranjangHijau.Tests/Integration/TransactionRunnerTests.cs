using Dapper;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

public class TransactionRunnerTests
{
    [DatabaseFact]
    public async Task Execute_Success_Commits()
    {
        await using var provider = BuildProvider();
        var runner = provider.GetRequiredService<ITransactionRunner>();
        var users = provider.GetRequiredService<IUserRepository>();
        var email = $"tx-{Guid.NewGuid():N}@keranjanghijau.test";

        var id = await runner.ExecuteAsync((tx, ct) => users.InsertAsync(NewUser(email), tx, ct));
        try
        {
            Assert.True(await users.EmailExistsAsync(email));
        }
        finally
        {
            await DeleteUserAsync(provider, id);
        }
    }

    [DatabaseFact]
    public async Task Execute_Failure_RollsBackAndRethrowsOriginalException()
    {
        await using var provider = BuildProvider();
        var runner = provider.GetRequiredService<ITransactionRunner>();
        var users = provider.GetRequiredService<IUserRepository>();
        var email = $"tx-{Guid.NewGuid():N}@keranjanghijau.test";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteAsync<int>(async (tx, ct) =>
        {
            await users.InsertAsync(NewUser(email), tx, ct);
            throw new InvalidOperationException("gagal di tengah");
        }));

        Assert.Equal("gagal di tengah", ex.Message);
        Assert.False(await users.EmailExistsAsync(email));
    }

    private static User NewUser(string email) => new()
    {
        Nama = "Test Transaksi",
        Email = email,
        PasswordHash = "x-hash",
    };

    private static async Task DeleteUserAsync(ServiceProvider provider, int userId)
    {
        await using var conn = await provider.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM users WHERE user_id = @userId", new { userId });
    }

    private static ServiceProvider BuildProvider() =>
        new ServiceCollection().AddKeranjangHijauBackend(BackendConfiguration.Build()).BuildServiceProvider();
}
