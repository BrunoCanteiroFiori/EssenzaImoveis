using Backend.Application.Common;
using Backend.Application.Properties;
using Backend.Application.PropertyViews.Dtos;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;
using Backend.Infrastructure.Conversions;

namespace Backend.Application.PropertyViews;

public sealed class PropertyViewService(
    IPropertyViewRepository propertyViewRepository,
    IPropertyRepository propertyRepository,
    TimeProvider timeProvider) : IPropertyViewService
{
    public async Task<Result> RegisterAsync(Guid propertyId, RegisterPropertyViewRequest request, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null || !property.IsPubliclyVisible)
        {
            return PropertyErrors.NotFound(propertyId);
        }

        var view = PropertyView.Create(propertyId, request.VisitorId.Trim().ToSha256Hex(), timeProvider.GetUtcNow());
        await propertyViewRepository.AddIfNotExistsAsync(view, cancellationToken);
        return Result.Success();
    }
}
