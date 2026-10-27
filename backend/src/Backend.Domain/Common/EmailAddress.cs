using System.Net.Mail;

namespace Backend.Domain.Common;

public static class EmailAddress
{
    public const int MaxLength = 200;

    /// <summary>
    /// Valida e normaliza um e-mail (sem espaços e em minúsculas). Lança <see cref="DomainException"/> se for inválido.
    /// </summary>
    public static string Normalize(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("O e-mail é obrigatório.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength)
        {
            throw new DomainException($"O e-mail deve ter no máximo {MaxLength} caracteres.");
        }

        if (!MailAddress.TryCreate(normalized, out var address) || address.Address != normalized || !address.Host.Contains('.'))
        {
            throw new DomainException("O e-mail informado é inválido.");
        }

        return normalized;
    }
}
