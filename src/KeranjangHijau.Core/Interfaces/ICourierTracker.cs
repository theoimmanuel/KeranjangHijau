using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Sumber data paket dari nomor resi: kota asal, kota tujuan, layanan, dan berat (CLAUDE.md 7.10).</summary>
public interface ICourierTracker
{
    /// <summary>
    /// Mencari data paket. Kode kurir dan nomor resi tidak peka huruf besar/kecil, dan spasi
    /// pada nomor resi diabaikan. <see cref="TrackingRequest.NomorHpAkhir"/> hanya dipakai
    /// tracker yang membutuhkannya (API cek resi).
    /// </summary>
    /// <returns>Data paket, atau <c>null</c> jika resi tidak ditemukan.</returns>
    Task<TrackingResult?> TrackAsync(TrackingRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Bentuk pendek pemanggilan tracker tanpa nomor HP.</summary>
public static class CourierTrackerExtensions
{
    public static Task<TrackingResult?> TrackAsync(
        this ICourierTracker tracker, string kodeKurir, string nomorResi, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tracker);
        return tracker.TrackAsync(new TrackingRequest(kodeKurir, nomorResi), cancellationToken);
    }
}
