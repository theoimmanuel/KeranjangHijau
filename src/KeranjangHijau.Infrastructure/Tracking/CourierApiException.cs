using System.Net;

namespace KeranjangHijau.Infrastructure.Tracking;

/// <summary>
/// API cek resi gagal dipakai (timeout, HTTP error, respons tidak valid, konfigurasi salah).
/// Ditangkap <see cref="CompositeCourierTracker"/> yang lalu beralih ke data mock.
/// </summary>
/// <remarks>Pesan tidak pernah berisi API key, BaseUrl, atau isi respons API.</remarks>
public sealed class CourierApiException : Exception
{
    public CourierApiException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>Kode HTTP dari API, jika kegagalan berasal dari respons HTTP.</summary>
    public HttpStatusCode? StatusCode { get; }
}
