namespace KeranjangHijau.Core.Dtos;

/// <summary>Total paket dan emisi satu kurir di ringkasan admin.</summary>
public sealed record AdminCourierTotalDto(string KodeKurir, string NamaKurir, int JumlahPaket, decimal TotalCo2eKg);
