using System.Net;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Infrastructure.Tracking;
using KeranjangHijau.Tests.Fakes;
using static KeranjangHijau.Tests.Infrastructure.CourierTrackerTestData;

namespace KeranjangHijau.Tests.Infrastructure;

public class ApiCourierTrackerTests
{
    [Fact]
    public async Task Success_ReturnsApiResult()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, SuccessJson);

        var result = await CreateApi(handler).TrackAsync(" JNE ", "jne 1001 000003");

        Assert.NotNull(result);
        Assert.Equal(TrackingSource.Api, result.Sumber);
        Assert.Equal("JNE1001000003", result.NomorResi);
        Assert.Equal("jne", result.KodeKurir);
        Assert.Equal("Surabaya", result.KotaAsal);
        Assert.Equal("Denpasar", result.KotaTujuan);
        Assert.Equal("REG", result.Layanan);
        Assert.Equal(2.5m, result.BeratKg);
    }

    [Fact]
    public async Task Request_UsesNamedClient_ApiKeyHeader_AndNormalizedValues()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, SuccessJson);
        var factory = new FakeHttpClientFactory(handler);
        var tracker = new ApiCourierTracker(
            factory, new GenericCourierApiAdapter(), Microsoft.Extensions.Options.Options.Create(Options()));

        await tracker.TrackAsync("JnT", "jp 1002 000004");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api-cek-resi.test/v1/tracking/jnt/JP1002000004", request.RequestUri!.AbsoluteUri);
        Assert.Equal(ApiKey, Assert.Single(request.Headers.GetValues(GenericCourierApiAdapter.ApiKeyHeader)));
        Assert.DoesNotContain(ApiKey, request.RequestUri.AbsoluteUri);
        Assert.Equal([ApiCourierTracker.HttpClientName], factory.RequestedNames);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyApiKey_ReturnsNull_WithoutAnyHttpRequest(string apiKey)
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, SuccessJson);
        var factory = new FakeHttpClientFactory(handler);
        var tracker = new ApiCourierTracker(
            factory, new GenericCourierApiAdapter(), Microsoft.Extensions.Options.Options.Create(Options(apiKey)));

        Assert.Null(await tracker.TrackAsync("jne", MockResi));
        Assert.Empty(handler.Requests);
        Assert.Empty(factory.RequestedNames);
    }

    [Fact]
    public async Task NotFound404_ReturnsNull()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.NotFound);

        Assert.Null(await CreateApi(handler).TrackAsync("jne", MockResi));
    }

    [Fact]
    public async Task Http500_ThrowsCourierApiException()
    {
        var ex = await Assert.ThrowsAsync<CourierApiException>(
            () => CreateApi(FakeHttpMessageHandler.Returning(HttpStatusCode.InternalServerError)).TrackAsync("jne", MockResi));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task Http429_ThrowsQuotaMessage()
    {
        var ex = await Assert.ThrowsAsync<CourierApiException>(
            () => CreateApi(FakeHttpMessageHandler.Returning(HttpStatusCode.TooManyRequests)).TrackAsync("jne", MockResi));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Contains("Kuota", ex.Message);
    }

    [Fact]
    public async Task Timeout_ThrowsCourierApiException()
    {
        var tracker = CreateApi(FakeHttpMessageHandler.NeverResponding(), Options(timeoutSeconds: 1));

        var ex = await Assert.ThrowsAsync<CourierApiException>(() => tracker.TrackAsync("jne", MockResi));

        Assert.Contains("1 detik", ex.Message);
        Assert.Null(ex.StatusCode);
    }

    [Fact]
    public async Task CallerCancellation_IsNotTreatedAsTimeout()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var tracker = CreateApi(FakeHttpMessageHandler.NeverResponding(), Options(timeoutSeconds: 30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tracker.TrackAsync("jne", MockResi, cts.Token));
    }

    [Fact]
    public async Task NetworkError_ThrowsCourierApiException()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("Connection refused"));

        var ex = await Assert.ThrowsAsync<CourierApiException>(() => CreateApi(handler).TrackAsync("jne", MockResi));

        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Theory]
    [InlineData("<html>bukan json</html>")]
    [InlineData("""{ "kotaTujuan": "Bandung" }""")]
    [InlineData("""{ "kotaAsal": "Jakarta", "kotaTujuan": "Bandung", "beratKg": "satu" }""")]
    [InlineData("[]")]
    public async Task InvalidJson_ThrowsCourierApiException(string body)
    {
        var ex = await Assert.ThrowsAsync<CourierApiException>(
            () => CreateApi(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, body)).TrackAsync("jne", MockResi));

        Assert.Equal("Respons API cek resi tidak valid.", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bukan-url")]
    [InlineData("ftp://api.test")]
    public async Task InvalidBaseUrl_ThrowsWithoutRequest(string baseUrl)
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, SuccessJson);
        var options = Options();
        options.BaseUrl = baseUrl;

        await Assert.ThrowsAsync<CourierApiException>(() => CreateApi(handler, options).TrackAsync("jne", MockResi));
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task ErrorMessages_NeverContainApiKey(HttpStatusCode status)
    {
        // Isi respons sengaja memuat API key, seolah API memantulkannya kembali.
        var handler = FakeHttpMessageHandler.Returning(status, $$"""{ "error": "invalid key {{ApiKey}}" }""");

        var ex = await Assert.ThrowsAsync<CourierApiException>(() => CreateApi(handler).TrackAsync("jne", MockResi));

        Assert.DoesNotContain(ApiKey, ex.Message);
        Assert.DoesNotContain(BaseUrl, ex.Message);
    }

    [Fact]
    public async Task InvalidJsonMessage_NeverContainsApiKey()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, $"{{ \"kotaAsal\": {ApiKey} }}");

        var ex = await Assert.ThrowsAsync<CourierApiException>(() => CreateApi(handler).TrackAsync("jne", MockResi));

        Assert.DoesNotContain(ApiKey, ex.Message);
    }
}
