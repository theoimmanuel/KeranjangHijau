using Dapper;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

public class UserRepositoryTests : RepositoryTestBase
{
    [DatabaseFact]
    public async Task Insert_ThenGetById_ReturnsSameUser()
    {
        var user = NewUser();
        user.Role = UserRole.Pengguna;

        var id = await Users.InsertAsync(user, Transaction);
        var loaded = await Users.GetByIdAsync(id, Transaction);

        Assert.True(id > 0);
        Assert.NotNull(loaded);
        Assert.Equal(id, loaded.UserId);
        Assert.Equal(user.Nama, loaded.Nama);
        Assert.Equal(user.Email, loaded.Email);
        Assert.Equal(user.PasswordHash, loaded.PasswordHash);
        Assert.Equal(UserRole.Pengguna, loaded.Role);
        Assert.Equal(user.CreatedAt, loaded.CreatedAt);
        Assert.Equal(TimeSpan.Zero, loaded.CreatedAt.Offset);
    }

    [DatabaseFact]
    public async Task Insert_StoresEmailLowercaseAndTrimmed()
    {
        var email = UniqueEmail();
        var id = await Users.InsertAsync(NewUser($"  {email.ToUpperInvariant()}  "), Transaction);

        var stored = await Transaction.Connection!.ExecuteScalarAsync<string>(
            "SELECT email FROM users WHERE user_id = @id", new { id }, Transaction);

        Assert.Equal(email, stored);
    }

    [DatabaseFact]
    public async Task GetByEmail_IgnoresCaseAndSurroundingSpaces()
    {
        var email = UniqueEmail();
        var id = await Users.InsertAsync(NewUser(email), Transaction);

        var loaded = await Users.GetByEmailAsync($" {email.ToUpperInvariant()} ", Transaction);

        Assert.NotNull(loaded);
        Assert.Equal(id, loaded.UserId);
    }

    [DatabaseFact]
    public async Task GetByEmail_And_GetById_Unknown_ReturnNull()
    {
        Assert.Null(await Users.GetByEmailAsync(UniqueEmail(), Transaction));
        Assert.Null(await Users.GetByIdAsync(-1, Transaction));
    }

    [DatabaseFact]
    public async Task EmailExists_ReflectsInsertedUser()
    {
        var email = UniqueEmail();

        Assert.False(await Users.EmailExistsAsync(email, Transaction));
        await Users.InsertAsync(NewUser(email), Transaction);
        Assert.True(await Users.EmailExistsAsync(email.ToUpperInvariant(), Transaction));
    }

    [DatabaseFact]
    public async Task Insert_DuplicateEmailDifferentCase_ThrowsValidationException()
    {
        var email = UniqueEmail();
        await Users.InsertAsync(NewUser(email), Transaction);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Users.InsertAsync(NewUser(email.ToUpperInvariant()), Transaction));

        Assert.Equal(UserRepository.EmailAlreadyRegisteredMessage, ex.Message);
        Assert.Equal("Email", ex.Field);
    }

    [DatabaseFact]
    public async Task Insert_AdminRole_IsStoredAsText()
    {
        var user = NewUser();
        user.Role = UserRole.Admin;

        var id = await Users.InsertAsync(user, Transaction);
        var role = await Transaction.Connection!.ExecuteScalarAsync<string>(
            "SELECT role FROM users WHERE user_id = @id", new { id }, Transaction);

        Assert.Equal("admin", role);
        Assert.Equal(UserRole.Admin, (await Users.GetByIdAsync(id, Transaction))!.Role);
    }

    [DatabaseFact]
    public async Task Insert_WithoutCreatedAt_UsesClock()
    {
        var user = NewUser();
        user.CreatedAt = default;
        var before = DateTimeOffset.UtcNow.AddSeconds(-5);

        var id = await Users.InsertAsync(user, Transaction);
        var loaded = await Users.GetByIdAsync(id, Transaction);

        Assert.InRange(loaded!.CreatedAt, before, DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [DatabaseFact]
    public async Task Rollback_DiscardsInsertedUser()
    {
        var email = UniqueEmail();
        var factory = Provider.GetRequiredService<IDbConnectionFactory>();

        await using (var conn = await factory.OpenConnectionAsync())
        await using (var tx = await conn.BeginTransactionAsync())
        {
            await Users.InsertAsync(NewUser(email), tx);
            Assert.True(await Users.EmailExistsAsync(email, tx));
            await tx.RollbackAsync();
        }

        Assert.False(await Users.EmailExistsAsync(email));
    }

    [DatabaseFact]
    public async Task WithoutTransaction_OpensOwnConnection()
    {
        var email = UniqueEmail();
        var id = await Users.InsertAsync(NewUser(email));
        try
        {
            var loaded = await Users.GetByEmailAsync(email);

            Assert.NotNull(loaded);
            Assert.Equal(id, loaded.UserId);
        }
        finally
        {
            await using var conn = await Provider.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM users WHERE user_id = @id", new { id });
        }
    }

    [DatabaseFact]
    public async Task SeedAdmin_IsReadableAndPasswordVerifies()
    {
        var admin = await Users.GetByEmailAsync("admin@keranjanghijau.local", Transaction);

        Assert.NotNull(admin);
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.True(Provider.GetRequiredService<IPasswordHasher>().Verify("AdminKH2026!", admin.PasswordHash));
    }
}
