using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Core.Services;

/// <summary>
/// <see cref="ICurrentUserSession"/> yang disimpan di memori. Didaftarkan sebagai singleton,
/// jadi sesi hilang saat aplikasi ditutup.
/// </summary>
public sealed class InMemoryCurrentUserSession : ICurrentUserSession
{
    private volatile UserDto? _currentUser;

    public UserDto? CurrentUser => _currentUser;

    public bool IsLoggedIn => _currentUser is not null;

    public bool IsAdmin => _currentUser?.IsAdmin == true;

    public void SignIn(UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _currentUser = user;
    }

    public void SignOut() => _currentUser = null;
}
