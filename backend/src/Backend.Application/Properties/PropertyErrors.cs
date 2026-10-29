using Backend.Application.Common;

namespace Backend.Application.Properties;

public static class PropertyErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("Property.NotFound", $"Imóvel com id '{id}' não foi encontrado.");

    public static Error Invalid(string message) => Error.Validation("Property.Invalid", message);

    public static Error InvalidMedia(string message) => Error.Validation("Property.InvalidMedia", message);

    public static Error MediaNotFound(string fileName) => Error.NotFound("Property.MediaNotFound", $"O arquivo '{fileName}' não foi encontrado no imóvel.");
}
