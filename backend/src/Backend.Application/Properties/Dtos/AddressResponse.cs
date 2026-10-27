namespace Backend.Application.Properties.Dtos;

public sealed record AddressResponse(
    string Street,
    string? Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    string ZipCode);
