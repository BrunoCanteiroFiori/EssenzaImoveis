using Backend.Application.ContactMessages.Dtos;
using Backend.Domain.ContactMessages;

namespace Backend.Application.ContactMessages;

public static class ContactMessageMappings
{
    public static ContactMessageResponse ToResponse(this ContactMessage contactMessage, string? propertyTitle) => new(
        contactMessage.Id,
        contactMessage.PropertyId,
        propertyTitle,
        contactMessage.Name,
        contactMessage.Email,
        contactMessage.Phone,
        contactMessage.Message,
        contactMessage.Status,
        contactMessage.InternalNotes,
        contactMessage.CreatedAt,
        contactMessage.UpdatedAt);
}
