using Backend.Application.Common;
using Backend.Application.Properties;
using Backend.Application.Properties.Dtos;
using Backend.Domain.Properties;
using Backend.Domain.SavedProperties;

namespace Backend.Application.SavedProperties;

public sealed class SavedPropertyService(
    ISavedPropertyRepository savedPropertyRepository,
    IPropertyRepository propertyRepository,
    TimeProvider timeProvider) : ISavedPropertyService
{
    public const int ListLimit = 50;

    public async Task<IReadOnlyList<PropertySummaryResponse>> ListAsync(string visitorId, CancellationToken cancellationToken = default)
    {
        var properties = await propertyRepository.GetSavedAsync(visitorId.ToVisitorHash(), ListLimit, cancellationToken);
        return [.. properties.Select(property => property.ToSummaryResponse())];
    }

    public async Task<Result> SaveAsync(Guid propertyId, string visitorId, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null || !property.IsPubliclyVisible)
        {
            return PropertyErrors.NotFound(propertyId);
        }

        var savedProperty = SavedProperty.Create(propertyId, visitorId.ToVisitorHash(), timeProvider.GetUtcNow());
        await savedPropertyRepository.AddIfNotExistsAsync(savedProperty, cancellationToken);
        return Result.Success();
    }

    public Task RemoveAsync(Guid propertyId, string visitorId, CancellationToken cancellationToken = default) =>
        savedPropertyRepository.RemoveAsync(visitorId.ToVisitorHash(), propertyId, cancellationToken);
}
