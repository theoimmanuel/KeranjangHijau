using System.Data;
using Dapper;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;
using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="IUserRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class UserRepository : DapperRepositoryBase, IUserRepository
{
    public const string EmailAlreadyRegisteredMessage = "Email sudah terdaftar.";

    private const string SelectColumns =
        "SELECT user_id, nama, email, password_hash, role, created_at FROM users";

    private readonly IClock _clock;

    public UserRepository(IDbConnectionFactory connectionFactory, IClock clock)
        : base(connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public Task<User?> GetByEmailAsync(
        string email, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return QuerySingleUserAsync(
            $"{SelectColumns} WHERE email = @Email",
            new { Email = EmailNormalizer.Normalize(email) },
            transaction,
            cancellationToken);
    }

    public Task<User?> GetByIdAsync(
        int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        QuerySingleUserAsync($"{SelectColumns} WHERE user_id = @UserId", new { UserId = userId }, transaction, cancellationToken);

    public Task<bool> EmailExistsAsync(
        string email, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return RunAsync(
            transaction,
            conn => conn.ExecuteScalarAsync<bool>(Command(
                "SELECT EXISTS (SELECT 1 FROM users WHERE email = @Email)",
                new { Email = EmailNormalizer.Normalize(email) },
                transaction,
                cancellationToken)),
            cancellationToken);
    }

    public async Task<int> InsertAsync(
        User user, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(user.Email, nameof(user.Email));
        ArgumentException.ThrowIfNullOrWhiteSpace(user.PasswordHash, nameof(user.PasswordHash));

        const string sql = """
            INSERT INTO users (nama, email, password_hash, role, created_at)
            VALUES (@Nama, @Email, @PasswordHash, @Role, @CreatedAt)
            RETURNING user_id
            """;

        var parameters = new
        {
            Nama = user.Nama.Trim(),
            Email = EmailNormalizer.Normalize(user.Email),
            user.PasswordHash,
            Role = user.Role.ToDbValue(),
            CreatedAt = (user.CreatedAt == default ? _clock.UtcNow : user.CreatedAt).ToUniversalTime(),
        };

        try
        {
            return await RunAsync(
                transaction,
                conn => conn.ExecuteScalarAsync<int>(Command(sql, parameters, transaction, cancellationToken)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (PostgresErrors.IsUniqueViolation(ex, "uq_users_email"))
        {
            throw new ValidationException(EmailAlreadyRegisteredMessage, nameof(User.Email));
        }
    }

    private async Task<User?> QuerySingleUserAsync(
        string sql, object parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        var row = await RunAsync(
            transaction,
            conn => conn.QuerySingleOrDefaultAsync<UserRow>(Command(sql, parameters, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return row?.ToModel();
    }

    /// <summary>Bentuk mentah baris <c>users</c>; <c>role</c> masih teks.</summary>
    private sealed class UserRow
    {
        public int UserId { get; init; }

        public string Nama { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string PasswordHash { get; init; } = string.Empty;

        public string Role { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public User ToModel() => new()
        {
            UserId = UserId,
            Nama = Nama,
            Email = Email,
            PasswordHash = PasswordHash,
            Role = UserRoleExtensions.ParseUserRole(Role),
            CreatedAt = CreatedAt,
        };
    }
}
