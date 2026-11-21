using System.Text;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Mencocokkan nama kota dari tracker (mis. API: <c>"KOTA ADM. JAKARTA BARAT"</c>) ke daftar
/// kota di tabel <c>cities</c> (M2.4). Urutan percobaan:
/// <list type="number">
///   <item>Sama persis (tidak peka huruf besar/kecil, spasi berlebih diabaikan).</item>
///   <item>Ambil bagian sebelum koma pertama, buang awalan <c>KOTA ADM.</c>/<c>KOTA</c>/<c>KAB.</c>/<c>KABUPATEN</c>,
///         lalu bandingkan persis lagi.</item>
///   <item>Nama kota di tabel muncul sebagai kata utuh di teks hasil langkah 2
///         (<c>"JAKARTA BARAT"</c> → Jakarta). Jika beberapa cocok, dipilih nama terpanjang.</item>
/// </list>
/// Jika ada beberapa kota dengan nama yang sama, dipilih <c>CityId</c> terkecil
/// (sama dengan <c>ICityRepository.GetByNameAsync</c>).
/// </summary>
public static class CityNameMatcher
{
    // Urutan penting: awalan yang lebih panjang dicek lebih dulu.
    private static readonly string[] Prefixes = ["KOTA ADM.", "KABUPATEN", "KAB.", "KOTA"];

    /// <returns>Kota yang cocok, atau <c>null</c>.</returns>
    public static City? Match(string? namaKota, IEnumerable<City> cities)
    {
        ArgumentNullException.ThrowIfNull(cities);

        var input = Normalize(namaKota);
        if (input.Length == 0)
        {
            return null;
        }

        var candidates = cities
            .Where(c => !string.IsNullOrWhiteSpace(c.NamaKota))
            .Select(c => (City: c, Name: Normalize(c.NamaKota)))
            .OrderBy(c => c.City.CityId)
            .ToList();

        // a. Persis.
        var exact = candidates.FirstOrDefault(c => c.Name == input).City;
        if (exact is not null)
        {
            return exact;
        }

        // b. Tanpa awalan administratif, bagian sebelum koma.
        var cleaned = StripPrefix(CutAtComma(input));
        if (cleaned.Length == 0)
        {
            return null;
        }

        var exactCleaned = candidates.FirstOrDefault(c => c.Name == cleaned).City;
        if (exactCleaned is not null)
        {
            return exactCleaned;
        }

        // c. Nama kota sebagai kata utuh; nama terpanjang menang (mis. "Bandar Lampung" > "Lampung").
        var words = Words(cleaned);
        return candidates
            .Where(c => ContainsWordSequence(words, Words(c.Name)))
            .OrderByDescending(c => c.Name.Length)
            .ThenBy(c => c.City.CityId)
            .Select(c => c.City)
            .FirstOrDefault();
    }

    /// <summary>Trim, huruf besar, dan spasi berlebih menjadi satu spasi.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var part in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(part.ToUpperInvariant());
        }

        return builder.ToString();
    }

    private static string CutAtComma(string text)
    {
        var comma = text.IndexOf(',');
        return (comma < 0 ? text : text[..comma]).Trim();
    }

    private static string StripPrefix(string text)
    {
        foreach (var prefix in Prefixes)
        {
            if (!text.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var rest = text[prefix.Length..];
            // Awalan berupa kata ("KOTA", "KABUPATEN") harus diikuti spasi, supaya "KOTABARU" tidak terpotong.
            if (prefix.EndsWith('.') || rest.Length == 0 || rest[0] == ' ')
            {
                return rest.Trim();
            }
        }

        return text;
    }

    /// <summary>Kata-kata dalam teks: urutan huruf/angka, dipisah karakter lain (spasi, titik, tanda hubung).</summary>
    private static string[] Words(string text)
    {
        var words = new List<string>();
        var start = -1;
        for (var i = 0; i <= text.Length; i++)
        {
            var isSeparator = i == text.Length || !char.IsLetterOrDigit(text[i]);
            if (!isSeparator && start < 0)
            {
                start = i;
            }
            else if (isSeparator && start >= 0)
            {
                words.Add(text[start..i]);
                start = -1;
            }
        }

        return [.. words];
    }

    private static bool ContainsWordSequence(string[] haystack, string[] needle)
    {
        if (needle.Length == 0 || needle.Length > haystack.Length)
        {
            return false;
        }

        for (var start = 0; start <= haystack.Length - needle.Length; start++)
        {
            if (haystack.AsSpan(start, needle.Length).SequenceEqual(needle))
            {
                return true;
            }
        }

        return false;
    }
}
