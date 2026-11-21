using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Data pengguna yang aman dikirim ke UI (tanpa password hash).</summary>
public sealed record UserDto(int UserId, string Nama, string Email, UserRole Role, DateTimeOffset CreatedAt)
{
    public bool IsAdmin => Role == UserRole.Admin;

    public static UserDto FromModel(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserDto(user.UserId, user.Nama, user.Email, user.Role, user.CreatedAt);
    }
}
