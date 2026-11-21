namespace KeranjangHijau.Core.Enums;

/// <summary>Asal data paket yang disimpan (kolom <c>shipments.sumber_data</c>).</summary>
/// <remarks>
/// Di database disimpan sebagai teks <c>'api'</c>/<c>'mock'</c>/<c>'manual'</c>.
/// Konversi selalu lewat <see cref="ShipmentDataSourceExtensions"/>, jangan memakai <c>ToString()</c>.
/// </remarks>
public enum ShipmentDataSource
{
    /// <summary>Data paket dari API cek resi.</summary>
    Api,

    /// <summary>Data paket dari <c>mock-resi.json</c>.</summary>
    Mock,

    /// <summary>Berat paket diisi manual oleh pengguna.</summary>
    Manual,
}

/// <summary>Konversi <see cref="ShipmentDataSource"/> dari/ke teks yang disimpan di database.</summary>
public static class ShipmentDataSourceExtensions
{
    public const string ApiValue = "api";
    public const string MockValue = "mock";
    public const string ManualValue = "manual";

    /// <summary>Teks untuk kolom <c>shipments.sumber_data</c>.</summary>
    public static string ToDbValue(this ShipmentDataSource source) => source switch
    {
        ShipmentDataSource.Api => ApiValue,
        ShipmentDataSource.Mock => MockValue,
        ShipmentDataSource.Manual => ManualValue,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Sumber data paket tidak dikenal."),
    };

    /// <summary>
    /// Mengubah teks dari database ke <see cref="ShipmentDataSource"/>. Tidak peka huruf besar/kecil
    /// dan mengabaikan spasi di awal/akhir.
    /// </summary>
    /// <exception cref="ArgumentException">Teks bukan <c>api</c>, <c>mock</c>, atau <c>manual</c>.</exception>
    public static ShipmentDataSource ParseShipmentDataSource(string value) =>
        TryParseShipmentDataSource(value, out var source)
            ? source
            : throw new ArgumentException($"Sumber data paket '{value}' tidak dikenal.", nameof(value));

    public static bool TryParseShipmentDataSource(string? value, out ShipmentDataSource source)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case ApiValue:
                source = ShipmentDataSource.Api;
                return true;
            case MockValue:
                source = ShipmentDataSource.Mock;
                return true;
            case ManualValue:
                source = ShipmentDataSource.Manual;
                return true;
            default:
                source = default;
                return false;
        }
    }

    /// <summary>
    /// Sumber data yang disimpan (CLAUDE.md M3.3): <c>manual</c> jika berat manual dipakai,
    /// selain itu mengikuti tracker (<c>api</c>/<c>mock</c>).
    /// </summary>
    public static ShipmentDataSource From(TrackingSource trackingSource, WeightSource weightSource) =>
        weightSource == WeightSource.Manual
            ? ShipmentDataSource.Manual
            : trackingSource switch
            {
                TrackingSource.Api => ShipmentDataSource.Api,
                TrackingSource.Mock => ShipmentDataSource.Mock,
                _ => throw new ArgumentOutOfRangeException(nameof(trackingSource), trackingSource, "Sumber tracker tidak dikenal."),
            };
}
