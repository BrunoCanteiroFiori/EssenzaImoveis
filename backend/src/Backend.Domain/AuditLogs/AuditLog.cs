using Backend.Domain.Common;

namespace Backend.Domain.AuditLogs;

/// <summary>
/// Registro de auditoria de uma inclusão, alteração ou exclusão. É somente de inclusão: não existe método para alterar.
/// </summary>
public sealed class AuditLog : AggregateRoot
{
    public const int EntityNameMaxLength = 100;
    public const int EntityIdMaxLength = 100;

    private AuditLog()
    {
    }

    public string EntityName { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public AuditAction Action { get; private set; }

    /// <summary>
    /// Valores em JSON: novos valores na inclusão, valores removidos na exclusão e <c>{ campo: { old, new } }</c> na alteração.
    /// </summary>
    public string Changes { get; private set; } = "{}";

    public Guid? UserId { get; private set; }

    public string? UserEmail { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static AuditLog Create(
        string entityName,
        string entityId,
        AuditAction action,
        string changes,
        Guid? userId,
        string? userEmail,
        DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(entityName) || entityName.Length > EntityNameMaxLength)
        {
            throw new DomainException("Nome da entidade auditada inválido.");
        }

        if (string.IsNullOrWhiteSpace(entityId) || entityId.Length > EntityIdMaxLength)
        {
            throw new DomainException("Id da entidade auditada inválido.");
        }

        return new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Changes = string.IsNullOrWhiteSpace(changes) ? "{}" : changes,
            UserId = userId,
            UserEmail = string.IsNullOrWhiteSpace(userEmail) ? null : userEmail,
            OccurredAt = occurredAt.ToUniversalTime(),
        };
    }
}
