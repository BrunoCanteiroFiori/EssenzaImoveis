namespace Backend.Domain.Properties;

/// <summary>
/// Filtros da busca de imóveis. Campos nulos não filtram. <see cref="Text"/> procura em título, bairro, cidade e UF.
/// <see cref="SortField"/>, quando informado, tem prioridade sobre <see cref="Sort"/>.
/// <see cref="SponsoredOnly"/> restringe aos patrocinados ativos.
/// </summary>
public sealed record PropertySearchFilter(
    string? Text,
    PropertyType? Type,
    PropertyPurpose? Purpose,
    PropertyStatus? Status,
    int? MinBedrooms,
    int? MinBathrooms,
    int? MinParkingSpaces,
    decimal? MinArea,
    decimal? MaxArea,
    decimal? MinPrice,
    decimal? MaxPrice,
    PropertySortOrder Sort,
    int Page,
    int PageSize,
    PropertySortField? SortField = null,
    bool SortDescending = false,
    bool SponsoredOnly = false);
