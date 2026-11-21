using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Services;

public class AuthServiceTests
{
    private const string ValidPassword = "HutanHijau2026";

    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryForestRepository _forests = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeTransactionRunner _transactions = new();
    private readonly InMemoryCurrentUserSession _session = new();
    private readonly FakeClock _clock = new();
    private readonly ForestOptions _forestOptions = new();

    private AuthService CreateService() =>
        new(_users, _forests, _hasher, _transactions, _session, _clock, Options.Create(_forestOptions));

    // ---------------------------------------------------------------- Registrasi

    [Fact]
    public async Task Register_Success_CreatesPenggunaAndHealthyForest()
    {
        var result = await CreateService().RegisterAsync("Budi Santoso", "budi@contoh.id", ValidPassword);

        var user = Assert.Single(_users.Users);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal("Budi Santoso", result.Nama);
        Assert.Equal("budi@contoh.id", result.Email);
        Assert.Equal(UserRole.Pengguna, result.Role);
        Assert.False(result.IsAdmin);
        Assert.Equal(_clock.UtcNow, result.CreatedAt);

        var forest = Assert.Single(_forests.Forests);
        Assert.Equal(user.UserId, forest.UserId);
        Assert.Equal(100m, forest.Kesehatan);
        Assert.Equal(100, forest.JumlahPohonAwal);
        Assert.Equal("Hutan Budi Santoso", forest.NamaHutan);
        Assert.Equal("Lestari", forest.Status);
        Assert.Equal(_clock.UtcNow, forest.UpdatedAt);
    }

    [Fact]
    public async Task Register_UserAndForest_AreWrittenInOneCommittedTransaction()
    {
        await CreateService().RegisterAsync("Budi", "budi@contoh.id", ValidPassword);

        Assert.Equal(1, _transactions.Commits);
        Assert.Equal(0, _transactions.Rollbacks);
        Assert.NotNull(_transactions.LastTransaction);
        Assert.Same(_transactions.LastTransaction, Assert.Single(_users.InsertTransactions));
        Assert.Same(_transactions.LastTransaction, Assert.Single(_forests.InsertTransactions));
    }

    [Fact]
    public async Task Register_ForestInsertFails_TransactionIsRolledBack()
    {
        _forests.FailOnInsert = new InvalidOperationException("DB mati");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().RegisterAsync("Budi", "budi@contoh.id", ValidPassword));

