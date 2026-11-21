using System.Text.Json;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Infrastructure.Tracking;

/// <summary>
/// Adapter contoh dengan format netral milik KeranjangHijau (M2.3). Ini <b>bukan</b> format API
/// pihak ketiga mana pun. API terpilih adalah RajaOngkir (<see cref="RajaOngkirCourierApiAdapter"/>);
/// adapter ini tetap tersedia lewat <c>CourierApi:Provider = "Generic"</c> sebagai contoh/cadangan.
/// </summary>
/// <remarks>
/// Kontrak contoh:
/// <code>
/// GET {BaseUrl}/tracking/{kodeKurir}/{nomorResi}
/// Header: X-Api-Key: {ApiKey}
///
/// 200 OK → { "kotaAsal": "Jakarta", "kotaTujuan": "Bandung", "layanan": "REG", "beratKg": 1.2 }
///          (layanan dan beratKg boleh null atau tidak ada)
/// 404    → resi tidak ditemukan (ditangani ApiCourierTracker)
/// </code>
/// Nomor HP (<see cref="TrackingRequest.NomorHpAkhir"/>) tidak dipakai format ini.
/// </remarks>
public sealed class GenericCourierApiAdapter : ICourierApiAdapter
{
    public const string ApiKeyHeader = "X-Api-Key";

    public HttpRequestMessage CreateRequest(Uri baseUrl, string apiKey, TrackingRequest request)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(request);

        var uri = new Uri(
            CourierApiUrls.WithTrailingSlash(baseUrl),
            $"tracking/{Uri.EscapeDataString(request.KodeKurir)}/{Uri.EscapeDataString(request.NomorResi)}");

        var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Headers.TryAddWithoutValidation(ApiKeyHeader, apiKey);
        message.Headers.Accept.ParseAdd("application/json");
        return message;
    }

    public TrackingResult? ParseResponse(string responseBody, string kodeKurir, string nomorResi)
    {
        ArgumentNullException.ThrowIfNull(responseBody);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Respons harus berupa objek JSON.");
        }

        return new TrackingResult(
            nomorResi,
            kodeKurir,
            RequiredString(root, "kotaAsal"),
            RequiredString(root, "kotaTujuan"),
            OptionalString(root, "layanan"),
            OptionalPositiveDecimal(root, "beratKg"),
            TrackingSource.Api);
    }

    private static string RequiredString(JsonElement root, string name) =>
        OptionalString(root, name) ?? throw new JsonException($"Field '{name}' wajib diisi.");

    private static string? OptionalString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Field '{name}' harus berupa teks.");
        }

        var text = value.GetString()?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    // Berat tidak valid (≤ 0) dianggap tidak tersedia, jadi dipakai berat manual/default (7.4).
    private static decimal? OptionalPositiveDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number))
        {
            throw new JsonException($"Field '{name}' harus berupa angka.");
        }

        return number > 0 ? number : null;
    }
}
