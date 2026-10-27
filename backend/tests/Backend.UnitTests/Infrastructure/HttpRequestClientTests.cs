using System.Net;
using System.Text;
using Backend.Infrastructure.Http;

namespace Backend.UnitTests.Infrastructure;

public sealed class HttpRequestClientTests
{
    [Fact]
    public async Task GetAsync_WhenResponseIsSuccess_ShouldDeserializeBody()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, """{"name":"Valor de teste"}"""));
        var client = new HttpRequestClient(httpClient);

        var response = await client.GetAsync<SampleResponse>("https://example.com/items");

        Assert.Equal("Valor de teste", response?.Name);
    }

    [Fact]
    public async Task GetAsync_WhenResponseIsError_ShouldThrowHttpRequestException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.InternalServerError, "{}"));
        var client = new HttpRequestClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync<SampleResponse>("https://example.com/items"));
    }

    private sealed record SampleResponse(string Name);

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            });
    }
}
