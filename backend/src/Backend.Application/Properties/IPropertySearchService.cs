using Backend.Application.Common;
using Backend.Application.Common.Dtos;
using Backend.Application.Properties.Dtos;

namespace Backend.Application.Properties;

/// <summary>
/// Consultas de imóveis com temperatura: busca pública, listagem administrativa, detalhe público, destaques e localidades.
/// </summary>
public interface IPropertySearchService
{
    Task<PagedResponse<PropertySummaryResponse>> SearchAsync(SearchPropertiesRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<PropertySummaryResponse>> ListAsync(ListPropertiesRequest request, CancellationToken cancellationToken = default);

    Task<Result<PropertyDetailsResponse>> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FeaturedPropertiesResponse> GetFeaturedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyLocationResponse>> GetLocationsAsync(CancellationToken cancellationToken = default);
}
