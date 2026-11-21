using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Satu paket di daftar laporan/riwayat (gabungan shipments, emission_records, couriers, cities).</summary>
/// <param name="ShipmentId">ID paket.</param>
/// <param name="RecordId">ID hasil hitung emisi.</param>
/// <param name="NomorResi">Nomor resi bentuk baku.</param>
/// <param name="KodeKurir">Kode kurir, mis. "jne".</param>
/// <param name="NamaKurir">Nama kurir.</param>
/// <param name="KotaAsal">Nama kota asal.</param>
/// <param name="KotaTujuan">Nama kota tujuan.</param>
/// <param name="Layanan">Layanan pengiriman; <c>null</c> jika tidak ada.</param>
/// <param name="BeratKg">Berat paket (kg).</param>
/// <param name="JarakKm">Jarak terhitung (km).</param>
/// <param name="ModaTransport">Moda yang dipakai saat menghitung.</param>
/// <param name="TotalCo2eKg">Emisi paket (kg CO2e).</param>
/// <param name="CreatedAt">Waktu paket dicatat (UTC).</param>
public sealed record ReportShipmentDto(
    int ShipmentId,
    int RecordId,
    string NomorResi,
    string KodeKurir,
    string NamaKurir,
    string KotaAsal,
    string KotaTujuan,
    string? Layanan,
    decimal BeratKg,
    decimal JarakKm,
    TransportMode ModaTransport,
    decimal TotalCo2eKg,
    DateTimeOffset CreatedAt);
