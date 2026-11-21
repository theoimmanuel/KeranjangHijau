using System.Net;
using System.Text.Json;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Infrastructure.Tracking;

/// <summary>
/// Adapter API cek resi RajaOngkir (Komerce), API terpilih sejak M2.4.
/// </summary>
/// <remarks>
/// Kontrak (lihat <c>docs/api-samples/CATATAN.md</c>):
/// <code>
/// POST {BaseUrl}/track/waybill
/// Header : key: {ApiKey}
/// Body   : application/x-www-form-urlencoded → awb, courier, last_phone_number (opsional)
///
/// Sukses       → { "meta": { "code": 200, ... }, "data": { "summary": {...}, "details": {...}, ... } }
/// Tidak ada    → { "meta": { "code": 404, "message": "Invalid Awb" }, "data": null }   (HTTP 404 atau 200)
/// Kurir ditolak→ { "meta": { "code": 400, ... }, "data": null }
/// </code>
/// Resi dan nomor HP dikirim di body (bukan query string) agar tidak tercatat di log URL HttpClient.
/// Nama dan alamat pengirim/penerima di respons <b>tidak pernah dibaca</b>; yang dibaca hanya kota,
/// kode layanan, dan berat.
/// </remarks>
public sealed class RajaOngkirCourierApiAdapter : ICourierApiAdapter
{
    public const string ApiKeyHeader = "key";
    public const string TrackPath = "track/waybill";

    public HttpRequestMessage CreateRequest(Uri baseUrl, string apiKey, TrackingRequest request)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(request);

        var form = new List<KeyValuePair<string, string>>
        {
            new("awb", request.NomorResi),
            new("courier", request.KodeKurir),
        };
        if (!string.IsNullOrEmpty(request.NomorHpAkhir))
        {
            form.Add(new("last_phone_number", request.NomorHpAkhir));
        }

        var message = new HttpRequestMessage(HttpMethod.Post, new Uri(CourierApiUrls.WithTrailingSlash(baseUrl), TrackPath))
        {
            Content = new FormUrlEncodedContent(form),
        };
        message.Headers.TryAddWithoutValidation(ApiKeyHeader, apiKey);
        message.Headers.Accept.ParseAdd("application/json");
        return message;
    }

    public TrackingResult? ParseResponse(string responseBody, string kodeKurir, string nomorResi)
    {
        ArgumentNullException.ThrowIfNull(responseBody);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Respons harus berupa objek JSON.");
        }

        // meta.code 404 → resi tidak ditemukan; kode error lain → CourierApiException (fallback ke mock).
        if (MetaCode(root) is { } code)
        {
            if (code == (int)HttpStatusCode.NotFound)
            {
                return null;
            }

            if (code is < 200 or > 299)
            {
                throw new CourierApiException(CourierApiUrls.DescribeError(code), (HttpStatusCode)code);
            }
        }

        if (!root.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (data.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Field 'data' harus berupa objek.");
        }

        var summary = Child(data, "summary");
        var details = Child(data, "details");

        var kotaAsal = FirstText(Text(details, "origin"), Text(summary, "origin"), Text(details, "shipper_city"))
            ?? throw new JsonException("Kota asal tidak ada di respons.");
        var kotaTujuan = FirstText(Text(details, "destination"), Text(summary, "destination"), Text(details, "receiver_city"))
            ?? throw new JsonException("Kota tujuan tidak ada di respons.");

        return new TrackingResult(
            nomorResi,
            kodeKurir,
            kotaAsal,
            kotaTujuan,
            Text(summary, "service_code"),
            Weight(details),
            TrackingSource.Api);
    }

    private static int? MetaCode(JsonElement root)
    {
        if (!root.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object
            || !meta.TryGetProperty("code", out var code))
        {
            return null;
        }

        return code.ValueKind switch
        {
            JsonValueKind.Number when code.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(code.GetString(), out var number) => number,
            _ => null,
        };
    }

    private static JsonElement? Child(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var child) && child.ValueKind == JsonValueKind.Object ? child : null;

    /// <summary>Teks yang sudah di-trim; kosong, null, atau bukan teks → <c>null</c> (format asli belum pasti).</summary>
    private static string? Text(JsonElement? parent, string name)
    {
        if (parent is not { } element || !element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString()?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static string? FirstText(params string?[] values) => values.FirstOrDefault(v => v is not null);

    private static decimal? Weight(JsonElement? details)
    {
        if (details is not { } element || !element.TryGetProperty("weight", out var weight))
        {
            return null;
        }

        return weight.ValueKind switch
        {
            JsonValueKind.Number when weight.TryGetDecimal(out var number) => WeightParser.Parse(number),
            JsonValueKind.String => WeightParser.Parse(weight.GetString()),
            _ => null,
        };
    }
}
