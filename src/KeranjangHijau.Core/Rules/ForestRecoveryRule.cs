namespace KeranjangHijau.Core.Rules;

/// <summary>
/// Pemulihan alami hutan (CLAUDE.md 7.7). Aturan murni tanpa I/O: waktu sekarang dikirim oleh pemanggil
/// (dari <see cref="Interfaces.IClock"/>).
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><c>hari_penuh</c> = jumlah hari penuh (24 jam) sejak <c>updated_at</c>.</item>
///   <item>Pemulihan = <c>hari_penuh × RecoveryPointsPerDay</c>, kesehatan maksimum 100, dibulatkan 2 desimal.</item>
///   <item><c>updated_at</c> dimajukan tepat <c>hari_penuh</c> hari (bukan ke sekarang) agar sisa jam tetap terhitung.</item>
///   <item>Kesehatan sudah 100 → kesehatan tetap, <c>updated_at</c> = sekarang (hari saat hutan penuh tidak ditabung).</item>
///   <item><c>hari_penuh</c> = 0 → tidak ada perubahan sama sekali.</item>
/// </list>
/// </remarks>
public static class ForestRecoveryRule
{
    /// <summary>Menghitung pemulihan untuk hutan dengan kesehatan dan <c>updated_at</c> tertentu.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Kesehatan di luar 0–100, atau poin per hari negatif.</exception>
    public static ForestRecovery Calculate(
        decimal currentHealth, DateTimeOffset updatedAt, DateTimeOffset now, decimal recoveryPointsPerDay)
    {
        if (currentHealth is < ForestStatusRule.MinHealth or > ForestStatusRule.MaxHealth)
        {
            throw new ArgumentOutOfRangeException(nameof(currentHealth), currentHealth, "Kesehatan hutan harus di antara 0 dan 100.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(recoveryPointsPerDay);

        var current = Math.Round(currentHealth, ForestDamageRule.Decimals, MidpointRounding.AwayFromZero);
        var fullDays = FullDaysBetween(updatedAt, now);

        if (current >= ForestStatusRule.MaxHealth)
        {
            return new ForestRecovery(fullDays, 0m, current, now);
        }

        if (fullDays == 0)
        {
            return new ForestRecovery(0, 0m, current, updatedAt);
        }

        var recovered = Math.Round(
            current + (fullDays * recoveryPointsPerDay), ForestDamageRule.Decimals, MidpointRounding.AwayFromZero);
        var newHealth = Math.Min(ForestStatusRule.MaxHealth, recovered);

        return new ForestRecovery(fullDays, newHealth - current, newHealth, updatedAt.AddDays(fullDays));
    }

    /// <summary>Jumlah hari penuh (24 jam) dari <paramref name="from"/> ke <paramref name="to"/>; 0 jika negatif.</summary>
    public static int FullDaysBetween(DateTimeOffset from, DateTimeOffset to)
    {
        var elapsed = to - from;
        return elapsed <= TimeSpan.Zero ? 0 : (int)(elapsed.Ticks / TimeSpan.TicksPerDay);
    }
}

/// <summary>Hasil <see cref="ForestRecoveryRule.Calculate"/>.</summary>
/// <param name="HariPenuh">Jumlah hari penuh sejak <c>updated_at</c> lama.</param>
/// <param name="Perubahan">Kenaikan kesehatan yang diterapkan (≥ 0, 2 desimal).</param>
/// <param name="KesehatanBaru">Kesehatan setelah pemulihan (0–100).</param>
/// <param name="UpdatedAtBaru">Nilai <c>updated_at</c> yang baru (sama dengan yang lama jika tidak ada perubahan).</param>
public sealed record ForestRecovery(int HariPenuh, decimal Perubahan, decimal KesehatanBaru, DateTimeOffset UpdatedAtBaru)
{
    /// <summary><c>true</c> jika kesehatan naik (perlu ditulis ke <c>forest_logs</c>).</summary>
    public bool HasHealthChange => Perubahan > 0m;
}
