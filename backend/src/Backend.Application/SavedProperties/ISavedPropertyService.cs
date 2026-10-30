using Backend.Application.Common;
using Backend.Application.Properties.Dtos;

namespace Backend.Application.SavedProperties;

/// <summary>
/// Imóveis salvos por um visitante anônimo.
/// </summary>
public interface ISavedPropertyService
{
    /// <summary>
    /// Salvos do visitante, do último para o primeiro. Inclui os que saíram do site: o <c>Status</c> diferente de disponível
    /// indica isso.
    /// </summary>
    Task<IReadOnlyList<PropertySummaryResponse>> ListAsync(string visitorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Salva um imóvel disponível. Salvar de novo não muda nada.
    /// </summary>
    Task<Result> SaveAsync(Guid propertyId, string visitorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tira o imóvel dos salvos. Remover um imóvel que não está salvo não muda nada.
    /// </summary>
    Task RemoveAsync(Guid propertyId, string visitorId, CancellationToken cancellationToken = default);
}
