using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Dtos;

/// <summary>
/// Isi kartu Instagram Story (CLAUDE.md 7.9). Backend hanya menyiapkan data; gambar PNG digambar oleh
/// <see cref="Interfaces.IShareCardRenderer"/> di proyek WPF. Angka dikirim apa adanya; format tampilan
/// (koma desimal, pembulatan) diatur renderer.
/// </summary>
/// <param name="NamaPengguna">Nama pengguna yang membagikan.</param>
/// <param name="Periode">Jenis periode laporan.</param>
/// <param name="LabelPeriode">Label periode siap tampil, mis. "November 2026" atau "17–23 November 2026".</param>
/// <param name="JumlahPaket">Jumlah paket di periode (selalu &gt; 0).</param>
/// <param name="TotalCo2eKg">Total emisi periode (kg CO2e).</param>
/// <param name="KonversiUtama">
/// Konversi setara utama = aktivitas pertama (urut <c>equivalence_id</c>) beserta total nilai setaranya;
/// <c>null</c> jika tabel <c>equivalences</c> kosong.
/// </param>
/// <param name="NamaHutan">Nama hutan pengguna.</param>
/// <param name="StatusHutan">Status hutan saat ini.</param>
/// <param name="KesehatanHutan">Kesehatan hutan saat ini (0–100).</param>
/// <param name="KalimatAjakan">Satu kalimat ajakan di bagian bawah kartu.</param>
public sealed record ShareCardData(
    string NamaPengguna,
    ReportPeriod Periode,
    string LabelPeriode,
    int JumlahPaket,
    decimal TotalCo2eKg,
    EquivalenceDto? KonversiUtama,
    string NamaHutan,
    ForestStatus StatusHutan,
    decimal KesehatanHutan,
    string KalimatAjakan)
{
    /// <summary>Lebar kartu Instagram Story (px).</summary>
    public const int DefaultLebarPx = 1080;

    /// <summary>Tinggi kartu Instagram Story (px).</summary>
    public const int DefaultTinggiPx = 1920;

    /// <summary>Kalimat ajakan bawaan.</summary>
    public const string DefaultKalimatAjakan = "Yuk cek jejak karbon paketmu di KeranjangHijau!";

    /// <summary>Lebar kartu (px).</summary>
    public int LebarPx { get; init; } = DefaultLebarPx;

    /// <summary>Tinggi kartu (px).</summary>
    public int TinggiPx { get; init; } = DefaultTinggiPx;
}
