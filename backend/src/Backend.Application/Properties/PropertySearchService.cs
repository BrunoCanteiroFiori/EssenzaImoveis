using Backend.Application.Common;
using Backend.Application.Common.Dtos;
using Backend.Application.Properties.Dtos;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;

namespace Backend.Application.Properties;

public sealed class PropertySearchService(
    IPropertyRepository propertyRepository,
    IPropertyViewRepository propertyViewRepository,
    TimeProvider timeProvider) : IPropertySearchService
{
    public const int FeaturedLimit = 6;

    public Task<PagedResponse<PropertySummaryResponse>> SearchAsync(SearchPropertiesRequest request, CancellationToken cancellationToken = default) =>
        SearchAsync(request.ToFilter(), cancellationToken);

    public Task<PagedResponse<PropertySummaryResponse>> ListAsync(ListPropertiesRequest request, CancellationToken cancellationToken = default) =>
        SearchAsync(request.ToFilter(), cancellationToken);

    public async Task<Result<PropertyDetailsResponse>> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property is null || !property.IsPubliclyVisible)
        {
            return PropertyErrors.NotFound(id);
        }

        var now = timeProvider.GetUtcNow();
        var views = await CountRecentViewsAsync([property], now, cancellationToken);
        return property.ToDetailsResponse(now, views.GetValueOrDefault(property.Id));
    }

    public async Task<FeaturedPropertiesResponse> GetFeaturedAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var viewsSince = now - PropertyTemperature.Window;

        var sponsored = await propertyRepository.GetSponsoredAsync(now, FeaturedLimit, cancellationToken);
        var hottest = await propertyRepository.GetHottestAsync(viewsSince, FeaturedLimit, cancellationToken);
        var newest = await propertyRepository.SearchAsync(NewestAvailableFilter(), now, viewsSince, cancellationToken);

        var views = await CountRecentViewsAsync([.. sponsored, .. hottest, .. newest.Items], now, cancellationToken);
        return new FeaturedPropertiesResponse(
            ToSummaries(sponsored, now, views),
            ToSummaries(hottest, now, views),
            ToSummaries(newest.Items, now, views));
    }

    public async Task<IReadOnlyList<PropertyLocationResponse>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var locations = await propertyRepository.GetAvailableLocationsAsync(cancellationToken);
        return [.. locations.Select(location => location.ToResponse())];
    }

    private static PropertySearchFilter NewestAvailableFilter() => new(
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
        Sort: PropertySortOrder.Newest,
        Page: 1,
        PageSize: FeaturedLimit);

    private static IReadOnlyList<PropertySummaryResponse> ToSummaries(
        IEnumerable<Property> properties,
        DateTimeOffset now,
        IReadOnlyDictionary<Guid, int> views) =>
        [.. properties.Select(property => property.ToSummaryResponse(now, views.GetValueOrDefault(property.Id)))];

    private async Task<PagedResponse<PropertySummaryResponse>> SearchAsync(PropertySearchFilter filter, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var page = await propertyRepository.SearchAsync(filter, now, now - PropertyTemperature.Window, cancellationToken);
        var views = await CountRecentViewsAsync(page.Items, now, cancellationToken);
        return page.ToResponse(property => property.ToSummaryResponse(now, views.GetValueOrDefault(property.Id)));
    }

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
