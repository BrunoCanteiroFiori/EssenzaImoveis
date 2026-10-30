using Backend.Domain.Abstractions;
using Backend.Domain.Common;

namespace Backend.Domain.Properties;

public interface IPropertyRepository : IRepository<Property>
{
    /// <summary>
    /// Busca paginada. A ordenação por relevância segue <see cref="PropertyRecommendation"/>, com os patrocinados ativos
    /// em <paramref name="now"/>; ela e a ordenação por temperatura consideram as visualizações desde <paramref name="viewsSince"/>.
    /// </summary>
    Task<PagedList<Property>> SearchAsync(
        PropertySearchFilter filter,
        DateTimeOffset now,
        DateTimeOffset viewsSince,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Property>> GetHottestAsync(DateTimeOffset viewsSince, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imóveis disponíveis vistos pelo visitante, do mais recente para o mais antigo.
    /// </summary>
    Task<IReadOnlyList<Property>> GetRecentlyViewedAsync(string visitorHash, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imóveis salvos pelo visitante em qualquer status, do último salvo para o primeiro.
    /// </summary>
    Task<IReadOnlyList<Property>> GetSavedAsync(string visitorHash, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imóveis disponíveis que podem ser sugeridos (ver <see cref="PropertySuggestion"/>), dos mais novos para os mais antigos.
    /// </summary>
    Task<IReadOnlyList<Property>> GetSuggestionCandidatesAsync(
        IReadOnlyCollection<PropertyPurpose> purposes,
        IReadOnlyCollection<Guid> excludedIds,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyLocation>> GetAvailableLocationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<PropertyStatus, int>> CountByStatusAsync(CancellationToken cancellationToken = default);

    Task<int> CountSponsoredAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}
