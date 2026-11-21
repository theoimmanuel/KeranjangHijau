using System.Data;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Fakes;

/// <summary>
/// <see cref="IUserRepository"/> di memori dengan perilaku yang sama seperti repository Dapper
/// (normalisasi email, tolak email ganda).
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];
    private int _nextId = 1;

    public IReadOnlyList<User> Users => _users;

    /// <summary>Transaksi yang diterima pada setiap pemanggilan <see cref="InsertAsync"/>.</summary>
    public List<IDbTransaction?> InsertTransactions { get; } = [];

    /// <summary>Menambahkan user langsung (mis. admin dari seed), seperti data yang sudah ada di DB.</summary>
    public User Seed(User user)
    {
        user.UserId = _nextId++;
        user.Email = EmailNormalizer.Normalize(user.Email);
        _users.Add(user);
        return user;
    }

    public Task<User?> GetByEmailAsync(string email, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Find(email));

    public Task<User?> GetByIdAsync(int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.SingleOrDefault(u => u.UserId == userId));

    public Task<bool> EmailExistsAsync(string email, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Find(email) is not null);

    public Task<int> InsertAsync(User user, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        InsertTransactions.Add(transaction);
        if (Find(user.Email) is not null)
        {
            throw new ValidationException("Email sudah terdaftar.", nameof(User.Email));
        }

        var copy = new User
        {
            UserId = _nextId++,
            Nama = user.Nama,
            Email = EmailNormalizer.Normalize(user.Email),
            PasswordHash = user.PasswordHash,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
        };
        _users.Add(copy);
        return Task.FromResult(copy.UserId);
    }

    private User? Find(string email)
    {
        var normalized = EmailNormalizer.Normalize(email);
        return _users.SingleOrDefault(u => u.Email == normalized);
    }
}
