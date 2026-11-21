using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Registrasi, login, dan logout.</summary>
public interface IAuthService
{
    /// <summary>
    /// Mendaftarkan akun baru dengan peran <c>pengguna</c> sekaligus membuat hutan awalnya
    /// (dalam satu transaksi). Tidak otomatis login.
    /// </summary>
    /// <exception cref="Exceptions.ValidationException">Input tidak valid atau email sudah terdaftar.</exception>
    Task<UserDto> RegisterAsync(string nama, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Memeriksa email + password, lalu mengisi <see cref="ICurrentUserSession"/>.</summary>
    /// <exception cref="Exceptions.ValidationException">Selalu dengan pesan "Email atau password salah."</exception>
    Task<UserDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Mengosongkan sesi login.</summary>
    void Logout();
}
