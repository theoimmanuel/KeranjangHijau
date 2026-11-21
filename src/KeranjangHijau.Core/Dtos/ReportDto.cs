using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Laporan emisi satu periode untuk pengguna yang sedang login (CLAUDE.md 7.8).</summary>
/// <param name="Periode">Jenis periode.</param>
/// <param name="PeriodeMulai">
/// Awal periode (UTC, inklusif) = 00.00 WIB <paramref name="TanggalMulai"/>. Untuk <see cref="ReportPeriod.Semua"/>
/// diisi dari paket tertua (atau tanggal acuan jika belum ada paket).
/// </param>
/// <param name="PeriodeSelesai">Akhir periode (UTC, <b>eksklusif</b>) = 00.00 WIB sehari setelah <paramref name="TanggalSelesai"/>.</param>
/// <param name="TanggalMulai">Hari pertama periode (WIB) untuk ditampilkan.</param>
/// <param name="TanggalSelesai">Hari terakhir periode (WIB, inklusif) untuk ditampilkan.</param>
/// <param name="JumlahPaket">Jumlah paket di <paramref name="Paket"/>.</param>
/// <param name="TotalCo2eKg">Jumlah emisi semua paket di <paramref name="Paket"/> (kg CO2e).</param>
/// <param name="Paket">Daftar paket, terbaru lebih dulu. Kosong jika tidak ada data.</param>
/// <param name="DataHarian">
/// Total per hari, urut tanggal. Untuk mingguan/bulanan berisi setiap hari di periode (hari tanpa paket bernilai 0);
/// untuk <see cref="ReportPeriod.Semua"/> hanya hari yang ada paketnya.
/// </param>
/// <param name="KonversiSetara">
/// Total nilai setara per aktivitas (jumlah <c>record_equivalences.nilai_setara</c>), urut <c>equivalence_id</c>;
/// aktivitas tanpa data bernilai 0.
/// </param>
/// <param name="Hutan">Kondisi hutan saat ini (sudah termasuk pemulihan alami terbaru).</param>
public sealed record ReportDto(
    ReportPeriod Periode,
    DateTimeOffset PeriodeMulai,
    DateTimeOffset PeriodeSelesai,
    DateOnly TanggalMulai,
    DateOnly TanggalSelesai,
    int JumlahPaket,
    decimal TotalCo2eKg,
    IReadOnlyList<ReportShipmentDto> Paket,
    IReadOnlyList<DailyEmissionDto> DataHarian,
    IReadOnlyList<EquivalenceDto> KonversiSetara,
    ForestDto Hutan)
{
    /// <summary><c>true</c> jika tidak ada paket di periode ini.</summary>
    public bool IsEmpty => JumlahPaket == 0;
}
