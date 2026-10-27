using Backend.Application.Common;

namespace Backend.Application.Properties;

public static class PropertyErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("Property.NotFound", $"Imóvel com id '{id}' não foi encontrado.");

    public static Error Invalid(string message) => Error.Validation("Property.Invalid", message);

    public static Error InvalidImage(string message) => Error.Validation("Property.InvalidImage", message);

    public static Error ImageNotFound(string fileName) => Error.NotFound("Property.ImageNotFound", $"A imagem '{fileName}' não foi encontrada no imóvel.");
}
