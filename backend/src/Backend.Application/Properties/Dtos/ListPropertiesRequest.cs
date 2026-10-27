using System.ComponentModel.DataAnnotations;
using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Listagem da área administrativa: <see cref="Search"/> procura em título, bairro, cidade e UF; inclui todos os status.
/// </summary>
public sealed record ListPropertiesRequest(
    [MaxLength(100)] string? Search = null,
    PropertyStatus? Status = null,
    PropertyType? Type = null,
    PropertyPurpose? Purpose = null,
    PropertySortOrder Sort = PropertySortOrder.Newest,
    [Range(1, 10_000)] int Page = 1,
    [Range(1, 100)] int PageSize = 20);
