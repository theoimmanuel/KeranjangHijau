using BCrypt.Net;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Infrastructure.Security;

/// <summary>
/// <see cref="IPasswordHasher"/> berbasis BCrypt (paket BCrypt.Net-Next). Hash yang dihasilkan
/// berformat <c>$2a$</c>/<c>$2b$</c> sehingga cocok dengan hash admin di <c>database/seed.sql</c>.
/// </summary>
/// <remarks>BCrypt hanya memakai 72 byte pertama password (batasan algoritma).</remarks>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>Cost default; sama dengan hash admin di seed.</summary>
    public const int DefaultWorkFactor = 11;

    private const int MinWorkFactor = 4;
    private const int MaxWorkFactor = 31;

    private readonly int _workFactor;

    public BcryptPasswordHasher()
        : this(DefaultWorkFactor)
    {
    }

    /// <param name="workFactor">Cost BCrypt (4–31). Nilai kecil hanya untuk unit test.</param>
    public BcryptPasswordHasher(int workFactor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workFactor, MinWorkFactor);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(workFactor, MaxWorkFactor);
        _workFactor = workFactor;
    }

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (Exception ex) when (ex is SaltParseException or ArgumentException)
        {
            // Hash di database rusak/bukan BCrypt (mis. terpotong): anggap password tidak cocok.
            // Hash terpotong membuat BCrypt.Net melempar ArgumentOutOfRangeException, bukan SaltParseException.
            return false;
        }
    }
}
