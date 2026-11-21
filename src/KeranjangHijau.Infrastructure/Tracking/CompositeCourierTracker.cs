using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KeranjangHijau.Infrastructure.Tracking;

/// <summary>
/// <see cref="ICourierTracker"/> utama aplikasi (CLAUDE.md 7.10): coba
/// <see cref="ApiCourierTracker"/> dulu, lalu <see cref="MockCourierTracker"/> jika API
/// tidak menemukan resi, belum dikonfigurasi, kurirnya tidak didukung API, timeout, HTTP error
/// (termasuk 429), atau responsnya tidak valid.
/// </summary>
public sealed class CompositeCourierTracker : ICourierTracker
{
    private readonly ApiCourierTracker _api;
    private readonly MockCourierTracker _mock;
    private readonly ILogger<CompositeCourierTracker> _logger;

    public CompositeCourierTracker(ApiCourierTracker api, MockCourierTracker mock, ILogger<CompositeCourierTracker> logger)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(mock);
        ArgumentNullException.ThrowIfNull(logger);
        _api = api;
        _mock = mock;
        _logger = logger;
    }

    public async Task<TrackingResult?> TrackAsync(TrackingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Hanya kode kurir yang dicatat di log: resi dan nomor HP tidak pernah masuk log.
        var kodeKurir = request.KodeKurir?.Trim().ToLowerInvariant();
        try
        {
            var fromApi = await _api.TrackAsync(request, cancellationToken).ConfigureAwait(false);
            if (fromApi is not null)
            {
                return fromApi;
            }

            _logger.LogDebug("Resi tidak didapat dari API cek resi (kurir {KodeKurir}); memakai data mock.", kodeKurir);
        }
        catch (CourierApiException ex)
        {
            // Hanya pesan (sudah disaring, tanpa API key) yang dicatat, bukan objek exception-nya.
            _logger.LogWarning("API cek resi gagal (kurir {KodeKurir}): {Alasan} Memakai data mock.", kodeKurir, ex.Message);
        }

        return await _mock.TrackAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
