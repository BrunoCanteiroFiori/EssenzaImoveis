using System.ComponentModel.DataAnnotations;
using Backend.Domain.Common;
using Backend.Domain.ContactMessages;

namespace Backend.Application.ContactMessages.Dtos;

/// <summary>
/// Mensagem enviada pelo site. <see cref="Website"/> é um campo armadilha (honeypot): fica oculto no formulário
/// e, se vier preenchido, a mensagem é descartada como spam.
/// </summary>
public sealed record CreateContactMessageRequest(
    Guid? PropertyId,
    [Required, MaxLength(ContactMessage.NameMaxLength)] string Name,
    [MaxLength(EmailAddress.MaxLength)] string? Email,
    [MaxLength(20)] string? Phone,
    [Required, MaxLength(ContactMessage.MessageMaxLength)] string Message,
    [MaxLength(200)] string? Website = null);
