using Backend.Application.Common;
using Backend.Application.Common.Dtos;
using Backend.Application.Properties.Dtos;

namespace Backend.Application.Properties;

/// <summary>
/// Consultas de imóveis: busca pública, listagem administrativa (com temperatura e patrocínio), detalhe público, vitrine e localidades.
/// </summary>
public interface IPropertySearchService
{
    Task<PagedResponse<PropertySummaryResponse>> SearchAsync(SearchPropertiesRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<PropertyManagementSummaryResponse>> ListAsync(ListPropertiesRequest request, CancellationToken cancellationToken = default);

    Task<Result<PropertyDetailsResponse>> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FeaturedPropertiesResponse> GetFeaturedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyLocationResponse>> GetLocationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imóveis disponíveis que o visitante viu por último.
    /// </summary>
    Task<IReadOnlyList<PropertySummaryResponse>> GetRecentlyViewedAsync(string visitorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sugestões para quem está vendo um imóvel, a partir dele e do que o visitante viu antes. Sem visitante, usa só o imóvel.
    /// Nunca sugere um imóvel que o visitante já viu.
    /// </summary>
    Task<Result<IReadOnlyList<PropertySummaryResponse>>> GetSuggestionsAsync(Guid id, string? visitorId, CancellationToken cancellationToken = default);
}
