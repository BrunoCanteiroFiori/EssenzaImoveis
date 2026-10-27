using Backend.Domain.PropertyViews;
using Backend.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.PropertyViews;

public sealed class PropertyViewRepository(AppDbContext dbContext) : RepositoryBase<PropertyView>(dbContext), IPropertyViewRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<bool> AddIfNotExistsAsync(PropertyView view, CancellationToken cancellationToken = default)
    {
        var inserted = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO property_views (id, property_id, visitor_hash, viewed_on, viewed_at)
            VALUES ({view.Id}, {view.PropertyId}, {view.VisitorHash}, {view.ViewedOn}, {view.ViewedAt})
            ON CONFLICT (property_id, visitor_hash, viewed_on) DO NOTHING
            """,
            cancellationToken);

        return inserted > 0;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountByPropertySinceAsync(
        IReadOnlyCollection<Guid> propertyIds,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        var sinceUtc = since.ToUniversalTime();
        return await Entities.AsNoTracking()
            .Where(view => propertyIds.Contains(view.PropertyId) && view.ViewedAt >= sinceUtc)
            .GroupBy(view => view.PropertyId)
            .Select(group => new { PropertyId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.PropertyId, group => group.Count, cancellationToken);
    }

    public async Task<int> CountSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        var sinceUtc = since.ToUniversalTime();
        return await Entities.CountAsync(view => view.ViewedAt >= sinceUtc, cancellationToken);
    }
}
