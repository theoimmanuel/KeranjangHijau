using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Satu aktivitas pembanding beserta nilai setaranya untuk satu hasil emisi.</summary>
/// <param name="Equivalence">Baris <c>equivalences</c> yang dipakai.</param>
/// <param name="NilaiSetara">Nilai setara (2 desimal).</param>
public sealed record EquivalenceValue(Equivalence Equivalence, decimal NilaiSetara)
{
    /// <summary>Bentuk untuk UI.</summary>
    public EquivalenceDto ToDto() => new(
        Equivalence.Aktivitas,
        Equivalence.Satuan,
        NilaiSetara,
        Equivalence.Ikon,
        Equivalence.IsPlaceholder);

    /// <summary>Baris <c>record_equivalences</c> untuk record emisi <paramref name="recordId"/>.</summary>
    public RecordEquivalence ToRecordEquivalence(int recordId) => new()
    {
        RecordId = recordId,
        EquivalenceId = Equivalence.EquivalenceId,
        NilaiSetara = NilaiSetara,
    };
}
