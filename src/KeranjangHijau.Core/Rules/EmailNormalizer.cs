namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Bentuk baku email: tanpa spasi di awal/akhir dan huruf kecil. Dipakai sebelum email
/// disimpan maupun dicari, karena <c>uq_users_email</c> peka huruf besar/kecil.
/// </summary>
public static class EmailNormalizer
{
    public static string Normalize(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToLowerInvariant();
    }
}
