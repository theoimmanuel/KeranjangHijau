using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Infrastructure.Tracking;

/// <summary>
/// Penerjemah antara <see cref="ApiCourierTracker"/> dan satu API cek resi tertentu: cara
/// menyusun request (endpoint, autentikasi) dan cara membaca respons. Implementasi dipilih lewat
/// <c>CourierApi:Provider</c> (<see cref="RajaOngkirCourierApiAdapter"/> atau
/// <see cref="GenericCourierApiAdapter"/>); tracker, timeout, dan fallback ke mock tidak perlu diubah.
/// </summary>
public interface ICourierApiAdapter
{
    /// <summary>Menyusun request untuk satu resi.</summary>
    /// <param name="baseUrl">Nilai <c>CourierApi:BaseUrl</c> (URL absolut).</param>
    /// <param name="apiKey">API key. Sebaiknya dikirim lewat header, jangan di URL, agar tidak ikut tercatat di log.</param>
    /// <param name="request">
    /// Permintaan bentuk baku (<see cref="Core.Dtos.TrackingRequest.Normalize"/>): kode kurir huruf kecil,
    /// resi tanpa spasi + huruf besar, nomor HP tepat 5 digit atau <c>null</c>. Resi dan nomor HP
    /// sebaiknya dikirim di body, bukan di URL, agar tidak tercatat di log HttpClient.
    /// </param>
    HttpRequestMessage CreateRequest(Uri baseUrl, string apiKey, TrackingRequest request);

    /// <summary>
    /// Mengubah isi respons HTTP sukses (2xx) menjadi <see cref="TrackingResult"/> dengan
    /// <c>Sumber = Api</c>. Kembalikan <c>null</c> jika respons menyatakan resi tidak ditemukan.
    /// </summary>
    /// <exception cref="System.Text.Json.JsonException">Isi respons bukan JSON atau bentuknya tidak sesuai.</exception>
    /// <exception cref="CourierApiException">Respons menyatakan error selain "tidak ditemukan" (mis. kurir ditolak API).</exception>
    TrackingResult? ParseResponse(string responseBody, string kodeKurir, string nomorResi);
}
