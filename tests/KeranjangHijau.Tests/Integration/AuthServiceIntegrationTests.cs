using System.Data;
using Dapper;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Integration;

/// <summary>Registrasi dan login ke database sungguhan. Data test dihapus setelah selesai.</summary>
public class AuthServiceIntegrationTests
{
    [DatabaseFact]
    public async Task Register_ThenLogin_WorksEndToEnd()
    {
        await using var provider = BuildProvider();
        var auth = provider.GetRequiredService<IAuthService>();
        var session = provider.GetRequiredService<ICurrentUserSession>();
        var email = UniqueEmail();

        var registered = await auth.RegisterAsync("Budi Integrasi", email.ToUpperInvariant(), "HutanHijau2026");
        try
        {
            var forest = await provider.GetRequiredService<IForestRepository>().GetByUserIdAsync(registered.UserId);
            Assert.NotNull(forest);
            Assert.Equal(100m, forest.Kesehatan);
            Assert.Equal("Hutan Budi Integrasi", forest.NamaHutan);

            var loggedIn = await auth.LoginAsync(email, "HutanHijau2026");
            Assert.Equal(registered.UserId, loggedIn.UserId);
            Assert.Equal(email, loggedIn.Email);
            Assert.True(session.IsLoggedIn);
            Assert.False(session.IsAdmin);

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => auth.RegisterAsync("Budi Lagi", email, "HutanHijau2026"));
            Assert.Equal(AuthService.EmailAlreadyRegisteredMessage, ex.Message);
        }
        finally
        {
            await DeleteUserAsync(provider, registered.UserId);
        }
    }

    [DatabaseFact]
    public async Task Login_SeedAdmin_IsAdmin()
    {
        await using var provider = BuildProvider();

        var admin = await provider.GetRequiredService<IAuthService>().LoginAsync("admin@keranjanghijau.local", "AdminKH2026!");

        Assert.True(admin.IsAdmin);
        Assert.True(provider.GetRequiredService<ICurrentUserSession>().IsAdmin);
    }

    [DatabaseFact]
    public async Task Register_ForestInsertFails_UserIsNotSaved()
    {
        await using var provider = BuildProvider();
        var users = provider.GetRequiredService<IUserRepository>();
        var auth = new AuthService(
            users,
            new FailingForestRepository(),
            provider.GetRequiredService<IPasswordHasher>(),
            provider.GetRequiredService<ITransactionRunner>(),
            provider.GetRequiredService<ICurrentUserSession>(),
            provider.GetRequiredService<IClock>(),
            provider.GetRequiredService<IOptions<ForestOptions>>());
        var email = UniqueEmail();

        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.RegisterAsync("Gagal", email, "HutanHijau2026"));

        Assert.False(await users.EmailExistsAsync(email));
    }

    private static string UniqueEmail() => $"auth-{Guid.NewGuid():N}@keranjanghijau.test";

    private static async Task DeleteUserAsync(ServiceProvider provider, int userId)
    {
        await using var conn = await provider.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM users WHERE user_id = @userId", new { userId });
    }

    private static ServiceProvider BuildProvider() =>
        new ServiceCollection().AddKeranjangHijauBackend(BackendConfiguration.Build()).BuildServiceProvider();

    private sealed class FailingForestRepository : IForestRepository
    {
        public Task<Forest?> GetByUserIdAsync(int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<Forest?>(null);

        public Task<Forest?> GetByUserIdForUpdateAsync(int userId, IDbTransaction transaction, CancellationToken cancellationToken = default) =>
            Task.FromResult<Forest?>(null);

        public Task<int> InsertAsync(Forest forest, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulasi gagal membuat hutan.");

        public Task UpdateHealthAsync(int forestId, decimal kesehatan, string status, DateTimeOffset updatedAt, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
