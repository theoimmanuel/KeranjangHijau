using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Dtos;

/// <summary>
/// Satu hasil perhitungan emisi di halaman admin (gabungan users, shipments, couriers, cities,
/// emission_records, emission_factors). Hanya untuk dibaca.
/// </summary>
/// <param name="RecordId">ID hasil hitung emisi.</param>
/// <param name="ShipmentId">ID paket.</param>
/// <param name="UserId">ID pemilik paket.</param>
/// <param name="NamaPengguna">Nama pemilik paket.</param>
/// <param name="Email">Email pemilik paket.</param>
/// <param name="NomorResi">Nomor resi bentuk baku.</param>
/// <param name="KodeKurir">Kode kurir, mis. "jne".</param>
/// <param name="NamaKurir">Nama kurir.</param>
/// <param name="KotaAsal">Nama kota asal.</param>
/// <param name="KotaTujuan">Nama kota tujuan.</param>
/// <param name="ModaTransport">Moda yang dipakai saat menghitung.</param>
/// <param name="BeratKg">Berat paket (kg).</param>
/// <param name="JarakKm">Jarak terhitung (km).</param>
/// <param name="TotalCo2eKg">Emisi paket (kg CO2e).</param>
/// <param name="CalculatedAt">Waktu hitung (UTC).</param>
public sealed record AdminRecordDto(
    int RecordId,
    int ShipmentId,
    int UserId,
    string NamaPengguna,
    string Email,
    string NomorResi,
    string KodeKurir,
    string NamaKurir,
    string KotaAsal,
    string KotaTujuan,
    TransportMode ModaTransport,
    decimal BeratKg,
    decimal JarakKm,
    decimal TotalCo2eKg,
    DateTimeOffset CalculatedAt)
{
    /// <summary>Teks rute untuk tabel, mis. "Jakarta → Medan".</summary>
    public string Rute => $"{KotaAsal} → {KotaTujuan}";
}
