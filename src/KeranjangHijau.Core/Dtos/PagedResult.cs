namespace KeranjangHijau.Core.Dtos;

/// <summary>Satu halaman data beserta jumlah seluruh baris yang cocok dengan filter.</summary>
/// <param name="Items">Baris di halaman ini (bisa kosong jika halaman melewati data terakhir).</param>
/// <param name="TotalBaris">Jumlah seluruh baris yang cocok dengan filter (semua halaman).</param>
/// <param name="Halaman">Nomor halaman yang dipakai (≥ 1).</param>
/// <param name="UkuranHalaman">Ukuran halaman yang dipakai (setelah dibatasi).</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalBaris, int Halaman, int UkuranHalaman)
{
    /// <summary>Jumlah halaman; 0 jika tidak ada data.</summary>
    public int TotalHalaman => UkuranHalaman <= 0 ? 0 : (int)(((long)TotalBaris + UkuranHalaman - 1) / UkuranHalaman);

    public bool AdaHalamanSebelumnya => Halaman > 1;

    public bool AdaHalamanBerikutnya => Halaman < TotalHalaman;
}
