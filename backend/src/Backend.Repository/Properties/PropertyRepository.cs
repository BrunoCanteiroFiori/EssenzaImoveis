using Backend.Domain.Common;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;
using Backend.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Properties;

public sealed class PropertyRepository(AppDbContext dbContext) : RepositoryBase<Property>(dbContext), IPropertyRepository
{
    private readonly DbSet<PropertyView> _views = dbContext.Set<PropertyView>();

    public async Task<PagedList<Property>> SearchAsync(
        PropertySearchFilter filter,
        DateTimeOffset now,
        DateTimeOffset viewsSince,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(Entities.AsNoTracking(), filter);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await Sort(query, filter.Sort, now.ToUniversalTime(), viewsSince.ToUniversalTime())
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Property>(items, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<IReadOnlyList<Property>> GetSponsoredAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken = default)
    {
        var nowUtc = now.ToUniversalTime();
        return await Entities.AsNoTracking()
            .Where(property => property.Status == PropertyStatus.Available && property.SponsoredUntil > nowUtc)
            .OrderByDescending(property => property.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Property>> GetHottestAsync(DateTimeOffset viewsSince, int limit, CancellationToken cancellationToken = default)
    {
        var sinceUtc = viewsSince.ToUniversalTime();
        return await Entities.AsNoTracking()
            .Where(property => property.Status == PropertyStatus.Available
                && _views.Any(view => view.PropertyId == property.Id && view.ViewedAt >= sinceUtc))
            .OrderByDescending(property => _views.Count(view => view.PropertyId == property.Id && view.ViewedAt >= sinceUtc))
            .ThenByDescending(property => property.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PropertyLocation>> GetAvailableLocationsAsync(CancellationToken cancellationToken = default)
    {
        var locations = await Entities.AsNoTracking()
            .Where(property => property.Status == PropertyStatus.Available)
            .Select(property => new { property.Address.Neighborhood, property.Address.City, property.Address.State })
            .Distinct()
            .OrderBy(location => location.State)
            .ThenBy(location => location.City)
            .ThenBy(location => location.Neighborhood)
            .ToListAsync(cancellationToken);

        return [.. locations.Select(location => new PropertyLocation(location.Neighborhood, location.City, location.State))];
    }

    public async Task<IReadOnlyDictionary<PropertyStatus, int>> CountByStatusAsync(CancellationToken cancellationToken = default) =>
        await Entities.AsNoTracking()
            .GroupBy(property => property.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

    public async Task<int> CountSponsoredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var nowUtc = now.ToUniversalTime();
        return await Entities.CountAsync(
            property => property.Status == PropertyStatus.Available && property.SponsoredUntil > nowUtc,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        await Entities.AsNoTracking()
            .Where(property => ids.Contains(property.Id))
            .ToDictionaryAsync(property => property.Id, property => property.Title, cancellationToken);

    private static IQueryable<Property> ApplyFilter(IQueryable<Property> query, PropertySearchFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Text))
        {
            var pattern = QueryPatterns.Contains(filter.Text);
            query = query.Where(property =>
                EF.Functions.ILike(EF.Functions.Unaccent(property.Title), EF.Functions.Unaccent(pattern), QueryPatterns.LikeEscapeCharacter)
                || EF.Functions.ILike(EF.Functions.Unaccent(property.Address.Neighborhood), EF.Functions.Unaccent(pattern), QueryPatterns.LikeEscapeCharacter)
                || EF.Functions.ILike(EF.Functions.Unaccent(property.Address.City), EF.Functions.Unaccent(pattern), QueryPatterns.LikeEscapeCharacter)
                || EF.Functions.ILike(property.Address.State, pattern, QueryPatterns.LikeEscapeCharacter));
        }

        if (filter.Type is { } type)
        {
            query = query.Where(property => property.Type == type);
        }

        if (filter.Purpose is { } purpose)
        {
            query = query.Where(property => property.Purpose == purpose);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(property => property.Status == status);
        }

        if (filter.MinBedrooms is { } minBedrooms)
        {
            query = query.Where(property => property.Bedrooms >= minBedrooms);
        }

        if (filter.MinBathrooms is { } minBathrooms)
        {
            query = query.Where(property => property.Bathrooms >= minBathrooms);
        }

        if (filter.MinParkingSpaces is { } minParkingSpaces)
        {
            query = query.Where(property => property.ParkingSpaces >= minParkingSpaces);
        }

        if (filter.MinArea is { } minArea)
        {
            query = query.Where(property => property.Area >= minArea);
        }

        if (filter.MaxArea is { } maxArea)
        {
            query = query.Where(property => property.Area <= maxArea);
        }

        if (filter.MinPrice is { } minPrice)
        {
            query = query.Where(property => property.Price >= minPrice);
        }

        if (filter.MaxPrice is { } maxPrice)
        {
            query = query.Where(property => property.Price <= maxPrice);
        }

        return query;
    }

    private IQueryable<Property> Sort(IQueryable<Property> query, PropertySortOrder sort, DateTimeOffset now, DateTimeOffset viewsSince) =>
        sort switch
        {
            PropertySortOrder.Newest => query.OrderByDescending(property => property.CreatedAt),
            PropertySortOrder.PriceAscending => query.OrderBy(property => property.Price).ThenByDescending(property => property.CreatedAt),
            PropertySortOrder.PriceDescending => query.OrderByDescending(property => property.Price).ThenByDescending(property => property.CreatedAt),
            PropertySortOrder.Hottest => query
                .OrderByDescending(property => _views.Count(view => view.PropertyId == property.Id && view.ViewedAt >= viewsSince))
                .ThenByDescending(property => property.CreatedAt),
            _ => query
                .OrderByDescending(property => property.SponsoredUntil != null && property.SponsoredUntil > now)
                .ThenByDescending(property => property.CreatedAt),
        };
}
