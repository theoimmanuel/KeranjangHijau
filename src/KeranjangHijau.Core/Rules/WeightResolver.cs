using System.Globalization;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Options;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Implementasi <see cref="IWeightResolver"/> (CLAUDE.md 7.4). Berat terpilih dibulatkan 3 desimal
/// (<c>shipments.berat_kg NUMERIC(6,3)</c>) lalu divalidasi, sehingga nilai yang lolos pasti
/// memenuhi <c>ck_shipments_berat_kg</c>.
/// </summary>
/// <remarks>Aturan murni tanpa I/O; opsi dibaca setiap kali dipanggil.</remarks>
public sealed class WeightResolver : IWeightResolver
{
    public const string BeratKgField = "BeratKg";
    public const string MustBePositiveMessage = "Berat paket harus lebih dari 0 kg.";

    /// <summary>Jumlah desimal berat, sama dengan kolom <c>shipments.berat_kg</c>.</summary>
    public const int Decimals = 3;

    private readonly IOptions<EmissionOptions> _options;

    public WeightResolver(IOptions<EmissionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>Pesan jika berat melebihi batas, mis. "Berat paket maksimal 100 kg." (desimal pakai koma).</summary>
    /// <remarks>Tidak memakai CultureInfo "id-ID" agar tetap jalan di mode invariant globalization.</remarks>
    public static string TooHeavyMessage(decimal maxWeightKg) =>
        $"Berat paket maksimal {maxWeightKg.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', ',')} kg.";

    public ResolvedWeight Resolve(decimal? trackerBeratKg, decimal? manualBeratKg)
    {
        var options = _options.Value;

        var (beratKg, sumber) = (trackerBeratKg, manualBeratKg) switch
        {
            ({ } tracker, _) => (tracker, WeightSource.Api),
            (null, { } manual) => (manual, WeightSource.Manual),
            _ => (options.DefaultWeightKg, WeightSource.Default),
        };

        var rounded = Math.Round(beratKg, Decimals, MidpointRounding.AwayFromZero);

        if (rounded <= 0)
        {
            throw new ValidationException(MustBePositiveMessage, BeratKgField);
        }

        if (rounded > options.MaxWeightKg)
        {
            throw new ValidationException(TooHeavyMessage(options.MaxWeightKg), BeratKgField);
        }

        return new ResolvedWeight(rounded, sumber);
    }
}
