using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Kondisi hutan pengguna untuk halaman hutan di UI (CLAUDE.md 7.7).</summary>
/// <param name="NamaHutan">Nama hutan, mis. "Hutan Budi".</param>
/// <param name="Kesehatan">Kesehatan 0–100 (2 desimal), sudah termasuk pemulihan alami terbaru.</param>
/// <param name="Status">Status hasil <see cref="ForestStatusRule"/>.</param>
/// <param name="JumlahPohonAwal">Jumlah pohon saat hutan dibuat.</param>
/// <param name="PerkiraanPohonTersisa">
/// <c>round(JumlahPohonAwal × Kesehatan / 100)</c> (<see cref="ForestTreeRule"/>).
/// </param>
public sealed record ForestDto(
    string NamaHutan,
    decimal Kesehatan,
    ForestStatus Status,
    int JumlahPohonAwal,
    int PerkiraanPohonTersisa)
{
    /// <summary>Membuat DTO dari model; status dihitung ulang dari kesehatan.</summary>
    public static ForestDto FromModel(Forest forest)
    {
        ArgumentNullException.ThrowIfNull(forest);
        return new ForestDto(
            forest.NamaHutan,
            forest.Kesehatan,
            ForestStatusRule.GetStatus(forest.Kesehatan),
            forest.JumlahPohonAwal,
            ForestTreeRule.EstimateRemainingTrees(forest.JumlahPohonAwal, forest.Kesehatan));
    }
}
