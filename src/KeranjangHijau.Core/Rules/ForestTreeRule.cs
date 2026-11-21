namespace KeranjangHijau.Core.Rules;

/// <summary>Perkiraan jumlah pohon yang tersisa dari kesehatan hutan. Aturan murni tanpa I/O.</summary>
public static class ForestTreeRule
{
    /// <summary>
    /// <c>round(jumlah_pohon_awal × kesehatan / 100)</c>, dibulatkan ke bilangan bulat terdekat (<c>AwayFromZero</c>).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Jumlah pohon negatif atau kesehatan di luar 0–100.</exception>
    public static int EstimateRemainingTrees(int jumlahPohonAwal, decimal kesehatan)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(jumlahPohonAwal);
        if (kesehatan is < ForestStatusRule.MinHealth or > ForestStatusRule.MaxHealth)
        {
            throw new ArgumentOutOfRangeException(nameof(kesehatan), kesehatan, "Kesehatan hutan harus di antara 0 dan 100.");
        }

        return (int)Math.Round(jumlahPohonAwal * kesehatan / 100m, 0, MidpointRounding.AwayFromZero);
    }
}
