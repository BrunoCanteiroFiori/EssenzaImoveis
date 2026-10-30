using Backend.Application.Common;
using Backend.Application.Common.Dtos;
using Backend.Application.Properties.Dtos;
using Backend.Domain.Common;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;

namespace Backend.Application.Properties;

public sealed class PropertySearchService(
    IPropertyRepository propertyRepository,
    IPropertyViewRepository propertyViewRepository,
    TimeProvider timeProvider) : IPropertySearchService
{
    public const int FeaturedLimit = 6;
    public const int RecentlyViewedLimit = 12;
    public const int SuggestionLimit = 6;
    public const int SuggestionViewedLimit = 10;
    public const int SuggestionCandidateLimit = 200;

    public async Task<PagedResponse<PropertySummaryResponse>> SearchAsync(SearchPropertiesRequest request, CancellationToken cancellationToken = default)
    {
        var page = await SearchAsync(request.ToFilter(), timeProvider.GetUtcNow(), cancellationToken);
        return page.ToResponse(property => property.ToSummaryResponse());
    }

    public async Task<PagedResponse<PropertyManagementSummaryResponse>> ListAsync(ListPropertiesRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var page = await SearchAsync(request.ToFilter(), now, cancellationToken);
        var views = await CountRecentViewsAsync(page.Items, now, cancellationToken);
        return page.ToResponse(property => property.ToManagementSummaryResponse(now, views.GetValueOrDefault(property.Id)));
    }

    public async Task<Result<PropertyDetailsResponse>> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property is null || !property.IsPubliclyVisible)
        {
            return PropertyErrors.NotFound(id);
        }

        return property.ToDetailsResponse();
    }

    public async Task<FeaturedPropertiesResponse> GetFeaturedAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var topScoredFilter = FeaturedFilter(PropertySortOrder.Relevance);

        var sponsored = await SearchAsync(topScoredFilter with { SponsoredOnly = true }, now, cancellationToken);
        var topScored = await SearchAsync(topScoredFilter, now, cancellationToken);
        var forSale = await SearchAsync(topScoredFilter with { Purpose = PropertyPurpose.Sale }, now, cancellationToken);
        var forRent = await SearchAsync(topScoredFilter with { Purpose = PropertyPurpose.Rent }, now, cancellationToken);
        var mostSearched = await propertyRepository.GetHottestAsync(now - PropertyTemperature.Window, FeaturedLimit, cancellationToken);
        var newest = await SearchAsync(FeaturedFilter(PropertySortOrder.Newest), now, cancellationToken);

        return new FeaturedPropertiesResponse(
            ToSummaries(PropertyRecommendation.SelectHighlights(sponsored.Items, topScored.Items)),
            ToSummaries(forSale.Items),
            ToSummaries(forRent.Items),
            ToSummaries(mostSearched),
            ToSummaries(newest.Items));
    }

    public async Task<IReadOnlyList<PropertyLocationResponse>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var locations = await propertyRepository.GetAvailableLocationsAsync(cancellationToken);
        return [.. locations.Select(location => location.ToResponse())];
    }

    public async Task<IReadOnlyList<PropertySummaryResponse>> GetRecentlyViewedAsync(string visitorId, CancellationToken cancellationToken = default) =>
        ToSummaries(await propertyRepository.GetRecentlyViewedAsync(visitorId.ToVisitorHash(), RecentlyViewedLimit, cancellationToken));

    public async Task<Result<IReadOnlyList<PropertySummaryResponse>>> GetSuggestionsAsync(Guid id, string? visitorId, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property is null || !property.IsPubliclyVisible)
        {
            return PropertyErrors.NotFound(id);
        }

        IReadOnlyList<Property> recentlyViewed = string.IsNullOrWhiteSpace(visitorId)
            ? []
            : await propertyRepository.GetRecentlyViewedAsync(visitorId.ToVisitorHash(), SuggestionViewedLimit, cancellationToken);
        List<Property> viewed = [property, .. recentlyViewed.Where(other => other.Id != id)];

        var candidates = await propertyRepository.GetSuggestionCandidatesAsync(
            [.. viewed.Select(other => other.Purpose).Distinct()],
            [.. viewed.Select(other => other.Id)],
            SuggestionCandidateLimit,
            cancellationToken);

        return Result<IReadOnlyList<PropertySummaryResponse>>.Success(ToSummaries(PropertySuggestion.Rank(candidates, viewed, SuggestionLimit)));
    }

    private static PropertySearchFilter FeaturedFilter(PropertySortOrder sort) => new(
        Text: null,
        Type: null,
        Purpose: null,
        Status: PropertyStatus.Available,
        MinBedrooms: null,
        MinBathrooms: null,
        MinParkingSpaces: null,
        MinArea: null,
        MaxArea: null,
        MinPrice: null,
        MaxPrice: null,
        Sort: sort,
        Page: 1,
        PageSize: FeaturedLimit);

    private static IReadOnlyList<PropertySummaryResponse> ToSummaries(IEnumerable<Property> properties) =>
        [.. properties.Select(property => property.ToSummaryResponse())];

    private Task<PagedList<Property>> SearchAsync(PropertySearchFilter filter, DateTimeOffset now, CancellationToken cancellationToken) =>
        propertyRepository.SearchAsync(filter, now, now - PropertyTemperature.Window, cancellationToken);

    private async Task<IReadOnlyDictionary<Guid, int>> CountRecentViewsAsync(
        IEnumerable<Property> properties,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var ids = properties.Select(property => property.Id).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await propertyViewRepository.CountByPropertySinceAsync(ids, now - PropertyTemperature.Window, cancellationToken);
    }
}
