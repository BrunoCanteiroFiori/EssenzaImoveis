using System.Net;
using System.Net.Http.Json;
using Backend.Infrastructure.Conversions;

namespace Backend.Infrastructure.Http;

/// <summary>
/// Cliente HTTP genérico com JSON. Lança <see cref="HttpRequestException"/> quando a resposta não é de sucesso.
/// </summary>
public sealed class HttpRequestClient(HttpClient httpClient) : IHttpRequestClient
{
    public async Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        return await ReadAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(requestUri, body, JsonConversions.DefaultOptions, cancellationToken);
        return await ReadAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(requestUri, body, JsonConversions.DefaultOptions, cancellationToken);
        return await ReadAsync<TResponse>(response, cancellationToken);
    }

    public async Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<TResponse?> ReadAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(JsonConversions.DefaultOptions, cancellationToken);
    }
}
