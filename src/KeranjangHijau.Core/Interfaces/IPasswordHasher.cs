namespace KeranjangHijau.Core.Interfaces;

/// <summary>Hash dan verifikasi password (implementasi: BCrypt).</summary>
public interface IPasswordHasher
{
    /// <summary>Membuat hash (dengan salt acak) dari password.</summary>
    string Hash(string password);

    /// <summary>
    /// True jika <paramref name="password"/> cocok dengan <paramref name="passwordHash"/>.
    /// Hash yang rusak/tidak valid menghasilkan false, bukan exception.
    /// </summary>
    bool Verify(string password, string passwordHash);
}
