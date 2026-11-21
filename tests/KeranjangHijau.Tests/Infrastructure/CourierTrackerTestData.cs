using KeranjangHijau.Core.Options;
using KeranjangHijau.Infrastructure.Tracking;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Infrastructure;

/// <summary>Data dan pembuat objek bersama untuk test tracker API.</summary>
internal static class CourierTrackerTestData
{
    public const string ApiKey = "rahasia-api-key-12345";
    public const string BaseUrl = "https://api-cek-resi.test/v1";

    /// <summary>Resi yang ada di mock-resi.json: JNE, Jakarta → Medan, YES, 0,8 kg.</summary>
    public const string MockResi = "JNE1001000003";

    public const string SuccessJson = """{ "kotaAsal": "Surabaya", "kotaTujuan": "Denpasar", "layanan": "REG", "beratKg": 2.5 }""";

    public static CourierApiOptions Options(string apiKey = ApiKey, int timeoutSeconds = 5) =>
        new() { BaseUrl = BaseUrl, ApiKey = apiKey, TimeoutSeconds = timeoutSeconds };

    public static ApiCourierTracker CreateApi(FakeHttpMessageHandler handler, CourierApiOptions? options = null) =>
        new(new FakeHttpClientFactory(handler), new GenericCourierApiAdapter(), Microsoft.Extensions.Options.Options.Create(options ?? Options()));
}
