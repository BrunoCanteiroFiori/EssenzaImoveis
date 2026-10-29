using Backend.Application.ZipCodes.Dtos;
using Backend.Domain.Common;
using Backend.Infrastructure.ZipCodes;

namespace Backend.Application.ZipCodes;

public static class ZipCodeMappings
{
    /// <summary>
    /// Converte o endereço do serviço externo já com o texto normalizado, igual ao que será gravado no banco.
    /// </summary>
    public static ZipCodeResponse ToResponse(this ZipCodeAddress address) => new(
        address.ZipCode,
        OptionalLine(address.Street),
        OptionalLine(address.Neighborhood),
        TextNormalizer.NormalizeLine(address.City),
        TextNormalizer.NormalizeLine(address.State).ToUpperInvariant());

    private static string? OptionalLine(string value)
    {
        var normalized = TextNormalizer.NormalizeLine(value);
        return normalized.Length == 0 ? null : normalized;
    }
}
