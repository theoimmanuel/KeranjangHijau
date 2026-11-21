using System.Text;
using System.Text.RegularExpressions;

namespace KeranjangHijau.Core.Rules;

/// <summary>Aturan murni untuk data akun saat registrasi (tanpa I/O).</summary>
public static partial class CredentialRules
{
    /// <summary>Sesuai kolom <c>users.nama VARCHAR(100)</c>.</summary>
    public const int MaxNamaLength = 100;

    /// <summary>Sesuai kolom <c>users.email VARCHAR(254)</c>.</summary>
    public const int MaxEmailLength = 254;

    public const int MinPasswordLength = 8;

    /// <summary>BCrypt hanya memakai 72 byte pertama; password lebih panjang ditolak agar tidak terpotong diam-diam.</summary>
    public const int MaxPasswordBytes = 72;

    /// <summary>
    /// Format email sederhana: bagian lokal, '@', domain dengan minimal satu titik dan akhiran
    /// huruf minimal 2 karakter. Dipanggil setelah email dinormalisasi.
    /// </summary>
    public static bool IsValidEmail(string? email) =>
        !string.IsNullOrEmpty(email) && email.Length <= MaxEmailLength && EmailRegex().IsMatch(email);

    /// <summary>Minimal 8 karakter, berisi minimal satu huruf dan satu angka.</summary>
    public static bool IsStrongPassword(string? password) =>
        !string.IsNullOrEmpty(password)
        && password.Length >= MinPasswordLength
        && password.Any(char.IsLetter)
        && password.Any(char.IsDigit);

    public static bool IsPasswordTooLong(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes;
    }

    [GeneratedRegex(@"^[a-z0-9._%+\-]+@[a-z0-9\-]+(\.[a-z0-9\-]+)*\.[a-z]{2,}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
