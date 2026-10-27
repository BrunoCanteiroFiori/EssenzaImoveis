using Backend.Application.Common;
using Backend.Application.Properties.Dtos;

namespace Backend.Application.Properties;

public interface IPropertyImageService
{
    Task<Result<PropertyResponse>> AddAsync(Guid propertyId, IReadOnlyList<PropertyImageUpload> uploads, CancellationToken cancellationToken = default);

    Task<Result<PropertyResponse>> RemoveAsync(Guid propertyId, string fileName, CancellationToken cancellationToken = default);

    Task<Result<PropertyResponse>> ReorderAsync(Guid propertyId, ReorderPropertyImagesRequest request, CancellationToken cancellationToken = default);
}
