namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Penurunan kesehatan hutan karena emisi satu paket (CLAUDE.md 7.7). Aturan murni tanpa I/O.
/// </summary>
/// <remarks>
/// <c>perubahan = -(total_co2e_kg × 10 × PointsPer100GramCo2e)</c>, dibulatkan 2 desimal (<c>AwayFromZero</c>).
/// Faktor 10 mengubah kg ke satuan 100 gram. Kesehatan baru tidak pernah di bawah 0.
/// </remarks>
public static class ForestDamageRule
{
    /// <summary>Jumlah desimal kesehatan dan perubahan (<c>NUMERIC(5,2)</c>/<c>NUMERIC(7,2)</c>).</summary>
    public const int Decimals = 2;

    /// <summary>Jumlah satuan 100 gram dalam 1 kg.</summary>
    public const decimal HundredGramsPerKg = 10m;

    /// <summary>Perubahan nominal (≤ 0) sebelum dibatasi kesehatan minimum.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Emisi atau poin per 100 g negatif.</exception>
    public static decimal CalculateChange(decimal totalCo2eKg, decimal pointsPer100GramCo2e)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCo2eKg);
        ArgumentOutOfRangeException.ThrowIfNegative(pointsPer100GramCo2e);

        var change = -Math.Round(totalCo2eKg * HundredGramsPerKg * pointsPer100GramCo2e, Decimals, MidpointRounding.AwayFromZero);
        return change == 0m ? 0m : change; // hindari "-0"
    }

    /// <summary>
    /// Menerapkan emisi ke <paramref name="currentHealth"/>. <see cref="ForestDamage.Perubahan"/> adalah perubahan
    /// yang <b>benar-benar terjadi</b> (<c>KesehatanBaru − kesehatan lama</c>), jadi bisa lebih kecil dari nilai nominal
    /// saat kesehatan menyentuh 0.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Kesehatan di luar 0–100, atau emisi/poin negatif.</exception>
    public static ForestDamage Apply(decimal currentHealth, decimal totalCo2eKg, decimal pointsPer100GramCo2e)
    {
        if (currentHealth is < ForestStatusRule.MinHealth or > ForestStatusRule.MaxHealth)
        {
            throw new ArgumentOutOfRangeException(nameof(currentHealth), currentHealth, "Kesehatan hutan harus di antara 0 dan 100.");
        }

        var nominal = CalculateChange(totalCo2eKg, pointsPer100GramCo2e);
        var current = Math.Round(currentHealth, Decimals, MidpointRounding.AwayFromZero);
        var newHealth = Math.Max(ForestStatusRule.MinHealth, current + nominal);
        var applied = newHealth - current;

        return new ForestDamage(nominal, applied == 0m ? 0m : applied, newHealth);
    }
}

/// <summary>Hasil <see cref="ForestDamageRule.Apply"/>.</summary>
/// <param name="PerubahanNominal">Perubahan sesuai rumus (≤ 0), sebelum dibatasi minimum 0.</param>
/// <param name="Perubahan">Perubahan yang benar-benar diterapkan (≤ 0); ini yang disimpan di <c>forest_logs</c>.</param>
/// <param name="KesehatanBaru">Kesehatan setelah emisi (0–100, 2 desimal).</param>
public sealed record ForestDamage(decimal PerubahanNominal, decimal Perubahan, decimal KesehatanBaru);
