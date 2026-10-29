using System.Net;
using System.Text;
using Backend.Infrastructure.Http;
using Backend.Infrastructure.ZipCodes;

namespace Backend.UnitTests.Infrastructure;

public sealed class ViaCepZipCodeClientTests
{
    [Fact]
    public async Task FindAsync_WhenZipCodeExists_ShouldReturnAddress()
    {
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            """{"cep":"01001-000","logradouro":"Praça da Sé","complemento":"lado ímpar","bairro":"Sé","localidade":"São Paulo","uf":"SP"}""");
        using var httpClient = new HttpClient(handler);
        var client = new ViaCepZipCodeClient(new HttpRequestClient(httpClient));

        var address = await client.FindAsync("01001000");

        Assert.Equal(new ZipCodeAddress("01001000", "Praça da Sé", "Sé", "São Paulo", "SP"), address);
        Assert.Equal("https://viacep.com.br/ws/01001000/json/", handler.RequestUri?.ToString());
    }

    [Theory]
    [InlineData("""{"erro":true}""")]
    [InlineData("""{"erro":"true"}""")]
    public async Task FindAsync_WhenZipCodeDoesNotExist_ShouldReturnNull(string content)
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, content));
        var client = new ViaCepZipCodeClient(new HttpRequestClient(httpClient));

        var address = await client.FindAsync("99999999");

        Assert.Null(address);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "{}")]
    [InlineData(HttpStatusCode.OK, "<html>manutenção</html>")]
    public async Task FindAsync_WhenServiceFails_ShouldThrowHttpRequestException(HttpStatusCode statusCode, string content)
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode, content));
        var client = new ViaCepZipCodeClient(new HttpRequestClient(httpClient));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.FindAsync("01001000"));
    }

    [Theory]
    [InlineData("01001-000")]
    [InlineData("0100100")]
    [InlineData("../../x")]
    public async Task FindAsync_WithZipCodeNotEightDigits_ShouldThrowArgumentException(string zipCode)
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, "{}"));
        var client = new ViaCepZipCodeClient(new HttpRequestClient(httpClient));

        await Assert.ThrowsAsync<ArgumentException>(() => client.FindAsync(zipCode));
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            });
        }
    }
}
