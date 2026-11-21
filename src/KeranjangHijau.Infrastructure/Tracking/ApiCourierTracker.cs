using System.Net;
using System.Text.Json;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Options;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Infrastructure.Tracking;

/// <summary>
/// Tracker yang memanggil API cek resi (CLAUDE.md 7.10). Detail API (endpoint, autentikasi,
/// format respons) ada di <see cref="ICourierApiAdapter"/>; kelas ini hanya mengurus HTTP,
/// timeout, dan penerjemahan error.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>API key kosong → langsung <c>null</c>, tanpa request jaringan.</item>
///   <item>Kurir tidak ada di <c>CourierApi:SupportedCouriers</c> → langsung <c>null</c>, tanpa request
///         (kuota API tidak terbuang; <see cref="CompositeCourierTracker"/> lalu memakai mock).</item>
///   <item>HTTP 404 atau adapter mengembalikan null → <c>null</c> (resi tidak ditemukan).</item>
///   <item>Timeout, gangguan jaringan, HTTP error lain (termasuk 429), respons tidak valid, atau
///         BaseUrl salah → <see cref="CourierApiException"/>.</item>
///   <item>Pembatalan dari pemanggil → <see cref="OperationCanceledException"/> diteruskan apa adanya.</item>
/// </list>
/// API key, nomor HP, dan isi respons tidak pernah dimasukkan ke pesan error.
/// </remarks>
public sealed class ApiCourierTracker : ICourierTracker
{
    /// <summary>Nama HttpClient di <see cref="IHttpClientFactory"/>.</summary>
    public const string HttpClientName = "KeranjangHijau.CourierApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICourierApiAdapter _adapter;
    private readonly IOptions<CourierApiOptions> _options;

    public ApiCourierTracker(IHttpClientFactory httpClientFactory, ICourierApiAdapter adapter, IOptions<CourierApiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(options);
        _httpClientFactory = httpClientFactory;
        _adapter = adapter;
        _options = options;
    }

    public async Task<TrackingResult?> TrackAsync(TrackingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = request.Normalize();
        var options = _options.Value;
        if (!options.HasApiKey)
        {
            return null;
        }

        if (normalized.KodeKurir.Length == 0 || normalized.NomorResi.Length == 0)
        {
            return null;
        }

        if (!options.SupportsCourier(normalized.KodeKurir))
        {
            return null; // Kurir tidak didukung API: jangan buang kuota.
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl)
            || (baseUrl.Scheme != Uri.UriSchemeHttps && baseUrl.Scheme != Uri.UriSchemeHttp))
        {
            throw new CourierApiException("CourierApi:BaseUrl belum diatur atau bukan URL http/https yang valid.");
        }

        var timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            using var message = _adapter.CreateRequest(baseUrl, options.ApiKey, normalized);
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new CourierApiException(CourierApiUrls.DescribeError((int)response.StatusCode), response.StatusCode);
            }

            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            return _adapter.ParseResponse(body, normalized.KodeKurir, normalized.NomorResi);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Dibatalkan pemanggil, bukan timeout.
        }
        catch (OperationCanceledException ex)
        {
            // CancelAfter di atas, atau HttpClient.Timeout bawaan.
            throw new CourierApiException($"API cek resi tidak merespons dalam {options.TimeoutSeconds} detik.", innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new CourierApiException("Gagal terhubung ke API cek resi.", ex.StatusCode, ex);
        }
        catch (JsonException ex)
        {
            // Pesan JsonException bisa memuat potongan isi respons; tidak dipakai di pesan.
            throw new CourierApiException("Respons API cek resi tidak valid.", innerException: ex);
        }
    }
}
