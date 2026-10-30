using Backend.Application.Dashboard.Dtos;
using Backend.Application.Properties;
using Backend.Domain.ContactMessages;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;

namespace Backend.Application.Dashboard;

public sealed class DashboardService(
    IPropertyRepository propertyRepository,
    IPropertyViewRepository propertyViewRepository,
    IContactMessageRepository contactMessageRepository,
    TimeProvider timeProvider) : IDashboardService
{
    public const int HottestLimit = 5;

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var viewsSince = now - PropertyTemperature.Window;

        var byStatus = await propertyRepository.CountByStatusAsync(cancellationToken);
        var sponsored = await propertyRepository.CountSponsoredAsync(now, cancellationToken);
        var newMessages = await contactMessageRepository.CountByStatusAsync(ContactMessageStatus.New, cancellationToken);
        var recentViews = await propertyViewRepository.CountSinceAsync(viewsSince, cancellationToken);
        var hottest = await propertyRepository.GetHottestAsync(viewsSince, HottestLimit, cancellationToken);
        var views = hottest.Count == 0
            ? new Dictionary<Guid, int>()
            : await propertyViewRepository.CountByPropertySinceAsync([.. hottest.Select(property => property.Id)], viewsSince, cancellationToken);

        return new DashboardSummaryResponse(
            byStatus.GetValueOrDefault(PropertyStatus.Available),
            byStatus.GetValueOrDefault(PropertyStatus.Reserved),
            byStatus.GetValueOrDefault(PropertyStatus.Sold),
            byStatus.GetValueOrDefault(PropertyStatus.Rented),
            byStatus.GetValueOrDefault(PropertyStatus.Inactive),
            sponsored,
            newMessages,
            recentViews,
            [.. hottest.Select(property => property.ToManagementSummaryResponse(now, views.GetValueOrDefault(property.Id)))]);
    }
}
