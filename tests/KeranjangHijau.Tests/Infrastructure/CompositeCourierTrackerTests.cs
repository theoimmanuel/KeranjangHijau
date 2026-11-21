using System.Net;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Infrastructure.Tracking;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Logging;
using static KeranjangHijau.Tests.Infrastructure.CourierTrackerTestData;

namespace KeranjangHijau.Tests.Infrastructure;

public class CompositeCourierTrackerTests
{
    private readonly ListLogger<CompositeCourierTracker> _logger = new();

    private CompositeCourierTracker CreateComposite(FakeHttpMessageHandler handler, int timeoutSeconds = 5, string apiKey = ApiKey) =>
        new(CreateApi(handler, Options(apiKey, timeoutSeconds)), new MockCourierTracker(), _logger);

    [Fact]
    public async Task ApiSuccess_ReturnsApiResult()
    {
        var result = await CreateComposite(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, SuccessJson))
            .TrackAsync("jne", MockResi);

        Assert.NotNull(result);
        Assert.Equal(TrackingSource.Api, result.Sumber);
        Assert.Equal("Surabaya", result.KotaAsal);
    }

    [Fact]
    public async Task Api500_FallsBackToMock()
    {
        var result = await CreateComposite(FakeHttpMessageHandler.Returning(HttpStatusCode.InternalServerError))
            .TrackAsync("jne", MockResi);

        AssertMockResult(result);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("500"));
    }

    [Fact]
    public async Task Api429_FallsBackToMock()
    {
        var result = await CreateComposite(FakeHttpMessageHandler.Returning(HttpStatusCode.TooManyRequests))
            .TrackAsync("jne", MockResi);

        AssertMockResult(result);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("429"));
    }

    [Fact]
    public async Task ApiTimeout_FallsBackToMock()
    {
        var result = await CreateComposite(FakeHttpMessageHandler.NeverResponding(), timeoutSeconds: 1)
            .TrackAsync("jne", MockResi);

        AssertMockResult(result);
    }

    [Fact]
    public async Task ApiInvalidJson_FallsBackToMock()
    {
        var result = await CreateComposite(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "<html>"))
            .TrackAsync("jne", MockResi);

        AssertMockResult(result);
    }

    [Fact]
    public async Task ApiNotFound_FallsBackToMock()
    {
        var result = await CreateComposite(FakeHttpMessageHandler.Returning(HttpStatusCode.NotFound))
            .TrackAsync("jne", MockResi);

        AssertMockResult(result);
    }

    [Fact]
    public async Task EmptyApiKey_UsesMock_WithoutAnyHttpRequest()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, SuccessJson);

        var result = await CreateComposite(handler, apiKey: "").TrackAsync("jne", MockResi);

        AssertMockResult(result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task NotFoundInApiAndMock_ReturnsNull()
    {
        var result = await CreateComposite(FakeHttpMessageHandler.Returning(HttpStatusCode.InternalServerError))
            .TrackAsync("jne", "TIDAKADA0000");

        Assert.Null(result);
    }

    [Fact]
    public async Task CallerCancellation_IsNotSwallowed()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateComposite(FakeHttpMessageHandler.NeverResponding(), timeoutSeconds: 30).TrackAsync("jne", MockResi, cts.Token));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Logs_NeverContainApiKey(HttpStatusCode status)
    {
        await CreateComposite(FakeHttpMessageHandler.Returning(status, $"{{\"error\":\"{ApiKey}\"}}")).TrackAsync("jne", MockResi);

        Assert.NotEmpty(_logger.Entries);
        Assert.All(_logger.Entries, e =>
        {
            Assert.DoesNotContain(ApiKey, e.Message);
            Assert.Null(e.Exception);
        });
    }

    private static void AssertMockResult(TrackingResult? result)
    {
        Assert.NotNull(result);
        Assert.Equal(TrackingSource.Mock, result.Sumber);
        Assert.Equal("Jakarta", result.KotaAsal);
        Assert.Equal("Medan", result.KotaTujuan);
    }
}
