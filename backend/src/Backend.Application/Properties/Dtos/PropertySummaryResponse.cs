using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Resumo público do imóvel para cards e listagens. Não expõe temperatura, patrocínio nem o endereço completo.
/// </summary>
public sealed record PropertySummaryResponse(
    Guid Id,
    string Title,
    PropertyType Type,
    PropertyPurpose Purpose,
    PropertyStatus Status,
    decimal Price,
    decimal Area,
    int Bedrooms,
    int Bathrooms,
    int ParkingSpaces,
    string Neighborhood,
    string City,
    string State,
    string? CoverImageUrl,
    int MediaCount,
    DateTimeOffset CreatedAt);
