using System.ComponentModel.DataAnnotations;
using Backend.Domain.ContactMessages;

namespace Backend.Application.ContactMessages.Dtos;

public sealed record UpdateContactMessageRequest(
    ContactMessageStatus Status,
    [MaxLength(ContactMessage.InternalNotesMaxLength)] string? InternalNotes);
