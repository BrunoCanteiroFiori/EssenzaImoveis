using System.ComponentModel.DataAnnotations;
using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

public sealed record AddressRequest(
    [Required, MaxLength(Address.StreetMaxLength)] string Street,
    [MaxLength(Address.NumberMaxLength)] string? Number,
    [MaxLength(Address.ComplementMaxLength)] string? Complement,
    [Required, MaxLength(Address.NeighborhoodMaxLength)] string Neighborhood,
    [Required, MaxLength(Address.CityMaxLength)] string City,
    [Required, StringLength(Address.StateLength, MinimumLength = Address.StateLength)] string State,
    [Required, MaxLength(9)] string ZipCode);
