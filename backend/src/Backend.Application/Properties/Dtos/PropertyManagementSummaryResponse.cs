using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Resumo do imóvel para a área administrativa, com temperatura e patrocínio.
/// </summary>
public sealed record PropertyManagementSummaryResponse(
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
    bool IsSponsored,
    TemperatureResponse Temperature,
    DateTimeOffset CreatedAt);
