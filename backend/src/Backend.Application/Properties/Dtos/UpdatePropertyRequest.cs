using System.ComponentModel.DataAnnotations;
using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

public sealed record UpdatePropertyRequest(
    [Required, MaxLength(Property.TitleMaxLength)] string Title,
    [Required, MaxLength(Property.DescriptionMaxLength)] string Description,
    PropertyType Type,
    PropertyPurpose Purpose,
    [Range(0.01, 999_999_999)] decimal Price,
    [Range(0, 999_999_999)] decimal? CondominiumFee,
    [Range(0, 999_999_999)] decimal? PropertyTax,
    [Range(0.01, 99_999_999)] decimal Area,
    [Range(0, Property.MaxRooms)] int Bedrooms,
    [Range(0, Property.MaxRooms)] int Bathrooms,
    [Range(0, Property.MaxRooms)] int ParkingSpaces,
    [Required] AddressRequest Address);
