using Backend.Application.Common;

namespace Backend.Application.ZipCodes;

public static class ZipCodeErrors
{
    public static Error Invalid(string message) => Error.Validation("ZipCode.Invalid", message);

    public static Error NotFound(string zipCode) => Error.NotFound("ZipCode.NotFound", $"O CEP '{zipCode}' não foi encontrado.");

    public static Error Unavailable() =>
        Error.Unavailable("ZipCode.Unavailable", "Não foi possível consultar o CEP agora. Preencha o endereço manualmente.");
}
