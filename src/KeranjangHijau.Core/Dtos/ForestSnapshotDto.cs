using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Kondisi hutan pengguna setelah suatu perubahan, untuk ditampilkan di UI.</summary>
/// <param name="Kesehatan">Kesehatan setelah perubahan (0–100, 2 desimal).</param>
/// <param name="Status">Status hasil <see cref="Rules.ForestStatusRule"/>.</param>
/// <param name="Perubahan">Perubahan yang diterapkan (negatif = turun), 2 desimal.</param>
public sealed record ForestSnapshotDto(decimal Kesehatan, ForestStatus Status, decimal Perubahan);
