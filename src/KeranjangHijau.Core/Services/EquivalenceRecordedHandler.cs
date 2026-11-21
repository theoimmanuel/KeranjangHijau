using System.Data;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Core.Services;

/// <summary>
/// Konversi setara (M4.1), dipasang ke titik perluasan <see cref="IEmissionRecordedHandler"/>: menghitung nilai
/// setara dari total emisi, menyimpannya ke <c>record_equivalences</c> di transaksi hitung emisi, lalu menambahkan
/// daftarnya ke <see cref="Dtos.EmissionResultDto.KonversiSetara"/>.
/// </summary>
/// <remarks>Jika tabel <c>equivalences</c> kosong, tidak ada yang disimpan dan daftarnya kosong.</remarks>
public sealed class EquivalenceRecordedHandler : IEmissionRecordedHandler
{
    private readonly IEquivalenceRepository _equivalences;
    private readonly IEquivalenceCalculator _calculator;

    public EquivalenceRecordedHandler(IEquivalenceRepository equivalences, IEquivalenceCalculator calculator)
    {
        _equivalences = equivalences ?? throw new ArgumentNullException(nameof(equivalences));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    public async Task HandleAsync(
        EmissionRecordedContext context, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(transaction);

        var equivalences = await _equivalences.GetAllAsync(transaction, cancellationToken).ConfigureAwait(false);
        var values = _calculator.Calculate(context.Record.TotalCo2eKg, equivalences);

        var rows = values.Select(v => v.ToRecordEquivalence(context.Record.RecordId)).ToList();
        await _equivalences.InsertRecordEquivalencesAsync(rows, transaction, cancellationToken).ConfigureAwait(false);

        context.Result = context.Result with { KonversiSetara = values.Select(v => v.ToDto()).ToList() };
    }
}
