using System.Net;

namespace KeranjangHijau.Tests.Fakes;

/// <summary>
/// <see cref="HttpMessageHandler"/> palsu: tidak ada request jaringan sungguhan. Respons
/// ditentukan oleh fungsi <c>respond</c>, dan semua request dicatat di <see cref="Requests"/>.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        _respond = respond;
    }

    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Isi body setiap request (dibaca saat dikirim, karena content dibuang setelah request selesai).</summary>
    public List<string?> RequestBodies { get; } = [];

    /// <summary>Selalu membalas dengan status dan isi JSON tertentu.</summary>
    public static FakeHttpMessageHandler Returning(HttpStatusCode status, string? json = null) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json ?? string.Empty, System.Text.Encoding.UTF8, "application/json"),
        }));

    /// <summary>Tidak pernah membalas sampai dibatalkan (untuk menguji timeout).</summary>
    public static FakeHttpMessageHandler NeverResponding() =>
        new(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("Tidak akan tercapai.");
        });

    /// <summary>Membalas dengan isi file contoh respons (mis. dari <c>docs/api-samples</c>).</summary>
    public static FakeHttpMessageHandler ReturningFile(HttpStatusCode status, string path) =>
        Returning(status, File.ReadAllText(path));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
        return await _respond(request, cancellationToken);
    }
}

/// <summary><see cref="IHttpClientFactory"/> yang selalu memakai <see cref="FakeHttpMessageHandler"/>.</summary>
public sealed class FakeHttpClientFactory(FakeHttpMessageHandler handler) : IHttpClientFactory
{
    public List<string> RequestedNames { get; } = [];

    public HttpClient CreateClient(string name)
    {
        RequestedNames.Add(name);
        return new HttpClient(handler, disposeHandler: false);
    }
}
