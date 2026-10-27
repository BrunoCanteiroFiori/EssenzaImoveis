using System.ComponentModel.DataAnnotations;
using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Busca pública: <see cref="Location"/> procura em bairro, cidade e UF. Só retorna imóveis disponíveis.
/// </summary>
public sealed record SearchPropertiesRequest(
    [MaxLength(100)] string? Location = null,
    PropertyType? Type = null,
    PropertyPurpose? Purpose = null,
    [Range(0, Property.MaxRooms)] int? MinBedrooms = null,
    [Range(0, Property.MaxRooms)] int? MinBathrooms = null,
    [Range(0, Property.MaxRooms)] int? MinParkingSpaces = null,
    [Range(0, 99_999_999)] decimal? MinArea = null,
    [Range(0, 99_999_999)] decimal? MaxArea = null,
    [Range(0, 999_999_999)] decimal? MinPrice = null,
    [Range(0, 999_999_999)] decimal? MaxPrice = null,
    PropertySortOrder Sort = PropertySortOrder.Relevance,
    [Range(1, 10_000)] int Page = 1,
    [Range(1, 50)] int PageSize = 12);
