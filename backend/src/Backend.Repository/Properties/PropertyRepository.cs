using System.Linq.Expressions;
using Backend.Domain.Common;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;
using Backend.Domain.SavedProperties;
using Backend.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Properties;

public sealed class PropertyRepository(AppDbContext dbContext) : RepositoryBase<Property>(dbContext), IPropertyRepository
{
    // Os enums são gravados em inglês; estas ordens seguem os rótulos em pt-BR exibidos na gestão (labels.ts do frontend).
    private static readonly Expression<Func<Property, int>> _typeLabelOrder = property =>
        property.Type == PropertyType.Apartment ? 0
        : property.Type == PropertyType.House ? 1
        : property.Type == PropertyType.Commercial ? 2
        : property.Type == PropertyType.Rural ? 3
        : 4;

    private static readonly Expression<Func<Property, int>> _statusLabelOrder = property =>
        property.Status == PropertyStatus.Rented ? 0
        : property.Status == PropertyStatus.Available ? 1
        : property.Status == PropertyStatus.Inactive ? 2
        : property.Status == PropertyStatus.Reserved ? 3
        : 4;

    private readonly DbSet<PropertyView> _views = dbContext.Set<PropertyView>();
    private readonly DbSet<SavedProperty> _savedProperties = dbContext.Set<SavedProperty>();

    public async Task<PagedList<Property>> SearchAsync(
        PropertySearchFilter filter,
        DateTimeOffset now,
        DateTimeOffset viewsSince,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = now.ToUniversalTime();
        var query = ApplyFilter(Entities.AsNoTracking(), filter, nowUtc);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await Sort(query, filter, nowUtc, viewsSince.ToUniversalTime())
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Property>(items, totalCount, filter.Page, filter.PageSize);
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

    public async Task<IReadOnlyList<Property>> GetRecentlyViewedAsync(string visitorHash, int limit, CancellationToken cancellationToken = default) =>
        await Entities.AsNoTracking()
            .Where(property => property.Status == PropertyStatus.Available
                && _views.Any(view => view.PropertyId == property.Id && view.VisitorHash == visitorHash))
            .OrderByDescending(property => _views
                .Where(view => view.PropertyId == property.Id && view.VisitorHash == visitorHash)
                .Max(view => view.ViewedAt))
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Property>> GetSavedAsync(string visitorHash, int limit, CancellationToken cancellationToken = default) =>
        await Entities.AsNoTracking()
            .Join(
                _savedProperties.Where(savedProperty => savedProperty.VisitorHash == visitorHash),
                property => property.Id,
                savedProperty => savedProperty.PropertyId,
                (property, savedProperty) => new { Property = property, savedProperty.SavedAt })
            .OrderByDescending(saved => saved.SavedAt)
            .Select(saved => saved.Property)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Property>> GetSuggestionCandidatesAsync(
        IReadOnlyCollection<PropertyPurpose> purposes,
        IReadOnlyCollection<Guid> excludedIds,
        int limit,
        CancellationToken cancellationToken = default) =>
        await Entities.AsNoTracking()
            .Where(property => property.Status == PropertyStatus.Available
                && purposes.Contains(property.Purpose)
                && !excludedIds.Contains(property.Id))
            .OrderByDescending(property => property.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

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

    private static IQueryable<Property> ApplyFilter(IQueryable<Property> query, PropertySearchFilter filter, DateTimeOffset now)
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

        if (filter.SponsoredOnly)
        {
            query = query.Where(property => property.SponsoredUntil > now);
        }

        return query;
    }

    private static IOrderedQueryable<Property> OrderBy<TKey>(IQueryable<Property> query, Expression<Func<Property, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);

    private IQueryable<Property> Sort(IQueryable<Property> query, PropertySearchFilter filter, DateTimeOffset now, DateTimeOffset viewsSince) =>
        filter.SortField is { } field
            ? SortByField(query, field, filter.SortDescending, viewsSince).ThenByDescending(property => property.CreatedAt)
            : SortByOrder(query, filter.Sort, now, viewsSince);

    private IOrderedQueryable<Property> SortByField(IQueryable<Property> query, PropertySortField field, bool descending, DateTimeOffset viewsSince) =>
        field switch
        {
            PropertySortField.Title => OrderBy(query, property => property.Title, descending),
            PropertySortField.Type => OrderBy(query, _typeLabelOrder, descending),
            PropertySortField.Price => OrderBy(query, property => property.Price, descending),
            PropertySortField.Status => OrderBy(query, _statusLabelOrder, descending),
            _ => OrderBy(query, property => _views.Count(view => view.PropertyId == property.Id && view.ViewedAt >= viewsSince), descending),
        };

    private IQueryable<Property> SortByOrder(IQueryable<Property> query, PropertySortOrder sort, DateTimeOffset now, DateTimeOffset viewsSince) =>
        sort switch
        {
            PropertySortOrder.Newest => query.OrderByDescending(property => property.CreatedAt),
            PropertySortOrder.PriceAscending => query.OrderBy(property => property.Price).ThenByDescending(property => property.CreatedAt),
            PropertySortOrder.PriceDescending => query.OrderByDescending(property => property.Price).ThenByDescending(property => property.CreatedAt),
            PropertySortOrder.Hottest => query
                .OrderByDescending(property => _views.Count(view => view.PropertyId == property.Id && view.ViewedAt >= viewsSince))
                .ThenByDescending(property => property.CreatedAt),
            _ => query
                .Select(property => new PropertyRecommendationSignals
                {
                    Property = property,
                    RecentViews = _views.Count(view => view.PropertyId == property.Id && view.ViewedAt >= viewsSince),
                    IsSponsored = property.SponsoredUntil != null && property.SponsoredUntil > now,
                })
                .OrderByDescending(PropertyRecommendation.Score)
                .ThenByDescending(signals => signals.Property.CreatedAt)
                .Select(signals => signals.Property),
        };
}
