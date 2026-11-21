namespace KeranjangHijau.Core.Enums;

/// <summary>Peran akun (kolom <c>users.role</c>, CLAUDE.md bagian 2).</summary>
/// <remarks>
/// Di database disimpan sebagai teks <c>'pengguna'</c>/<c>'admin'</c>.
/// Konversi selalu lewat <see cref="UserRoleExtensions"/>, jangan memakai <c>ToString()</c>.
/// </remarks>
public enum UserRole
{
    Pengguna,
    Admin,
}

/// <summary>Konversi <see cref="UserRole"/> dari/ke teks yang disimpan di database.</summary>
public static class UserRoleExtensions
{
    public const string PenggunaValue = "pengguna";
    public const string AdminValue = "admin";

    /// <summary>Teks untuk kolom <c>users.role</c>.</summary>
    public static string ToDbValue(this UserRole role) => role switch
    {
        UserRole.Pengguna => PenggunaValue,
        UserRole.Admin => AdminValue,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Peran pengguna tidak dikenal."),
    };

    /// <summary>
    /// Mengubah teks dari database ke <see cref="UserRole"/>. Tidak peka huruf besar/kecil
    /// dan mengabaikan spasi di awal/akhir.
    /// </summary>
    /// <exception cref="ArgumentException">Teks bukan <c>pengguna</c> atau <c>admin</c>.</exception>
    public static UserRole ParseUserRole(string value) =>
        TryParseUserRole(value, out var role)
            ? role
            : throw new ArgumentException($"Peran pengguna '{value}' tidak dikenal.", nameof(value));

    public static bool TryParseUserRole(string? value, out UserRole role)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case PenggunaValue:
                role = UserRole.Pengguna;
                return true;
            case AdminValue:
                role = UserRole.Admin;
                return true;
            default:
                role = default;
                return false;
        }
    }
}
