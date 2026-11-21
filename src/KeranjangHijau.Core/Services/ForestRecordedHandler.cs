using System.Data;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Core.Services;

/// <summary>
/// Penurunan kesehatan hutan (M5.1), dipasang ke titik perluasan <see cref="IEmissionRecordedHandler"/> setelah
/// konversi setara: menerapkan emisi ke hutan pengguna lewat <see cref="IForestService"/> di transaksi hitung emisi,
/// lalu mengisi <see cref="Dtos.EmissionResultDto.Hutan"/>.
/// </summary>
public sealed class ForestRecordedHandler : IEmissionRecordedHandler
{
    private readonly IForestService _forestService;

    public ForestRecordedHandler(IForestService forestService)
    {
        _forestService = forestService ?? throw new ArgumentNullException(nameof(forestService));
    }

    public async Task HandleAsync(
        EmissionRecordedContext context, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(transaction);

        var snapshot = await _forestService.ApplyEmissionAsync(
            context.User.UserId,
            context.Record.RecordId,
            context.Shipment.NomorResi,
            context.Record.TotalCo2eKg,
            transaction,
            cancellationToken).ConfigureAwait(false);

        context.Result = context.Result with { Hutan = snapshot };
    }
}
