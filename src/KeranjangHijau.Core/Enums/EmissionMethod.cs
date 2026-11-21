namespace KeranjangHijau.Core.Enums;

/// <summary>Cara emisi dihitung (kolom <c>emission_records.metode</c>).</summary>
/// <remarks>
/// Di database disimpan sebagai teks <c>'api'</c>/<c>'rumus_lokal'</c>.
/// Konversi selalu lewat <see cref="EmissionMethodExtensions"/>, jangan memakai <c>ToString()</c>.
/// </remarks>
public enum EmissionMethod
{
    /// <summary>Emisi diambil dari API kalkulator karbon pihak ketiga (belum dipakai).</summary>
    Api,

    /// <summary>Emisi dihitung dengan rumus lokal CLAUDE.md 7.1.</summary>
    RumusLokal,
}

/// <summary>Konversi <see cref="EmissionMethod"/> dari/ke teks yang disimpan di database.</summary>
public static class EmissionMethodExtensions
{
    public const string ApiValue = "api";
    public const string RumusLokalValue = "rumus_lokal";

    /// <summary>Teks untuk kolom <c>emission_records.metode</c>.</summary>
    public static string ToDbValue(this EmissionMethod method) => method switch
    {
        EmissionMethod.Api => ApiValue,
        EmissionMethod.RumusLokal => RumusLokalValue,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Metode perhitungan tidak dikenal."),
    };

    /// <summary>
    /// Mengubah teks dari database ke <see cref="EmissionMethod"/>. Tidak peka huruf besar/kecil
    /// dan mengabaikan spasi di awal/akhir.
    /// </summary>
    /// <exception cref="ArgumentException">Teks bukan <c>api</c> atau <c>rumus_lokal</c>.</exception>
    public static EmissionMethod ParseEmissionMethod(string value) =>
        TryParseEmissionMethod(value, out var method)
            ? method
            : throw new ArgumentException($"Metode perhitungan '{value}' tidak dikenal.", nameof(value));

    public static bool TryParseEmissionMethod(string? value, out EmissionMethod method)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case ApiValue:
                method = EmissionMethod.Api;
                return true;
            case RumusLokalValue:
                method = EmissionMethod.RumusLokal;
                return true;
            default:
                method = default;
                return false;
        }
    }
}
