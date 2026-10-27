namespace Backend.Application.Properties.Dtos;

public sealed record FeaturedPropertiesResponse(
    IReadOnlyList<PropertySummaryResponse> Sponsored,
    IReadOnlyList<PropertySummaryResponse> Hottest,
    IReadOnlyList<PropertySummaryResponse> Newest);
