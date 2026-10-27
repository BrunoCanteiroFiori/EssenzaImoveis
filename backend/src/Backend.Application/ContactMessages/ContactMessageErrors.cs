using Backend.Application.Common;

namespace Backend.Application.ContactMessages;

public static class ContactMessageErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("ContactMessage.NotFound", $"Mensagem com id '{id}' não foi encontrada.");

    public static Error Invalid(string message) => Error.Validation("ContactMessage.Invalid", message);

    public static Error PropertyNotFound(Guid propertyId) =>
        Error.Validation("ContactMessage.PropertyNotFound", $"O imóvel '{propertyId}' não está disponível.");
}
