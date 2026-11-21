using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Options;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Implementasi <see cref="ITransportModeResolver"/> (CLAUDE.md 7.3). Layanan dianggap ekspres jika
/// namanya <em>mengandung</em> salah satu <see cref="EmissionOptions.ExpressKeywords"/>, tidak peka
/// huruf besar/kecil. Nama pulau dibandingkan setelah trim, juga tidak peka huruf besar/kecil.
/// </summary>
/// <remarks>Aturan murni tanpa I/O; opsi dibaca setiap kali menebak.</remarks>
public sealed class TransportModeResolver : ITransportModeResolver
{
    private readonly IOptions<EmissionOptions> _options;

    public TransportModeResolver(IOptions<EmissionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public TransportMode Resolve(string pulauAsal, string pulauTujuan, string? layanan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pulauAsal);
        ArgumentException.ThrowIfNullOrWhiteSpace(pulauTujuan);

        if (string.Equals(pulauAsal.Trim(), pulauTujuan.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return TransportMode.Truk;
        }

        return IsExpress(layanan) ? TransportMode.Pesawat : TransportMode.Kapal;
    }

    /// <summary><c>true</c> jika nama layanan mengandung kata kunci ekspres. <c>null</c>/kosong = reguler.</summary>
    public bool IsExpress(string? layanan)
    {
        if (string.IsNullOrWhiteSpace(layanan))
        {
            return false;
        }

        // Daftar kosong hanya terjadi jika opsi dibuat tanpa DI (PostConfigure tidak berjalan).
        IReadOnlyList<string> keywords = _options.Value.ExpressKeywords is { Count: > 0 } configured
            ? configured
            : EmissionOptions.DefaultExpressKeywords;

        return keywords.Any(k =>
            !string.IsNullOrWhiteSpace(k) && layanan.Contains(k.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
