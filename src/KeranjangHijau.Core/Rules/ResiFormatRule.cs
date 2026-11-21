namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Aturan format nomor resi yang <em>sudah dinormalisasi</em> (CLAUDE.md 7.6):
/// wajib diisi, 8–30 karakter, hanya huruf A–Z dan angka 0–9.
/// Sama dengan CHECK <c>ck_shipments_nomor_resi</c> di database.
/// </summary>
public static class ResiFormatRule
{
    public const int MinLength = 8;
    public const int MaxLength = 30;

    public const string RequiredMessage = "Nomor resi wajib diisi.";
    public const string LengthMessage = "Nomor resi harus 8–30 karakter.";
    public const string CharactersMessage = "Nomor resi hanya boleh berisi huruf dan angka.";

    /// <summary>Pesan error pertama yang ditemukan, atau <c>null</c> jika format valid.</summary>
    public static string? GetError(string normalizedResi)
    {
        ArgumentNullException.ThrowIfNull(normalizedResi);

        if (normalizedResi.Length == 0)
        {
            return RequiredMessage;
        }

        if (!normalizedResi.All(IsAllowedChar))
        {
            return CharactersMessage;
        }

        if (normalizedResi.Length is < MinLength or > MaxLength)
        {
            return LengthMessage;
        }

        return null;
    }

    // Hanya ASCII, karena char.IsLetterOrDigit menerima huruf non-Latin yang ditolak CHECK di DB.
    private static bool IsAllowedChar(char c) => c is (>= 'A' and <= 'Z') or (>= '0' and <= '9');
}
