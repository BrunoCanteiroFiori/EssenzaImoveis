using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Detalhe público do imóvel. Não expõe número, complemento nem CEP do endereço.
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
    string Street,
    string Neighborhood,
    string City,
    string State,
    IReadOnlyList<string> ImageUrls,
    bool IsSponsored,
    TemperatureResponse Temperature,
    DateTimeOffset CreatedAt);
