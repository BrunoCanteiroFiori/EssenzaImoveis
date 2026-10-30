using Backend.Domain.SavedProperties;
using Backend.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.SavedProperties;

public sealed class SavedPropertyRepository(AppDbContext dbContext) : RepositoryBase<SavedProperty>(dbContext), ISavedPropertyRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task AddIfNotExistsAsync(SavedProperty savedProperty, CancellationToken cancellationToken = default) =>
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO saved_properties (id, property_id, visitor_hash, saved_at)
            VALUES ({savedProperty.Id}, {savedProperty.PropertyId}, {savedProperty.VisitorHash}, {savedProperty.SavedAt})
            ON CONFLICT (visitor_hash, property_id) DO NOTHING
            """,
            cancellationToken);

    public async Task RemoveAsync(string visitorHash, Guid propertyId, CancellationToken cancellationToken = default) =>
        await Entities
            .Where(savedProperty => savedProperty.VisitorHash == visitorHash && savedProperty.PropertyId == propertyId)
            .ExecuteDeleteAsync(cancellationToken);
}
