using Backend.Domain.Common;

namespace Backend.Domain.ContactMessages;

/// <summary>
/// Mensagem enviada pelo site por um possível cliente, para retorno manual da corretora.
/// </summary>
public sealed class ContactMessage : AggregateRoot
{
    public const int NameMaxLength = 100;
    public const int PhoneMinDigits = 10;
    public const int PhoneMaxDigits = 13;
    public const int MessageMaxLength = 2000;
    public const int InternalNotesMaxLength = 2000;

    private ContactMessage()
    {
    }

    public Guid? PropertyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public ContactMessageStatus Status { get; private set; }

    public string? InternalNotes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ContactMessage Create(
        Guid? propertyId,
        string name,
        string? email,
        string? phone,
        string message,
        DateTimeOffset now)
    {
        var contactMessage = new ContactMessage
        {
            PropertyId = propertyId == Guid.Empty ? null : propertyId,
            Status = ContactMessageStatus.New,
            CreatedAt = now.ToUniversalTime(),
            UpdatedAt = now.ToUniversalTime(),
        };

        contactMessage.SetName(name);
        contactMessage.SetContact(email, phone);
        contactMessage.SetMessage(message);
        return contactMessage;
    }

    public void ChangeStatus(ContactMessageStatus status, DateTimeOffset now)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Status inválido.");
        }

        if (status == ContactMessageStatus.New && Status != ContactMessageStatus.New)
        {
            throw new DomainException("Uma mensagem já tratada não pode voltar para o status Nova.");
        }

        Status = status;
        UpdatedAt = now.ToUniversalTime();
    }

    public void UpdateInternalNotes(string? internalNotes, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(internalNotes))
        {
            InternalNotes = null;
        }
        else if (internalNotes.Trim().Length > InternalNotesMaxLength)
        {
            throw new DomainException($"As anotações devem ter no máximo {InternalNotesMaxLength} caracteres.");
        }
        else
        {
            InternalNotes = internalNotes.Trim();
        }

        UpdatedAt = now.ToUniversalTime();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("O campo Nome é obrigatório.");
        }

        if (name.Trim().Length > NameMaxLength)
        {
            throw new DomainException($"O campo Nome deve ter no máximo {NameMaxLength} caracteres.");
        }

        Name = name.Trim();
    }

    private void SetContact(string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("Informe um e-mail ou um telefone para retorno.");
        }

        Email = string.IsNullOrWhiteSpace(email) ? null : EmailAddress.Normalize(email);

        if (string.IsNullOrWhiteSpace(phone))
        {
            Phone = null;
            return;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length is < PhoneMinDigits or > PhoneMaxDigits)
        {
            throw new DomainException("O telefone deve ter DDD e número (10 a 13 dígitos).");
        }

        Phone = digits;
    }

    private void SetMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new DomainException("O campo Mensagem é obrigatório.");
        }

        if (message.Trim().Length > MessageMaxLength)
        {
            throw new DomainException($"O campo Mensagem deve ter no máximo {MessageMaxLength} caracteres.");
        }

        Message = message.Trim();
    }
}
