namespace KeranjangHijau.Core.Dtos;

/// <summary>Total emisi satu hari (tanggal kalender WIB) untuk grafik laporan.</summary>
/// <param name="Tanggal">Tanggal WIB.</param>
/// <param name="JumlahPaket">Jumlah paket yang dicatat pada tanggal itu.</param>
/// <param name="TotalCo2eKg">Total emisi pada tanggal itu (kg CO2e).</param>
public sealed record DailyEmissionDto(DateOnly Tanggal, int JumlahPaket, decimal TotalCo2eKg);
