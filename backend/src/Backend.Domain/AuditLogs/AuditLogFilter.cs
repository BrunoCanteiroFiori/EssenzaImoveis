namespace Backend.Domain.AuditLogs;

public sealed record AuditLogFilter(
    string? EntityName,
    string? EntityId,
    AuditAction? Action,
    Guid? UserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize);
