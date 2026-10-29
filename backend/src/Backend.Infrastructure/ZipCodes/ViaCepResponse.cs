using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Infrastructure.ZipCodes;

/// <summary>
/// Resposta do ViaCEP. CEP inexistente volta com status 200 e <c>"erro": true</c> (ou <c>"erro": "true"</c>).
/// </summary>
internal sealed record ViaCepResponse(
    [property: JsonPropertyName("logradouro")] string? Street,
    [property: JsonPropertyName("bairro")] string? Neighborhood,
    [property: JsonPropertyName("localidade")] string? City,
    [property: JsonPropertyName("uf")] string? State,
    [property: JsonPropertyName("erro")] JsonElement? Error);
