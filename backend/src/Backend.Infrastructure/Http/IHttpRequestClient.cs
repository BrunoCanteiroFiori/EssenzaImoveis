namespace Backend.Infrastructure.Http;

public interface IHttpRequestClient
{
    Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

    Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);
}
