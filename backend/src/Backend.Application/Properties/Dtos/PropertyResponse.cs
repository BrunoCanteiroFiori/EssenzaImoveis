using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Imóvel completo, para a área administrativa.
/// </summary>
public sealed record PropertyResponse(
    Guid Id,
    string Title,
    string Description,
    PropertyType Type,
    PropertyPurpose Purpose,
    PropertyStatus Status,
    decimal Price,
    decimal? CondominiumFee,
    decimal? PropertyTax,
    decimal Area,
    int Bedrooms,
    int Bathrooms,
    int ParkingSpaces,
    AddressResponse Address,
    IReadOnlyList<PropertyImageResponse> Images,
    DateTimeOffset? SponsoredUntil,
    bool IsSponsored,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
