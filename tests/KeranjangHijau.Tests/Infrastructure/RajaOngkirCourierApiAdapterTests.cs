using System.Net;
using System.Text.Json;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Infrastructure.Tracking;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace KeranjangHijau.Tests.Infrastructure;

/// <summary>
/// Adapter RajaOngkir (M2.4) diuji dengan contoh respons di <c>docs/api-samples</c> lewat
/// <see cref="FakeHttpMessageHandler"/>; tidak ada request ke internet.
/// </summary>
public class RajaOngkirCourierApiAdapterTests
{
    private const string ApiKey = "test-key";
    private const string BaseUrl = "https://rajaongkir.test/api/v1";
    private const string HpAkhir = "98765";

    /// <summary>Resi yang ada di mock-resi.json: JNE, Jakarta → Medan.</summary>
    private const string MockResi = "JNE1001000003";

    private static readonly string SuccessSample = Sample("rajaongkir-contoh-dokumentasi.json");
    private static readonly string InvalidAwbSample = Sample("rajaongkir-404-invalid-awb.json");
    private static readonly string InvalidCourierSample = Sample("rajaongkir-400-invalid-courier.json");

    private readonly RajaOngkirCourierApiAdapter _adapter = new();
    private readonly ListLogger<CompositeCourierTracker> _logger = new();

    private static string Sample(string fileName) => RepoFiles.Path("docs", "api-samples", fileName);

    private static CourierApiOptions Options(List<string>? supported = null) => new()
    {
        BaseUrl = BaseUrl,
        ApiKey = ApiKey,
        TimeoutSeconds = 5,
        SupportedCouriers = supported ?? [],
    };

    private static ApiCourierTracker CreateApi(FakeHttpMessageHandler handler, CourierApiOptions? options = null) =>
        new(new FakeHttpClientFactory(handler), new RajaOngkirCourierApiAdapter(), Microsoft.Extensions.Options.Options.Create(options ?? Options()));

    private CompositeCourierTracker CreateComposite(FakeHttpMessageHandler handler) =>
        new(CreateApi(handler), new MockCourierTracker(), _logger);

    // ---------------------------------------------------------------- Respons sukses

    [Fact]
    public async Task SuccessSample_MapsCitiesServiceAndWeight()
    {
        var handler = FakeHttpMessageHandler.ReturningFile(HttpStatusCode.OK, SuccessSample);

        var result = await CreateApi(handler).TrackAsync(new TrackingRequest("JNE", "contoh 123456"));

        Assert.NotNull(result);
        Assert.Equal(TrackingSource.Api, result.Sumber);
        Assert.Equal("jne", result.KodeKurir);
        Assert.Equal("CONTOH123456", result.NomorResi);
        Assert.Equal("JAKARTA BARAT", result.KotaAsal);
        Assert.Equal("YOGYAKARTA", result.KotaTujuan);
        Assert.Equal("REG", result.Layanan);
        Assert.Equal(1m, result.BeratKg);
    }

    [Fact]
    public async Task SuccessSample_ThroughComposite_UsesApiNotMock()
    {
        var result = await CreateComposite(FakeHttpMessageHandler.ReturningFile(HttpStatusCode.OK, SuccessSample))
            .TrackAsync("jne", MockResi);

        Assert.NotNull(result);
        Assert.Equal(TrackingSource.Api, result.Sumber);
        Assert.Equal("JAKARTA BARAT", result.KotaAsal);
    }

    [Fact]
    public void Parse_NeverReadsNamesOrAddresses()
    {
        var result = _adapter.ParseResponse(File.ReadAllText(SuccessSample), "jne", "CONTOH123456");

        Assert.NotNull(result);
        Assert.DoesNotContain("TOKO CONTOH", result.ToString());
        Assert.DoesNotContain("PENERIMA CONTOH", result.ToString());
    }

    [Fact]
    public void Parse_CityFallsBack_DetailsThenSummaryThenShipperReceiverCity()
    {
        const string onlySummary = """
            { "meta": { "code": 200 }, "data": {
                "summary": { "origin": "SURABAYA", "destination": "MEDAN", "service_code": "YES" },
                "details": { "origin": "", "destination": null, "weight": 2 } } }
            """;
        const string onlyCityFields = """
            { "meta": { "code": 200 }, "data": {
                "summary": {},
                "details": { "shipper_city": "BANDUNG", "receiver_city": "MAKASSAR" } } }
            """;

        var fromSummary = _adapter.ParseResponse(onlySummary, "jne", "JNE1");
        var fromCity = _adapter.ParseResponse(onlyCityFields, "jne", "JNE1");

        Assert.Equal(("SURABAYA", "MEDAN", "YES", 2m), (fromSummary!.KotaAsal, fromSummary.KotaTujuan, fromSummary.Layanan, fromSummary.BeratKg));
        Assert.Equal(("BANDUNG", "MAKASSAR", null, null), (fromCity!.KotaAsal, fromCity.KotaTujuan, fromCity.Layanan, fromCity.BeratKg));
    }

