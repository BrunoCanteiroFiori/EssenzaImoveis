using Backend.Domain.ContactMessages;

namespace Backend.Application.ContactMessages.Dtos;

public sealed record ContactMessageResponse(
    Guid Id,
    Guid? PropertyId,
    string? PropertyTitle,
    string Name,
    string? Email,
    string? Phone,
    string Message,
    ContactMessageStatus Status,
    string? InternalNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
