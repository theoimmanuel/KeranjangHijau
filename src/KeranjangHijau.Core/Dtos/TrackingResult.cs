using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Dtos;

/// <summary>
/// Data paket hasil pelacakan resi. Nama kota masih teks dari tracker (bisa berformat API,
/// mis. <c>"JAKARTA BARAT"</c>) dan dicocokkan ke tabel <c>cities</c> lewat <c>CityNameMatcher</c>.
/// </summary>
/// <param name="NomorResi">Nomor resi bentuk baku (tanpa spasi, huruf besar).</param>
/// <param name="KodeKurir">Kode kurir huruf kecil, mis. <c>jne</c>.</param>
/// <param name="Layanan">Nama layanan (mis. <c>REG</c>, <c>YES</c>); <c>null</c> jika tidak tersedia (dianggap reguler).</param>
/// <param name="BeratKg">Berat dari data resi; <c>null</c> jika tidak tersedia (CLAUDE.md 7.4).</param>
public sealed record TrackingResult(
    string NomorResi,
    string KodeKurir,
    string KotaAsal,
    string KotaTujuan,
    string? Layanan,
    decimal? BeratKg,
    TrackingSource Sumber);
