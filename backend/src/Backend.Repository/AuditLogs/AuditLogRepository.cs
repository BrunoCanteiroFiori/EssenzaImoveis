using Backend.Domain.AuditLogs;
using Backend.Domain.Common;
using Backend.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.AuditLogs;

public sealed class AuditLogRepository(AppDbContext dbContext) : RepositoryBase<AuditLog>(dbContext), IAuditLogRepository
{
    public async Task<PagedList<AuditLog>> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Entities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            query = query.Where(auditLog => auditLog.EntityName == filter.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
        {
            query = query.Where(auditLog => auditLog.EntityId == filter.EntityId);
        }

        if (filter.Action is { } action)
        {
            query = query.Where(auditLog => auditLog.Action == action);
        }

        if (filter.UserId is { } userId)
        {
            query = query.Where(auditLog => auditLog.UserId == userId);
        }

        if (filter.From is { } from)
        {
            var fromUtc = from.ToUniversalTime();
            query = query.Where(auditLog => auditLog.OccurredAt >= fromUtc);
        }

        if (filter.To is { } to)
        {
            var toUtc = to.ToUniversalTime();
            query = query.Where(auditLog => auditLog.OccurredAt <= toUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(auditLog => auditLog.OccurredAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<AuditLog>(items, totalCount, filter.Page, filter.PageSize);
    }
}
