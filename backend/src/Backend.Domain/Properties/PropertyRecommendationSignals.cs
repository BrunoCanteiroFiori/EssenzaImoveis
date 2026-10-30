namespace Backend.Domain.Properties;

/// <summary>
/// Sinais de um imóvel usados por <see cref="PropertyRecommendation"/>. Usa inicializadores porque o EF Core só
/// consegue ordenar por membros de uma projeção montada assim.
/// </summary>
public sealed class PropertyRecommendationSignals
{
    public required Property Property { get; init; }

    public required int RecentViews { get; init; }

    public required bool IsSponsored { get; init; }
}
