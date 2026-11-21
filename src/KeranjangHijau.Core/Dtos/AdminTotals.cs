namespace KeranjangHijau.Core.Dtos;

/// <summary>Total mentah dari repository untuk ringkasan admin (sebelum rata-rata dihitung).</summary>
/// <param name="TotalPengguna">Jumlah pengguna berbeda yang punya paket sesuai filter.</param>
/// <param name="TotalPaket">Jumlah paket (= jumlah hasil hitung) sesuai filter.</param>
/// <param name="TotalCo2eKg">Jumlah emisi (kg CO2e) sesuai filter.</param>
public sealed record AdminTotals(int TotalPengguna, int TotalPaket, decimal TotalCo2eKg);
