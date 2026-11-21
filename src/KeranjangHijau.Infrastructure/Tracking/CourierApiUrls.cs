using System.Net;

namespace KeranjangHijau.Infrastructure.Tracking;

/// <summary>Bantuan kecil yang dipakai bersama oleh adapter API cek resi.</summary>
internal static class CourierApiUrls
{
    /// <summary>
    /// Menambahkan "/" di akhir BaseUrl, supaya path relatif disambung (bukan menggantikan segmen
    /// terakhir, mis. <c>/api/v1</c> + <c>track/waybill</c> → <c>/api/v1/track/waybill</c>).
    /// </summary>
    public static Uri WithTrailingSlash(Uri baseUrl) =>
        baseUrl.AbsoluteUri.EndsWith('/') ? baseUrl : new Uri(baseUrl.AbsoluteUri + "/");

    /// <summary>Pesan error untuk kode status (HTTP atau <c>meta.code</c>); tidak pernah memuat isi respons.</summary>
    public static string DescribeError(int code) => code switch
    {
        (int)HttpStatusCode.TooManyRequests => "Kuota API cek resi habis atau terlalu banyak permintaan (HTTP 429).",
        (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden =>
            $"API cek resi menolak API key (HTTP {code}). Periksa CourierApi:ApiKey.",
        (int)HttpStatusCode.BadRequest => "API cek resi menolak permintaan (kode 400), mis. kurir tidak didukung.",
        _ => $"API cek resi mengembalikan error HTTP {code}.",
    };
}
