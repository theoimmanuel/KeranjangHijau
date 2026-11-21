namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Bentuk baku nomor resi (CLAUDE.md 7.6): semua spasi/whitespace dibuang dan diubah ke
/// huruf besar. Contoh: <c>" jp 1234 5678 "</c> → <c>"JP12345678"</c>.
/// </summary>
public static class ResiNormalizer
{
    public static string Normalize(string nomorResi)
    {
        ArgumentNullException.ThrowIfNull(nomorResi);
        return string.Concat(nomorResi.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
    }
}
