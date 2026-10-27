using Backend.Application.AuditLogs.Dtos;
using Backend.Application.Common;
using Backend.Application.Common.Dtos;
using Backend.Domain.AuditLogs;

namespace Backend.Application.AuditLogs;

public sealed class AuditLogService(IAuditLogRepository auditLogRepository) : IAuditLogService
{
    public async Task<PagedResponse<AuditLogResponse>> ListAsync(ListAuditLogsRequest request, CancellationToken cancellationToken = default)
    {
        var page = await auditLogRepository.SearchAsync(request.ToFilter(), cancellationToken);
        return page.ToResponse(auditLog => auditLog.ToResponse());
    }

    public async Task<Result<AuditLogResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var auditLog = await auditLogRepository.GetByIdAsync(id, cancellationToken);
        return auditLog is null ? AuditLogErrors.NotFound(id) : auditLog.ToResponse();
    }
}