    [Theory]
    [InlineData("\"1000 gram\"", 1.0)]
    [InlineData("\"1,5\"", 1.5)]
    [InlineData("2.25", 2.25)]
    [InlineData("\"0\"", null)]
    [InlineData("\"abc\"", null)]
    [InlineData("null", null)]
    [InlineData("{}", null)]
    public void Parse_Weight_UsesWeightParser(string weightJson, double? expected)
    {
        var body = $$"""
            { "meta": { "code": 200 }, "data": { "details": { "origin": "A", "destination": "B", "weight": {{weightJson}} } } }
            """;

        var result = _adapter.ParseResponse(body, "jne", "JNE1");

        Assert.Equal((decimal?)expected, result!.BeratKg);
    }

    // ---------------------------------------------------------------- Tidak ditemukan

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.OK)]
    public async Task InvalidAwbSample_Http404Or200_ReturnsNull(HttpStatusCode status)
    {
        var handler = FakeHttpMessageHandler.ReturningFile(status, InvalidAwbSample);

        Assert.Null(await CreateApi(handler).TrackAsync("jne", "JNE0000000001"));
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.OK)]
    public async Task InvalidAwbSample_FallsBackToMock(HttpStatusCode status)
    {
        var result = await CreateComposite(FakeHttpMessageHandler.ReturningFile(status, InvalidAwbSample))
            .TrackAsync("jne", MockResi);

        AssertMockResult(result);
    }

    [Theory]
    [InlineData("""{ "meta": { "code": 200 }, "data": null }""")]
    [InlineData("""{ "meta": { "code": 200 } }""")]
    [InlineData("""{ "meta": { "code": "404" }, "data": null }""")]
    public void Parse_DataNullOrMeta404_ReturnsNull(string body)
    {
        Assert.Null(_adapter.ParseResponse(body, "jne", "JNE1"));
    }

    // ---------------------------------------------------------------- Error → fallback

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.OK)]
    public async Task InvalidCourierSample_ThrowsCourierApiException(HttpStatusCode status)
    {
        var handler = FakeHttpMessageHandler.ReturningFile(status, InvalidCourierSample);

        var ex = await Assert.ThrowsAsync<CourierApiException>(() => CreateApi(handler).TrackAsync("jne", MockResi));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.DoesNotContain(ApiKey, ex.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.OK)]
    public async Task InvalidCourierSample_FallsBackToMock(HttpStatusCode status)
    {
        var result = await CreateComposite(FakeHttpMessageHandler.ReturningFile(status, InvalidCourierSample))
            .TrackAsync("jne", MockResi);

        AssertMockResult(result);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("400"));
    }

    [Theory]
    [InlineData("<html>bukan json</html>")]
    [InlineData("[]")]
    [InlineData("""{ "meta": { "code": 200 }, "data": "teks" }""")]
    [InlineData("""{ "meta": { "code": 200 }, "data": { "details": { "destination": "YOGYAKARTA" } } }""")]
    [InlineData("""{ "meta": { "code": 200 }, "data": { "details": { "origin": "JAKARTA", "destination": "  " } } }""")]
    public void Parse_BrokenJsonOrMissingCity_ThrowsJsonException(string body)
    {
        Assert.ThrowsAny<JsonException>(() => _adapter.ParseResponse(body, "jne", "JNE1"));
    }

    [Fact]
    public async Task MissingCity_ThroughTracker_FallsBackToMock()
    {
        const string body = """{ "meta": { "code": 200 }, "data": { "summary": { "origin": "JAKARTA" } } }""";

        var result = await CreateComposite(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, body)).TrackAsync("jne", MockResi);

        AssertMockResult(result);
    }

    // ---------------------------------------------------------------- Bentuk request

    [Fact]
    public async Task Request_IsPostFormWithKeyHeader_AndNoResiInUrl()
    {
        var handler = FakeHttpMessageHandler.ReturningFile(HttpStatusCode.OK, SuccessSample);

        await CreateApi(handler).TrackAsync(new TrackingRequest(" JNE ", "jne 1001 000003"));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://rajaongkir.test/api/v1/track/waybill", request.RequestUri!.AbsoluteUri);
        Assert.Equal(ApiKey, Assert.Single(request.Headers.GetValues(RajaOngkirCourierApiAdapter.ApiKeyHeader)));
        Assert.Empty(request.RequestUri.Query);
        Assert.DoesNotContain("JNE1001000003", request.RequestUri.AbsoluteUri);
        Assert.DoesNotContain(ApiKey, request.RequestUri.AbsoluteUri);

        Assert.Equal("awb=JNE1001000003&courier=jne", Assert.Single(handler.RequestBodies));
        Assert.Equal("application/x-www-form-urlencoded", handler.Requests[0].Content?.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Request_WithPhoneSuffix_SendsLastPhoneNumberInBodyOnly()
    {
        var handler = FakeHttpMessageHandler.ReturningFile(HttpStatusCode.OK, SuccessSample);

        await CreateApi(handler).TrackAsync(new TrackingRequest("jnt", "JP1002000004", $" {HpAkhir} "));

        var request = Assert.Single(handler.Requests);
        Assert.Equal($"awb=JP1002000004&courier=jnt&last_phone_number={HpAkhir}", Assert.Single(handler.RequestBodies));
        Assert.DoesNotContain(HpAkhir, request.RequestUri!.AbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234")]
    [InlineData("12a45")]
    public async Task Request_WithoutValidPhoneSuffix_OmitsLastPhoneNumber(string? hp)
    {
        var handler = FakeHttpMessageHandler.ReturningFile(HttpStatusCode.OK, SuccessSample);

        await CreateApi(handler).TrackAsync(new TrackingRequest("jne", "JNE1001000003", hp));

        Assert.DoesNotContain("last_phone_number", Assert.Single(handler.RequestBodies));
    }

    [Theory]
    [InlineData("https://rajaongkir.test/api/v1")]
    [InlineData("https://rajaongkir.test/api/v1/")]
    public void CreateRequest_HandlesBaseUrlWithOrWithoutSlash(string baseUrl)
    {
        using var request = _adapter.CreateRequest(new Uri(baseUrl), ApiKey, new TrackingRequest("jne", "JNE1"));

        Assert.Equal("https://rajaongkir.test/api/v1/track/waybill", request.RequestUri!.AbsoluteUri);
    }

    // ---------------------------------------------------------------- Kurir tidak didukung

    [Theory]
    [InlineData("sicepat")]
    [InlineData("SiCepat")]
    [InlineData("kurirfiktif")]
    public async Task UnsupportedCourier_NeverCallsHttp(string kodeKurir)
    {
        var handler = FakeHttpMessageHandler.ReturningFile(HttpStatusCode.OK, SuccessSample);
        var factory = new FakeHttpClientFactory(handler);
        var tracker = new ApiCourierTracker(factory, _adapter, Microsoft.Extensions.Options.Options.Create(Options()));

        Assert.Null(await tracker.TrackAsync(kodeKurir, "004710030002"));
        Assert.Empty(handler.Requests);
        Assert.Empty(factory.RequestedNames);
    }

    [Fact]
    public async Task UnsupportedCourier_ThroughComposite_UsesMockWithoutHttp()
    {
        var handler = FakeHttpMessageHandler.ReturningFile(HttpStatusCode.OK, SuccessSample);

        // mock-resi.json: sicepat 004710030002 ada di data mock.
        var result = await CreateComposite(handler).TrackAsync("sicepat", "004710030002");

        Assert.NotNull(result);
        Assert.Equal(TrackingSource.Mock, result.Sumber);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SupportedCouriers_FromOptions_ReplaceDefaultList()
    {
        var handler = FakeHttpMessageHandler.ReturningFile(HttpStatusCode.OK, SuccessSample);
        var tracker = CreateApi(handler, Options(supported: ["sicepat"]));

        Assert.NotNull(await tracker.TrackAsync("sicepat", "004710030002"));
        Assert.Null(await tracker.TrackAsync("jne", MockResi));
        Assert.Single(handler.Requests);
    }

    // ---------------------------------------------------------------- Data pribadi tidak bocor

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ErrorsAndLogs_NeverContainApiKeyOrPhone(HttpStatusCode status)
    {
        // API seolah memantulkan key dan nomor HP di isi respons.
        var body = $$"""{ "meta": { "code": {{(int)status}}, "message": "{{ApiKey}} {{HpAkhir}}" }, "data": null }""";
        var handler = FakeHttpMessageHandler.Returning(status, body);

        var ex = await Assert.ThrowsAsync<CourierApiException>(
            () => CreateApi(handler).TrackAsync(new TrackingRequest("jne", MockResi, HpAkhir)));
        await CreateComposite(handler).TrackAsync(new TrackingRequest("jne", MockResi, HpAkhir));

        Assert.DoesNotContain(ApiKey, ex.Message);
        Assert.DoesNotContain(HpAkhir, ex.Message);
        Assert.NotEmpty(_logger.Entries);
        Assert.All(_logger.Entries, e =>
        {
            Assert.DoesNotContain(ApiKey, e.Message);
            Assert.DoesNotContain(HpAkhir, e.Message);
            Assert.DoesNotContain(MockResi, e.Message);
            Assert.Null(e.Exception);
        });
    }

    [Fact]
    public async Task Meta400In200Body_MessageNeverContainsResponseText()
    {
        var body = $$"""{ "meta": { "code": 400, "message": "{{ApiKey}} {{HpAkhir}}" }, "data": null }""";

        var ex = await Assert.ThrowsAsync<CourierApiException>(
            () => CreateApi(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, body))
                .TrackAsync(new TrackingRequest("jne", MockResi, HpAkhir)));

        Assert.DoesNotContain(ApiKey, ex.Message);
        Assert.DoesNotContain(HpAkhir, ex.Message);
    }

    private static void AssertMockResult(TrackingResult? result)
    {
        Assert.NotNull(result);
        Assert.Equal(TrackingSource.Mock, result.Sumber);
        Assert.Equal("Jakarta", result.KotaAsal);
        Assert.Equal("Medan", result.KotaTujuan);
    }
}
