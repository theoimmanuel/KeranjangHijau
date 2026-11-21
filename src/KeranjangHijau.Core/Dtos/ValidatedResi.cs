using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Hasil validasi resi: kurir dari database dan nomor resi bentuk baku.</summary>
public sealed record ValidatedResi(Courier Courier, string NomorResi);
