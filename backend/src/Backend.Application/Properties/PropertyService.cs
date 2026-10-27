using Backend.Application.Common;
using Backend.Application.Properties.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Common;
using Backend.Domain.Properties;
using Backend.Infrastructure.Storage;

namespace Backend.Application.Properties;

public sealed class PropertyService(
    IPropertyRepository propertyRepository,
    IUnitOfWork unitOfWork,
    IFileStorage fileStorage,
    TimeProvider timeProvider) : IPropertyService
{
    public async Task<Result<PropertyResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(id, cancellationToken);
        return property is null ? PropertyErrors.NotFound(id) : property.ToResponse(timeProvider.GetUtcNow());
    }

    public async Task<Result<PropertyResponse>> CreateAsync(CreatePropertyRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        Property property;
        try
        {
            property = Property.Create(
                request.Title,
                request.Description,
                request.Type,
                request.Purpose,
                request.Price,
                request.CondominiumFee,
                request.PropertyTax,
                request.Area,
                request.Bedrooms,
                request.Bathrooms,
                request.ParkingSpaces,
                request.Address.ToAddress(),
                now);
        }
        catch (DomainException exception)
        {
            return PropertyErrors.Invalid(exception.Message);
        }

        await propertyRepository.AddAsync(property, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return property.ToResponse(now);
    }

    public Task<Result<PropertyResponse>> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken = default) =>
        ChangeAsync(
            id,
            (property, now) => property.Update(
                request.Title,
                request.Description,
                request.Type,
                request.Purpose,
                request.Price,
                request.CondominiumFee,
                request.PropertyTax,
                request.Area,
                request.Bedrooms,
                request.Bathrooms,
                request.ParkingSpaces,
                request.Address.ToAddress(),
                now),
            cancellationToken);

    public Task<Result<PropertyResponse>> ChangeStatusAsync(Guid id, ChangePropertyStatusRequest request, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, (property, now) => property.ChangeStatus(request.Status, now), cancellationToken);

    public Task<Result<PropertyResponse>> SponsorAsync(Guid id, SponsorPropertyRequest request, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, (property, now) => property.Sponsor(now.AddDays(request.Days), now), cancellationToken);

    public Task<Result<PropertyResponse>> RemoveSponsorshipAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, (property, now) => property.RemoveSponsorship(now), cancellationToken);

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property is null)
        {
            return PropertyErrors.NotFound(id);
        }

        propertyRepository.Remove(property);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteDirectoryAsync(PropertyImagePaths.GetDirectory(id), cancellationToken);
        return Result.Success();
    }

    private async Task<Result<PropertyResponse>> ChangeAsync(Guid id, Action<Property, DateTimeOffset> change, CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property is null)
        {
            return PropertyErrors.NotFound(id);
        }

        var now = timeProvider.GetUtcNow();
        try
        {
            change(property, now);
        }
        catch (DomainException exception)
        {
            return PropertyErrors.Invalid(exception.Message);
        }

        propertyRepository.Update(property);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return property.ToResponse(now);
    }
}
