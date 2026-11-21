using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Pemetaan kesehatan hutan ke status (CLAUDE.md 7.7). Aturan murni tanpa I/O.
/// </summary>
/// <remarks>
/// Batas bawah setiap status bersifat inklusif: 80–100 Lestari, 60–79,99 Sehat, 40–59,99 Menipis,
/// 20–39,99 Terancam, 0–19,99 Kritis. Batas sama dengan tabel di CLAUDE.md, jadi ditulis sebagai konstanta.
/// </remarks>
public static class ForestStatusRule
{
    public const decimal MinHealth = 0m;
    public const decimal MaxHealth = 100m;

    public const decimal LestariMin = 80m;
    public const decimal SehatMin = 60m;
    public const decimal MenipisMin = 40m;
    public const decimal TerancamMin = 20m;

    /// <summary>Status untuk <paramref name="kesehatan"/> (0–100).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Kesehatan di luar 0–100 (kesalahan pengembang).</exception>
    public static ForestStatus GetStatus(decimal kesehatan)
    {
        if (kesehatan is < MinHealth or > MaxHealth)
        {
            throw new ArgumentOutOfRangeException(nameof(kesehatan), kesehatan, "Kesehatan hutan harus di antara 0 dan 100.");
        }

        return kesehatan switch
        {
            >= LestariMin => ForestStatus.Lestari,
            >= SehatMin => ForestStatus.Sehat,
            >= MenipisMin => ForestStatus.Menipis,
            >= TerancamMin => ForestStatus.Terancam,
            _ => ForestStatus.Kritis,
        };
    }
}
