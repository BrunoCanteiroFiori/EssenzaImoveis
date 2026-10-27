using Backend.Domain.Abstractions;
using Backend.Domain.Common;

namespace Backend.Domain.AuditLogs;

/// <summary>
/// Os registros são gravados automaticamente na persistência; a camada Repository rejeita alteração e exclusão de auditoria.
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<PagedList<AuditLog>> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}
