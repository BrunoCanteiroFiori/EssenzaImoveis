using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Detalhe público do imóvel. Do endereço, expõe só bairro, cidade e UF; não expõe temperatura nem patrocínio.
/// </summary>
public sealed record PropertyDetailsResponse(
    Guid Id,
    string Title,
    string Description,
    PropertyType Type,
    PropertyPurpose Purpose,
    decimal Price,
    decimal? CondominiumFee,
    decimal? PropertyTax,
    decimal Area,
    int Bedrooms,
    int Bathrooms,
    int ParkingSpaces,
    string Neighborhood,
    string City,
    string State,
    string? CoverImageUrl,
    IReadOnlyList<PropertyMediaResponse> Media,
    DateTimeOffset CreatedAt);
