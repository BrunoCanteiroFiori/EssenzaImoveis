using System.Text.Json;
using Backend.Infrastructure.Http;

namespace Backend.Infrastructure.ZipCodes;

/// <summary>
/// Consulta de CEP pelo ViaCEP (https://viacep.com.br): gratuito, sem chave de acesso. Desiste após alguns segundos sem resposta.
/// </summary>
public sealed class ViaCepZipCodeClient(IHttpRequestClient httpRequestClient) : IZipCodeClient
{
    private const string BaseUrl = "https://viacep.com.br/ws";
    private const int ZipCodeLength = 8;

    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(5);

    public async Task<ZipCodeAddress?> FindAsync(string zipCode, CancellationToken cancellationToken = default)
    {
        if (zipCode.Length != ZipCodeLength || !zipCode.All(char.IsAsciiDigit))
        {
            throw new ArgumentException($"O CEP deve ter {ZipCodeLength} dígitos, sem máscara.", nameof(zipCode));
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_requestTimeout);

        ViaCepResponse? response;
        try
        {
            response = await httpRequestClient.GetAsync<ViaCepResponse>($"{BaseUrl}/{zipCode}/json/", timeout.Token);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HttpRequestException("O ViaCEP não respondeu a tempo.", exception);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("O ViaCEP respondeu em formato inesperado.", exception);
        }

        if (response is null || response.Error is not null)
        {
            return null;
        }

        return new ZipCodeAddress(
            zipCode,
            response.Street ?? string.Empty,
            response.Neighborhood ?? string.Empty,
            response.City ?? string.Empty,
            response.State ?? string.Empty);
    }
}
