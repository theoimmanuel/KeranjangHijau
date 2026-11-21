namespace KeranjangHijau.Core.Interfaces;

/// <summary>
/// Sumber waktu. Pakai ini, bukan <c>DateTime.Now</c>, agar logika berbasis waktu bisa dites.
/// </summary>
public interface IClock
{
    /// <summary>Waktu sekarang dalam UTC (offset selalu nol).</summary>
    DateTimeOffset UtcNow { get; }
}
