namespace Backend.Domain.Properties;

/// <summary>
/// Sugestões a partir dos imóveis vistos: a pontuação de um candidato é a soma da semelhança com cada imóvel visto, então
/// o que se repete no histórico do visitante pesa mais. Imóveis de finalidades diferentes não são comparados, porque o valor
/// de venda e o de aluguel não têm a mesma escala.
/// </summary>
public static class PropertySuggestion
{
    public const int SamePurposePoints = 1;
    public const int SameCityPoints = 1;
    public const int SameNeighborhoodPoints = 3;
    public const int SameTypePoints = 3;
    public const int SimilarPricePoints = 2;

    /// <summary>
    /// Diferença máxima de valor para dois imóveis serem considerados da mesma faixa.
    /// </summary>
    public const decimal PriceTolerance = 0.3m;

    public static int Score(Property candidate, Property viewed)
    {
        if (candidate.Purpose != viewed.Purpose)
        {
            return 0;
        }

        return SamePurposePoints
            + LocationPoints(candidate.Address, viewed.Address)
            + (candidate.Type == viewed.Type ? SameTypePoints : 0)
            + (IsSimilarPrice(candidate.Price, viewed.Price) ? SimilarPricePoints : 0);
    }

    /// <summary>
    /// Os candidatos mais parecidos com os imóveis vistos. No empate, vem o anúncio mais novo.
    /// </summary>
    public static IReadOnlyList<Property> Rank(IEnumerable<Property> candidates, IReadOnlyList<Property> viewed, int limit) =>
        [.. candidates
            .OrderByDescending(candidate => viewed.Sum(property => Score(candidate, property)))
            .ThenByDescending(candidate => candidate.CreatedAt)
            .Take(limit)];

    private static int LocationPoints(Address candidate, Address viewed)
    {
        if (!SameText(candidate.City, viewed.City) || candidate.State != viewed.State)
        {
            return 0;
        }

        return SameText(candidate.Neighborhood, viewed.Neighborhood) ? SameCityPoints + SameNeighborhoodPoints : SameCityPoints;
    }

    private static bool IsSimilarPrice(decimal candidate, decimal viewed) =>
        Math.Abs(candidate - viewed) <= viewed * PriceTolerance;

    private static bool SameText(string first, string second) =>
        string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
}
