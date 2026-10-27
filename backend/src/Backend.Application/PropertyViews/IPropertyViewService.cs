using Backend.Application.Common;
using Backend.Application.PropertyViews.Dtos;

namespace Backend.Application.PropertyViews;

public interface IPropertyViewService
{
    /// <summary>
    /// Registra o acesso de um visitante a um imóvel disponível (no máximo um por visitante por dia).
    /// </summary>
    Task<Result> RegisterAsync(Guid propertyId, RegisterPropertyViewRequest request, CancellationToken cancellationToken = default);
}
