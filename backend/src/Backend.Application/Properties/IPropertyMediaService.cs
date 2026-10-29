using Backend.Application.Common;
using Backend.Application.Properties.Dtos;
using Backend.Infrastructure.Storage;

namespace Backend.Application.Properties;

public interface IPropertyMediaService
{
    Task<Result<PropertyResponse>> AddAsync(Guid propertyId, IReadOnlyList<PropertyMediaUpload> uploads, CancellationToken cancellationToken = default);

    Task<Result<PropertyResponse>> RemoveAsync(Guid propertyId, string fileName, CancellationToken cancellationToken = default);

    Task<Result<PropertyResponse>> ReorderAsync(Guid propertyId, ReorderPropertyMediaRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre uma foto ou vídeo para leitura. Só abre arquivos que pertencem ao imóvel.
    /// </summary>
    Task<Result<StoredFile>> OpenAsync(Guid propertyId, string fileName, CancellationToken cancellationToken = default);
}
