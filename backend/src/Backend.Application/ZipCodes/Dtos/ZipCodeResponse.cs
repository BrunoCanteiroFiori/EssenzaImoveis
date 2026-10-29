namespace Backend.Application.ZipCodes.Dtos;

public sealed record ZipCodeResponse(
    string ZipCode,
    string? Street,
    string? Neighborhood,
    string City,
    string State);
