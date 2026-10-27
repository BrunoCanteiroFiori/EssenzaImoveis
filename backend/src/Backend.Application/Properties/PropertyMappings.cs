using Backend.Application.Properties.Dtos;
using Backend.Domain.Properties;

namespace Backend.Application.Properties;

public static class PropertyMappings
{
    public static PropertyResponse ToResponse(this Property property, DateTimeOffset now) => new(
        property.Id,
        property.Title,
        property.Description,
        property.Type,
        property.Purpose,
        property.Status,
        property.Price,
        property.CondominiumFee,
        property.PropertyTax,
        property.Area,
        property.Bedrooms,
        property.Bathrooms,
        property.ParkingSpaces,
        property.Address.ToResponse(),
        [.. property.Images.Select(fileName => new PropertyImageResponse(fileName, PropertyImagePaths.GetUrl(property.Id, fileName)))],
        property.SponsoredUntil,
        property.IsSponsored(now),
        property.CreatedAt,
        property.UpdatedAt);

    public static PropertySummaryResponse ToSummaryResponse(this Property property, DateTimeOffset now, int recentViews) => new(
        property.Id,
        property.Title,
        property.Type,
        property.Purpose,
        property.Status,
        property.Price,
        property.Area,
        property.Bedrooms,
        property.Bathrooms,
        property.ParkingSpaces,
        property.Address.Neighborhood,
        property.Address.City,
        property.Address.State,
        property.CoverImage is null ? null : PropertyImagePaths.GetUrl(property.Id, property.CoverImage),
        property.Images.Count,
        property.IsSponsored(now),
        ToTemperatureResponse(recentViews),
        property.CreatedAt);

    public static PropertyDetailsResponse ToDetailsResponse(this Property property, DateTimeOffset now, int recentViews) => new(
        property.Id,
        property.Title,
        property.Description,
        property.Type,
        property.Purpose,
        property.Price,
        property.CondominiumFee,
        property.PropertyTax,
        property.Area,
        property.Bedrooms,
        property.Bathrooms,
        property.ParkingSpaces,
        property.Address.Street,
        property.Address.Neighborhood,
        property.Address.City,
        property.Address.State,
        [.. property.Images.Select(fileName => PropertyImagePaths.GetUrl(property.Id, fileName))],
        property.IsSponsored(now),
        ToTemperatureResponse(recentViews),
        property.CreatedAt);

    public static AddressResponse ToResponse(this Address address) => new(
        address.Street,
        address.Number,
        address.Complement,
        address.Neighborhood,
        address.City,
        address.State,
        address.ZipCode);

    public static PropertyLocationResponse ToResponse(this PropertyLocation location) =>
        new(location.Neighborhood, location.City, location.State);

    /// <summary>
    /// Cria o value object de endereço. Lança <see cref="Backend.Domain.Common.DomainException"/> se for inválido.
    /// </summary>
    public static Address ToAddress(this AddressRequest request) => Address.Create(
        request.Street,
        request.Number,
        request.Complement,
        request.Neighborhood,
        request.City,
        request.State,
        request.ZipCode);

    public static PropertySearchFilter ToFilter(this SearchPropertiesRequest request) => new(
        request.Location,
        request.Type,
        request.Purpose,
        PropertyStatus.Available,
        request.MinBedrooms,
        request.MinBathrooms,
        request.MinParkingSpaces,
        request.MinArea,
        request.MaxArea,
        request.MinPrice,
        request.MaxPrice,
        request.Sort,
        request.Page,
        request.PageSize);

    public static PropertySearchFilter ToFilter(this ListPropertiesRequest request) => new(
        request.Search,
        request.Type,
        request.Purpose,
        request.Status,
        MinBedrooms: null,
        MinBathrooms: null,
        MinParkingSpaces: null,
        MinArea: null,
        MaxArea: null,
        MinPrice: null,
        MaxPrice: null,
        request.Sort,
        request.Page,
        request.PageSize);

    private static TemperatureResponse ToTemperatureResponse(int recentViews)
    {
        var temperature = PropertyTemperature.FromRecentViews(recentViews);
        return new TemperatureResponse(temperature.RecentViews, temperature.Level);
    }
}
