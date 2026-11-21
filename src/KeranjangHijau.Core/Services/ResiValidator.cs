using System.Data;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Core.Services;

/// <summary>
/// Implementasi <see cref="IResiValidator"/>. Urutan cek: format resi → kurir dikenal →
/// duplikat (dilewati jika <see cref="FeatureOptions.AllowDuplicateResi"/> = <c>true</c>).
/// </summary>
public sealed class ResiValidator : IResiValidator
{
    public const string CourierRequiredMessage = "Kurir wajib dipilih.";
    public const string CourierUnknownMessage = "Kurir tidak dikenal.";
    public const string NomorResiField = "NomorResi";
    public const string KodeKurirField = "KodeKurir";

    private readonly ICourierRepository _couriers;
    private readonly IShipmentRepository _shipments;
    private readonly IOptions<FeatureOptions> _features;

    public ResiValidator(ICourierRepository couriers, IShipmentRepository shipments, IOptions<FeatureOptions> features)
    {
        ArgumentNullException.ThrowIfNull(couriers);
        ArgumentNullException.ThrowIfNull(shipments);
        ArgumentNullException.ThrowIfNull(features);
        _couriers = couriers;
        _shipments = shipments;
        _features = features;
    }

    public async Task<ValidatedResi> ValidateAsync(
        string? kodeKurir,
        string? nomorResi,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedResi = ResiNormalizer.Normalize(nomorResi ?? string.Empty);
        var formatError = ResiFormatRule.GetError(normalizedResi);
        if (formatError is not null)
        {
            throw new ValidationException(formatError, NomorResiField);
        }

        if (string.IsNullOrWhiteSpace(kodeKurir))
        {
            throw new ValidationException(CourierRequiredMessage, KodeKurirField);
        }

        var courier = await _couriers.GetByKodeAsync(kodeKurir, transaction, cancellationToken).ConfigureAwait(false)
            ?? throw new ValidationException(CourierUnknownMessage, KodeKurirField);

        // Mode development (CLAUDE.md 7.6): resi yang sama boleh diinput berkali-kali.
        if (!_features.Value.AllowDuplicateResi
            && await _shipments.ExistsAsync(courier.CourierId, normalizedResi, transaction, cancellationToken).ConfigureAwait(false))
        {
            throw new DuplicateResiException(courier.KodeKurir, normalizedResi);
        }

        return new ValidatedResi(courier, normalizedResi);
    }
}
