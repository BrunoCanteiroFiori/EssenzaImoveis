using Backend.Application.Common;

namespace Backend.Application.AuditLogs;

public static class AuditLogErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("AuditLog.NotFound", $"Registro de auditoria com id '{id}' não foi encontrado.");
}
