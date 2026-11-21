using System.Text.Json;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Infrastructure.Tracking;

namespace KeranjangHijau.Tests.Infrastructure;

public class GenericCourierApiAdapterTests
{
    private readonly GenericCourierApiAdapter _adapter = new();

    [Theory]
    [InlineData("https://api.test")]
    [InlineData("https://api.test/")]
    public void CreateRequest_HandlesBaseUrlWithOrWithoutSlash(string baseUrl)
    {
        using var request = _adapter.CreateRequest(new Uri(baseUrl), "kunci", new TrackingRequest("jne", "JNE1001000001"));

        Assert.Equal("https://api.test/tracking/jne/JNE1001000001", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public void Parse_OptionalFieldsMissing_ReturnsNulls()
    {
        var result = _adapter.ParseResponse("""{ "kotaAsal": " Jakarta ", "kotaTujuan": "Bandung" }""", "jne", "JNE1001000001");

        Assert.NotNull(result);
        Assert.Equal("Jakarta", result.KotaAsal);
        Assert.Null(result.Layanan);
        Assert.Null(result.BeratKg);
        Assert.Equal(TrackingSource.Api, result.Sumber);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1.5")]
    public void Parse_NonPositiveWeight_IsTreatedAsMissing(string weight)
    {
        var result = _adapter.ParseResponse(
            $$"""{ "kotaAsal": "Jakarta", "kotaTujuan": "Bandung", "beratKg": {{weight}} }""", "jne", "JNE1001000001");

        Assert.NotNull(result);
        Assert.Null(result.BeratKg);
    }

    [Fact]
    public void Parse_JsonNull_ReturnsNull()
    {
        Assert.Null(_adapter.ParseResponse("null", "jne", "JNE1001000001"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ \"kotaAsal\": \"\", \"kotaTujuan\": \"Bandung\" }")]
    [InlineData("{ \"kotaAsal\": 12, \"kotaTujuan\": \"Bandung\" }")]
    public void Parse_InvalidShape_ThrowsJsonException(string body)
    {
        Assert.ThrowsAny<JsonException>(() => _adapter.ParseResponse(body, "jne", "JNE1001000001"));
    }
}
