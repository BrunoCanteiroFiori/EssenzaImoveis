namespace Backend.Infrastructure.ZipCodes;

/// <summary>
/// Consulta o endereço de um CEP em um serviço externo.
/// </summary>
public interface IZipCodeClient
{
    /// <summary>
    /// Busca o endereço do CEP (8 dígitos, sem máscara). Retorna nulo se o CEP não existir.
    /// Lança <see cref="HttpRequestException"/> se o serviço estiver fora do ar, demorar demais ou responder em formato inesperado.
    /// </summary>
    Task<ZipCodeAddress?> FindAsync(string zipCode, CancellationToken cancellationToken = default);
}
