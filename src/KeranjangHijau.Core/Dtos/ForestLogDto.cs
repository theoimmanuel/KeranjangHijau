using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Satu baris riwayat hutan untuk UI.</summary>
/// <param name="LogId">ID di <c>forest_logs</c>.</param>
/// <param name="RecordId">Record emisi penyebab penurunan; <c>null</c> untuk pemulihan alami.</param>
/// <param name="Perubahan">Perubahan kesehatan (negatif = turun, positif = pulih).</param>
/// <param name="KesehatanSetelah">Kesehatan setelah perubahan.</param>
/// <param name="Keterangan">Mis. "Emisi paket JNE..." atau "Pemulihan alami".</param>
/// <param name="CreatedAt">Waktu perubahan (UTC).</param>
public sealed record ForestLogDto(
    int LogId,
    int? RecordId,
    decimal Perubahan,
    decimal KesehatanSetelah,
    string Keterangan,
    DateTimeOffset CreatedAt)
{
    /// <summary><c>true</c> untuk log pemulihan alami (tanpa record emisi).</summary>
    public bool IsPemulihan => RecordId is null;

    public static ForestLogDto FromModel(ForestLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new ForestLogDto(log.LogId, log.RecordId, log.Perubahan, log.KesehatanSetelah, log.Keterangan, log.CreatedAt);
    }
}
