using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>
/// Sesi login aplikasi desktop (satu pengguna aktif per aplikasi). Diisi oleh
/// <see cref="IAuthService"/>; dibaca service lain untuk memeriksa login dan peran admin.
/// </summary>
public interface ICurrentUserSession
{
    /// <summary>Pengguna yang sedang login, atau null jika belum login.</summary>
    UserDto? CurrentUser { get; }

    bool IsLoggedIn { get; }

    bool IsAdmin { get; }

    /// <summary>Menyimpan pengguna sebagai pengguna aktif (menggantikan sesi sebelumnya).</summary>
    void SignIn(UserDto user);

    void SignOut();
}
