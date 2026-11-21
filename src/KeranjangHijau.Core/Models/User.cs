using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Models;

/// <summary>Akun pengguna atau admin (tabel <c>users</c>).</summary>
public sealed class User
{
    public int UserId { get; set; }

    public string Nama { get; set; } = string.Empty;

    /// <summary>Selalu huruf kecil tanpa spasi di awal/akhir (lihat <c>EmailNormalizer</c>).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash BCrypt; password asli tidak pernah disimpan.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Pengguna;

    /// <summary>Waktu registrasi (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
