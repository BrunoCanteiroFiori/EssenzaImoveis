using Backend.Application.Properties.Dtos;

namespace Backend.Application.Dashboard.Dtos;

public sealed record DashboardSummaryResponse(
    int AvailableProperties,
    int ReservedProperties,
    int SoldProperties,
    int RentedProperties,
    int InactiveProperties,
    int SponsoredProperties,
    int NewMessages,
    int RecentViews,
    IReadOnlyList<PropertyManagementSummaryResponse> HottestProperties);
