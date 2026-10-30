namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Vitrine da página inicial. Cada lista é independente: um imóvel pode aparecer em mais de uma.
/// </summary>
public sealed record FeaturedPropertiesResponse(
    IReadOnlyList<PropertySummaryResponse> Highlights,
    IReadOnlyList<PropertySummaryResponse> ForSale,
    IReadOnlyList<PropertySummaryResponse> ForRent,
    IReadOnlyList<PropertySummaryResponse> MostSearched,
    IReadOnlyList<PropertySummaryResponse> Newest);
