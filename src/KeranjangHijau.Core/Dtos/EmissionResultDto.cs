using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Hasil hitung emisi satu paket yang ditampilkan di UI.</summary>
/// <param name="RecordId">ID di <c>emission_records</c>.</param>
/// <param name="ShipmentId">ID di <c>shipments</c>.</param>
/// <param name="NomorResi">Nomor resi bentuk baku.</param>
/// <param name="KodeKurir">Kode kurir huruf kecil.</param>
/// <param name="NamaKurir">Nama kurir untuk ditampilkan.</param>
/// <param name="KotaAsal">Nama kota asal sesuai tabel <c>cities</c>.</param>
/// <param name="KotaTujuan">Nama kota tujuan sesuai tabel <c>cities</c>.</param>
/// <param name="Layanan">Nama layanan; <c>null</c> jika tidak tersedia.</param>
/// <param name="BeratKg">Berat yang dipakai menghitung (kg).</param>
/// <param name="SumberBerat">Asal berat: data resi, manual, atau default.</param>
/// <param name="SumberData">Nilai yang disimpan di <c>shipments.sumber_data</c>.</param>
/// <param name="JarakKm">Jarak tempuh (km), sudah dengan faktor koreksi rute.</param>
/// <param name="ModaTransport">Moda transport hasil tebakan.</param>
/// <param name="TotalCo2eKg">Total emisi (kg CO2e), 4 desimal.</param>
/// <param name="Metode">Cara emisi dihitung.</param>
/// <param name="CalculatedAt">Waktu hitung (UTC).</param>
/// <remarks>
/// Properti tambahan (bukan parameter konstruktor) diisi oleh <see cref="Interfaces.IEmissionRecordedHandler"/>
/// lewat <c>context.Result = context.Result with { ... }</c>.
/// </remarks>
public sealed record EmissionResultDto(
    int RecordId,
    int ShipmentId,
    string NomorResi,
    string KodeKurir,
    string NamaKurir,
    string KotaAsal,
    string KotaTujuan,
    string? Layanan,
    decimal BeratKg,
    WeightSource SumberBerat,
    ShipmentDataSource SumberData,
    decimal JarakKm,
    TransportMode ModaTransport,
    decimal TotalCo2eKg,
    EmissionMethod Metode,
    DateTimeOffset CalculatedAt)
{
    /// <summary>
    /// Konversi setara (CLAUDE.md 7.5), urut <c>equivalence_id</c>. Diisi oleh <c>EquivalenceRecordedHandler</c>
    /// (M4.1); kosong jika handler tidak terpasang atau tabel <c>equivalences</c> kosong.
    /// </summary>
    public IReadOnlyList<EquivalenceDto> KonversiSetara { get; init; } = Array.Empty<EquivalenceDto>();

    /// <summary>
    /// Kondisi hutan pengguna setelah emisi paket ini (CLAUDE.md 7.7). Diisi oleh <c>ForestRecordedHandler</c>
    /// (M5.1); <c>null</c> jika handler tidak terpasang.
    /// </summary>
    public ForestSnapshotDto? Hutan { get; init; }
}