        Assert.Equal(0, _transactions.Commits);
        Assert.Equal(1, _transactions.Rollbacks);
    }

    [Fact]
    public async Task Register_UsesForestOptions()
    {
        _forestOptions.InitialTrees = 250;

        await CreateService().RegisterAsync("Siti", "siti@contoh.id", ValidPassword);

        Assert.Equal(250, Assert.Single(_forests.Forests).JumlahPohonAwal);
    }

    [Fact]
    public async Task Register_NormalizesEmailAndTrimsNama()
    {
        var result = await CreateService().RegisterAsync("  Siti  ", "  Siti@Contoh.ID ", ValidPassword);

        Assert.Equal("siti@contoh.id", result.Email);
        Assert.Equal("Siti", result.Nama);
        Assert.Equal("Hutan Siti", Assert.Single(_forests.Forests).NamaHutan);
    }

    [Fact]
    public async Task Register_StoresHashNotPlainPassword()
    {
        await CreateService().RegisterAsync("Budi", "budi@contoh.id", ValidPassword);

        Assert.Equal(_hasher.Hash(ValidPassword), Assert.Single(_users.Users).PasswordHash);
    }

    [Fact]
    public async Task Register_DoesNotSignIn()
    {
        await CreateService().RegisterAsync("Budi", "budi@contoh.id", ValidPassword);

        Assert.False(_session.IsLoggedIn);
    }

    [Theory]
    [InlineData("budi@contoh.id")]
    [InlineData("BUDI@contoh.id")]
    [InlineData("  budi@CONTOH.id  ")]
    public async Task Register_DuplicateEmail_IsRejected(string secondEmail)
    {
        var service = CreateService();
        await service.RegisterAsync("Budi", "budi@contoh.id", ValidPassword);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.RegisterAsync("Budi Lain", secondEmail, ValidPassword));

        Assert.Equal(AuthService.EmailAlreadyRegisteredMessage, ex.Message);
        Assert.Equal("Email", ex.Field);
        Assert.Single(_users.Users);
        Assert.Single(_forests.Forests);
    }

    [Fact]
    public async Task Register_EmailOfSeededAdmin_IsRejected()
    {
        SeedAdmin();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().RegisterAsync("Penyusup", "admin@keranjanghijau.local", ValidPassword));

        Assert.Equal(AuthService.EmailAlreadyRegisteredMessage, ex.Message);
    }

    [Theory]
    [InlineData("pendek1")]          // < 8 karakter
    [InlineData("hanyahuruf")]       // tanpa angka
    [InlineData("1234567890")]       // tanpa huruf
    [InlineData("")]
    [InlineData("        ")]
    public async Task Register_WeakPassword_IsRejected(string password)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().RegisterAsync("Budi", "budi@contoh.id", password));

        Assert.Equal(AuthService.PasswordWeakMessage, ex.Message);
        Assert.Equal("Password", ex.Field);
        Assert.Empty(_users.Users);
        Assert.Equal(0, _transactions.Commits);
    }

    [Fact]
    public async Task Register_PasswordLongerThan72Bytes_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().RegisterAsync("Budi", "budi@contoh.id", new string('a', 72) + "1"));

        Assert.Equal(AuthService.PasswordTooLongMessage, ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_EmptyNama_IsRejected(string nama)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().RegisterAsync(nama, "budi@contoh.id", ValidPassword));

        Assert.Equal(AuthService.NamaRequiredMessage, ex.Message);
        Assert.Equal("Nama", ex.Field);
    }

    [Fact]
    public async Task Register_NamaTooLong_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().RegisterAsync(new string('A', 101), "budi@contoh.id", ValidPassword));

        Assert.Equal(AuthService.NamaTooLongMessage, ex.Message);
    }

    [Theory]
    [InlineData("", AuthService.EmailRequiredMessage)]
    [InlineData("   ", AuthService.EmailRequiredMessage)]
    [InlineData("budi", AuthService.EmailInvalidMessage)]
    [InlineData("budi@contoh", AuthService.EmailInvalidMessage)]
    [InlineData("budi contoh@mail.com", AuthService.EmailInvalidMessage)]
    public async Task Register_InvalidEmail_IsRejected(string email, string expectedMessage)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().RegisterAsync("Budi", email, ValidPassword));

        Assert.Equal(expectedMessage, ex.Message);
        Assert.Equal("Email", ex.Field);
    }

    // ---------------------------------------------------------------- Login

    [Fact]
    public async Task Login_RegisteredUser_ReturnsDtoAndFillsSession()
    {
        var service = CreateService();
        var registered = await service.RegisterAsync("Budi", "budi@contoh.id", ValidPassword);

        var result = await service.LoginAsync(" BUDI@contoh.id ", ValidPassword);

        Assert.Equal(registered, result);
        Assert.True(_session.IsLoggedIn);
        Assert.False(_session.IsAdmin);
        Assert.Equal(result, _session.CurrentUser);
    }

    [Fact]
    public async Task Login_Admin_SetsIsAdminTrue()
    {
        SeedAdmin();

        var result = await CreateService().LoginAsync("admin@keranjanghijau.local", "AdminKH2026!");

        Assert.True(result.IsAdmin);
        Assert.Equal(UserRole.Admin, result.Role);
        Assert.True(_session.IsLoggedIn);
        Assert.True(_session.IsAdmin);
    }

    [Fact]
    public async Task Login_WrongPassword_IsRejectedWithGenericMessage()
    {
        var service = CreateService();
        await service.RegisterAsync("Budi", "budi@contoh.id", ValidPassword);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.LoginAsync("budi@contoh.id", "SalahSekali99"));

        Assert.Equal(AuthService.LoginFailedMessage, ex.Message);
        Assert.Null(ex.Field);
        Assert.False(_session.IsLoggedIn);
    }

    [Fact]
    public async Task Login_UnknownEmail_IsRejectedWithSameMessage_AndStillVerifies()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().LoginAsync("tidakada@contoh.id", ValidPassword));

        Assert.Equal(AuthService.LoginFailedMessage, ex.Message);
        Assert.Null(ex.Field);
        Assert.Equal(1, _hasher.VerifyCalls); // waktu respons sama dengan password salah
        Assert.False(_session.IsLoggedIn);
    }

    [Theory]
    [InlineData("", ValidPassword)]
    [InlineData("budi@contoh.id", "")]
    [InlineData("   ", "   ")]
    public async Task Login_EmptyInput_IsRejectedWithSameMessage(string email, string password)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateService().LoginAsync(email, password));

        Assert.Equal(AuthService.LoginFailedMessage, ex.Message);
    }

    [Fact]
    public async Task Login_FailedAfterSuccessfulLogin_KeepsPreviousSession()
    {
        var service = CreateService();
        await service.RegisterAsync("Budi", "budi@contoh.id", ValidPassword);
        var budi = await service.LoginAsync("budi@contoh.id", ValidPassword);

        await Assert.ThrowsAsync<ValidationException>(() => service.LoginAsync("budi@contoh.id", "salah12345"));

        Assert.Equal(budi, _session.CurrentUser);
    }

    [Fact]
    public async Task Logout_ClearsSession()
    {
        SeedAdmin();
        var service = CreateService();
        await service.LoginAsync("admin@keranjanghijau.local", "AdminKH2026!");

        service.Logout();

        Assert.False(_session.IsLoggedIn);
        Assert.False(_session.IsAdmin);
        Assert.Null(_session.CurrentUser);
    }

    [Fact]
    public void RegisterAsync_HasNoRoleParameter()
    {
        // Registrasi tidak boleh bisa membuat admin: API-nya tidak menerima peran sama sekali.
        var parameters = typeof(KeranjangHijau.Core.Interfaces.IAuthService)
            .GetMethod(nameof(AuthService.RegisterAsync))!
            .GetParameters();

        Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(UserRole));
    }

    private void SeedAdmin() =>
        _users.Seed(new User
        {
            Nama = "Admin KeranjangHijau",
            Email = "admin@keranjanghijau.local",
            PasswordHash = _hasher.Hash("AdminKH2026!"),
            Role = UserRole.Admin,
        });
}
