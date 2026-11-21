namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Aturan "5 digit terakhir nomor HP penerima" yang diminta sebagian kurir saat cek resi (M2.4).
/// Isian ini opsional: kosong berarti tidak dikirim ke API.
/// </summary>
/// <remarks>Nomor HP adalah data pribadi: pesan error tidak pernah memuat nilai yang diisi.</remarks>
public static class PhoneSuffixRule
{
    public const int Length = 5;
    public const string Field = "NomorHpAkhir";
    public const string ErrorMessage = "Isi 5 digit terakhir nomor HP penerima.";

    /// <summary>Trim; kosong atau hanya spasi → <c>null</c> (tidak dikirim).</summary>
    public static string? Normalize(string? input)
    {
        var trimmed = input?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary><c>true</c> jika tepat 5 digit angka ASCII (0–9).</summary>
    public static bool IsValid(string normalized) =>
        normalized is { Length: Length } && normalized.All(char.IsAsciiDigit);

    /// <summary>Pesan error untuk input pengguna, atau <c>null</c> jika kosong/valid.</summary>
    public static string? GetError(string? input) =>
        Normalize(input) is { } value && !IsValid(value) ? ErrorMessage : null;

    /// <summary>Teks pengganti untuk <c>ToString()</c>/log: tidak pernah memperlihatkan digit asli.</summary>
    public static string Mask(string? input) => Normalize(input) is null ? "(kosong)" : "*****";
}
