namespace Backend.Infrastructure.ZipCodes;

/// <summary>
/// Endereço de um CEP como o serviço externo devolveu. Logradouro e bairro vêm vazios em CEP geral de cidade pequena.
/// </summary>
public sealed record ZipCodeAddress(string ZipCode, string Street, string Neighborhood, string City, string State);
