using Backend.Domain.Abstractions;

namespace Backend.Domain.PropertyViews;

public interface IPropertyViewRepository : IRepository<PropertyView>
{
    /// <summary>
    /// Grava a visualização de forma atômica, ignorando se o mesmo visitante já visualizou o imóvel no mesmo dia
    /// (inclusive em requisições simultâneas). Retorna verdadeiro se gravou.
    /// </summary>
    Task<bool> AddIfNotExistsAsync(PropertyView view, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quantidade de visualizações por imóvel desde <paramref name="since"/>. Imóveis sem visualização não aparecem.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> CountByPropertySinceAsync(
        IReadOnlyCollection<Guid> propertyIds,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    Task<int> CountSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default);
}
