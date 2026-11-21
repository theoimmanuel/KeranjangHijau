namespace KeranjangHijau.Core.Enums;

/// <summary>Moda transport pengiriman yang ditebak dari pulau dan layanan (CLAUDE.md 7.3).</summary>
/// <remarks>
/// Di database disimpan sebagai teks <c>'truk'</c>/<c>'kapal'</c>/<c>'pesawat'</c>
/// (kolom <c>emission_factors.moda_transport</c>). Konversi selalu lewat
/// <see cref="TransportModeExtensions"/>, jangan memakai <c>ToString()</c>.
/// </remarks>
public enum TransportMode
{
    Truk,
    Kapal,
    Pesawat,
}

/// <summary>Konversi <see cref="TransportMode"/> dari/ke teks yang disimpan di database.</summary>
public static class TransportModeExtensions
{
    public const string TrukValue = "truk";
    public const string KapalValue = "kapal";
    public const string PesawatValue = "pesawat";

    /// <summary>Teks untuk kolom <c>emission_factors.moda_transport</c>.</summary>
    public static string ToDbValue(this TransportMode mode) => mode switch
    {
        TransportMode.Truk => TrukValue,
        TransportMode.Kapal => KapalValue,
        TransportMode.Pesawat => PesawatValue,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Moda transport tidak dikenal."),
    };

    /// <summary>
    /// Mengubah teks dari database ke <see cref="TransportMode"/>. Tidak peka huruf besar/kecil
    /// dan mengabaikan spasi di awal/akhir.
    /// </summary>
    /// <exception cref="ArgumentException">Teks bukan <c>truk</c>, <c>kapal</c>, atau <c>pesawat</c>.</exception>
    public static TransportMode ParseTransportMode(string value) =>
        TryParseTransportMode(value, out var mode)
            ? mode
            : throw new ArgumentException($"Moda transport '{value}' tidak dikenal.", nameof(value));

    public static bool TryParseTransportMode(string? value, out TransportMode mode)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case TrukValue:
                mode = TransportMode.Truk;
                return true;
            case KapalValue:
                mode = TransportMode.Kapal;
                return true;
            case PesawatValue:
                mode = TransportMode.Pesawat;
                return true;
            default:
                mode = default;
                return false;
        }
    }
}
