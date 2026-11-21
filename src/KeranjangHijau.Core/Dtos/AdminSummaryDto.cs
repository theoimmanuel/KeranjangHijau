namespace KeranjangHijau.Core.Dtos;

/// <summary>Ringkasan hasil perhitungan emisi semua pengguna untuk filter tertentu (paging diabaikan).</summary>
/// <param name="TotalPengguna">Jumlah pengguna berbeda yang punya paket sesuai filter.</param>
/// <param name="TotalPaket">Jumlah paket sesuai filter.</param>
/// <param name="TotalCo2eKg">Jumlah emisi (kg CO2e).</param>
/// <param name="RataRataCo2ePerPaketKg">Total CO2e ÷ jumlah paket, 4 desimal; 0 jika tidak ada paket.</param>
/// <param name="PerKurir">Total per kurir (semua kurir yang cocok dengan filter kurir, urut <c>courier_id</c>; 0 jika kosong).</param>
public sealed record AdminSummaryDto(
    int TotalPengguna,
    int TotalPaket,
    decimal TotalCo2eKg,
    decimal RataRataCo2ePerPaketKg,
    IReadOnlyList<AdminCourierTotalDto> PerKurir);
