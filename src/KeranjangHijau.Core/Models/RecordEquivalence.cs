namespace KeranjangHijau.Core.Models;

/// <summary>Nilai setara satu hasil emisi untuk satu aktivitas (tabel <c>record_equivalences</c>).</summary>
public sealed class RecordEquivalence
{
    public int RecordId { get; set; }

    public int EquivalenceId { get; set; }

    /// <summary>Hasil <c>total_co2e_kg / kg_co2e_per_satuan</c>, dibulatkan 2 desimal (selalu ≥ 0).</summary>
    public decimal NilaiSetara { get; set; }
}
