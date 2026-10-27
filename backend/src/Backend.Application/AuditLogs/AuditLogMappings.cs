using System.Text.Json;
using Backend.Application.AuditLogs.Dtos;
using Backend.Domain.AuditLogs;

namespace Backend.Application.AuditLogs;

public static class AuditLogMappings
{
    public static AuditLogResponse ToResponse(this AuditLog auditLog)
    {
        using var changes = JsonDocument.Parse(auditLog.Changes);
        return new AuditLogResponse(
            auditLog.Id,
            auditLog.EntityName,
            auditLog.EntityId,
            auditLog.Action,
            changes.RootElement.Clone(),
            auditLog.UserId,
            auditLog.UserEmail,
            auditLog.OccurredAt);
    }

    public static AuditLogFilter ToFilter(this ListAuditLogsRequest request) => new(
        request.EntityName,
        request.EntityId,
        request.Action,
        request.UserId,
        request.From,
        request.To,
        request.Page,
        request.PageSize);
}
