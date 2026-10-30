using Backend.Application.Common;
using Backend.Application.Properties;
using Backend.Application.PropertyViews.Dtos;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;

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

        var view = PropertyView.Create(propertyId, request.VisitorId.ToVisitorHash(), timeProvider.GetUtcNow());
        await propertyViewRepository.AddOrRefreshAsync(view, cancellationToken);
        return Result.Success();
    }
}
