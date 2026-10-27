using Backend.Application.AuditLogs.Dtos;
using Backend.Application.Common;
using Backend.Application.Common.Dtos;

namespace Backend.Application.AuditLogs;

/// <summary>
/// Consulta da auditoria (somente leitura).
/// </summary>
public interface IAuditLogService
{
    Task<PagedResponse<AuditLogResponse>> ListAsync(ListAuditLogsRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuditLogResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
