namespace KeranjangHijau.Core.Dtos;

/// <summary>
/// Filter halaman admin untuk hasil perhitungan emisi semua pengguna (M7.1). Semua kriteria opsional;
/// kriteria yang kosong tidak membatasi hasil. Dinormalisasi oleh <see cref="Rules.AdminFilterRule"/>.
/// </summary>
public sealed record AdminRecordFilter
{
    /// <summary>Ukuran halaman jika tidak diisi (atau ≤ 0).</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Ukuran halaman terbesar; nilai lebih besar dibatasi ke angka ini.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Tanggal kalender WIB pertama (inklusif) berdasarkan waktu hitung; <c>null</c> = tanpa batas awal.</summary>
    public DateOnly? TanggalMulai { get; init; }

    /// <summary>Tanggal kalender WIB terakhir (inklusif) berdasarkan waktu hitung; <c>null</c> = tanpa batas akhir.</summary>
    public DateOnly? TanggalSelesai { get; init; }

    /// <summary>Kode kurir, mis. "jne" (tidak peka huruf besar/kecil); kosong = semua kurir.</summary>
    public string? KodeKurir { get; init; }

    /// <summary>Potongan nama atau email pengguna (tidak peka huruf besar/kecil); kosong = semua pengguna.</summary>
    public string? KataKunci { get; init; }

    /// <summary>Nomor halaman, mulai 1. Nilai &lt; 1 dianggap 1.</summary>
    public int Halaman { get; init; } = 1;

    /// <summary>Jumlah baris per halaman: default <see cref="DefaultPageSize"/>, maksimum <see cref="MaxPageSize"/>.</summary>
    public int UkuranHalaman { get; init; } = DefaultPageSize;
}
