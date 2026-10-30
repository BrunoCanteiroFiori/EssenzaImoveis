using Backend.Domain.Abstractions;

namespace Backend.Domain.SavedProperties;

public interface ISavedPropertyRepository : IRepository<SavedProperty>
{
    /// <summary>
    /// Grava de forma atômica, ignorando se o visitante já salvou o imóvel (inclusive em requisições simultâneas).
    /// </summary>
    Task AddIfNotExistsAsync(SavedProperty savedProperty, CancellationToken cancellationToken = default);

    Task RemoveAsync(string visitorHash, Guid propertyId, CancellationToken cancellationToken = default);
}
