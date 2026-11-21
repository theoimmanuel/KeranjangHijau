using System.Globalization;
using System.Text.RegularExpressions;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Membaca berat paket dari data API cek resi menjadi kilogram (M2.4). Format berat asli dari API
/// belum pasti, jadi parser ini toleran: angka (<c>1</c>, <c>1.5</c>) atau teks (<c>"1"</c>,
/// <c>"1,5"</c>, <c>"1 kg"</c>, <c>"1000 gr"</c>, <c>"1000 gram"</c>). Tanpa satuan = kg.
/// </summary>
/// <remarks>
/// Tidak pernah melempar exception. Hasil <c>null</c> (kosong, tidak terbaca, atau ≤ 0) berarti
/// "berat tidak tersedia", sehingga berat manual/default dipakai (CLAUDE.md 7.4). Hanya satu
/// pemisah desimal (titik atau koma) yang diterima; pemisah ribuan seperti <c>"1.000,5"</c> ditolak.
/// </remarks>
public static partial class WeightParser
{
    private const decimal GramsPerKilogram = 1000m;

    public static decimal? Parse(decimal? value) => value is > 0 ? value : null;

    public static decimal? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 50)
        {
            return null;
        }

        var match = WeightPattern().Match(text.Trim());
        if (!match.Success)
        {
            return null;
        }

        var number = match.Groups["num"].Value.Replace(',', '.');
        if (!decimal.TryParse(number, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        var unit = match.Groups["unit"].Value.ToLowerInvariant();
        var kg = unit is "g" or "gr" or "gram" or "grams" ? value / GramsPerKilogram : value;
        return Parse(kg);
    }

    [GeneratedRegex(
        @"^(?<num>[0-9]+(?:[.,][0-9]+)?)\s*(?<unit>kg|kgs|kilo|kilogram|g|gr|gram|grams)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WeightPattern();
}
