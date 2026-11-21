using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Core.Services;

/// <summary>Registrasi, login, dan logout (CLAUDE.md bagian 2 dan 7.7).</summary>
public sealed class AuthService : IAuthService
{
    public const string NamaRequiredMessage = "Nama wajib diisi.";
    public const string NamaTooLongMessage = "Nama maksimal 100 karakter.";
    public const string EmailRequiredMessage = "Email wajib diisi.";
    public const string EmailInvalidMessage = "Format email tidak valid.";
    public const string EmailAlreadyRegisteredMessage = "Email sudah terdaftar.";
    public const string PasswordWeakMessage = "Password minimal 8 karakter dan harus berisi huruf dan angka.";
    public const string PasswordTooLongMessage = "Password terlalu panjang (maksimal 72 karakter).";
    public const string LoginFailedMessage = "Email atau password salah.";

    private readonly IUserRepository _users;
    private readonly IForestRepository _forests;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITransactionRunner _transactionRunner;
    private readonly ICurrentUserSession _session;
    private readonly IClock _clock;
    private readonly IOptions<ForestOptions> _forestOptions;
    private readonly Lazy<string> _dummyHash;

    public AuthService(
        IUserRepository users,
        IForestRepository forests,
        IPasswordHasher passwordHasher,
        ITransactionRunner transactionRunner,
        ICurrentUserSession session,
        IClock clock,
        IOptions<ForestOptions> forestOptions)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _forests = forests ?? throw new ArgumentNullException(nameof(forests));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _transactionRunner = transactionRunner ?? throw new ArgumentNullException(nameof(transactionRunner));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _forestOptions = forestOptions ?? throw new ArgumentNullException(nameof(forestOptions));

        // Dipakai saat email tidak ditemukan, supaya waktu respons login sama dengan saat
        // password salah (tidak membocorkan email mana yang terdaftar).
        _dummyHash = new Lazy<string>(() => _passwordHasher.Hash("kh-dummy-password-0"), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<UserDto> RegisterAsync(
        string nama, string email, string password, CancellationToken cancellationToken = default)
    {
        var cleanNama = ValidateNama(nama);
        var cleanEmail = ValidateEmail(email);
        ValidatePassword(password);

        if (await _users.EmailExistsAsync(cleanEmail, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            throw new ValidationException(EmailAlreadyRegisteredMessage, nameof(User.Email));
        }

        // Hash dihitung di luar transaksi karena lambat (BCrypt) dan tidak menyentuh database.
        var now = _clock.UtcNow;
        var user = new User
        {
            Nama = cleanNama,
            Email = cleanEmail,
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.Pengguna, // registrasi tidak pernah membuat admin (bagian 2)
            CreatedAt = now,
        };

        var forestOptions = _forestOptions.Value;

        user.UserId = await _transactionRunner.ExecuteAsync(
            async (tx, ct) =>
            {
                var userId = await _users.InsertAsync(user, tx, ct).ConfigureAwait(false);
                await _forests.InsertAsync(
                    new Forest
                    {
                        UserId = userId,
                        NamaHutan = $"Hutan {cleanNama}",
                        JumlahPohonAwal = forestOptions.InitialTrees,
                        Kesehatan = forestOptions.InitialHealth,
                        Status = ForestStatusRule.GetStatus(forestOptions.InitialHealth).ToDbValue(),
                        UpdatedAt = now,
                    },
                    tx,
                    ct).ConfigureAwait(false);
                return userId;
            },
            cancellationToken).ConfigureAwait(false);

        return UserDto.FromModel(user);
    }

    public async Task<UserDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            throw new ValidationException(LoginFailedMessage);
        }

        var user = await _users.GetByEmailAsync(EmailNormalizer.Normalize(email), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Verify tetap dijalankan walaupun user tidak ada (lihat _dummyHash).
        var passwordMatches = _passwordHasher.Verify(password, user?.PasswordHash ?? _dummyHash.Value);
        if (user is null || !passwordMatches)
        {
            throw new ValidationException(LoginFailedMessage);
        }

        var dto = UserDto.FromModel(user);
        _session.SignIn(dto);
        return dto;
    }

    public void Logout() => _session.SignOut();

    private static string ValidateNama(string? nama)
    {
        var clean = nama?.Trim() ?? string.Empty;
        if (clean.Length == 0)
        {
            throw new ValidationException(NamaRequiredMessage, nameof(User.Nama));
        }

        if (clean.Length > CredentialRules.MaxNamaLength)
        {
            throw new ValidationException(NamaTooLongMessage, nameof(User.Nama));
        }

        return clean;
    }

    private static string ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException(EmailRequiredMessage, nameof(User.Email));
        }

        var clean = EmailNormalizer.Normalize(email);
        if (!CredentialRules.IsValidEmail(clean))
        {
            throw new ValidationException(EmailInvalidMessage, nameof(User.Email));
        }

        return clean;
    }

    private static void ValidatePassword(string? password)
    {
        if (!CredentialRules.IsStrongPassword(password))
        {
            throw new ValidationException(PasswordWeakMessage, "Password");
        }

        if (CredentialRules.IsPasswordTooLong(password!))
        {
            throw new ValidationException(PasswordTooLongMessage, "Password");
        }
    }
}
