namespace KeranjangHijau.Core.Dtos;

/// <summary>
/// <see cref="AdminRecordFilter"/> yang sudah dinormalisasi (hasil <see cref="Rules.AdminFilterRule.Normalize"/>),
/// siap dipakai repository. Rentang waktu setengah terbuka [<see cref="FromUtc"/>, <see cref="ToUtc"/>)
/// pada <c>emission_records.calculated_at</c>.
/// </summary>
/// <param name="FromUtc">Batas awal inklusif (UTC); <c>null</c> = tanpa batas awal.</param>
/// <param name="ToUtc">Batas akhir eksklusif (UTC); <c>null</c> = tanpa batas akhir.</param>
/// <param name="KodeKurir">Kode kurir huruf kecil; <c>null</c> = semua kurir.</param>
/// <param name="KataKunci">Kata kunci nama/email yang sudah di-trim; <c>null</c> = semua pengguna.</param>
/// <param name="Halaman">Nomor halaman (≥ 1).</param>
/// <param name="UkuranHalaman">Baris per halaman (1–<see cref="AdminRecordFilter.MaxPageSize"/>).</param>
public sealed record AdminRecordQuery(
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? KodeKurir,
    string? KataKunci,
    int Halaman,
    int UkuranHalaman)
{
    /// <summary>Jumlah baris yang dilewati (<c>long</c>, agar halaman sangat besar tidak overflow).</summary>
    public long Offset => (long)(Halaman - 1) * UkuranHalaman;
}
