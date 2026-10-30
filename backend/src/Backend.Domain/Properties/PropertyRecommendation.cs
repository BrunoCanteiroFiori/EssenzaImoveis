using System.Linq.Expressions;

namespace Backend.Domain.Properties;

/// <summary>
/// Algoritmo de recomendação: a pontuação do imóvel é a sua temperatura (visitas recentes) mais um bônus se estiver
/// patrocinado. O bônus equivale a um imóvel em alta: o patrocínio pesa muito, mas um imóvel muito procurado ainda
/// supera um patrocinado sem visitas. Essas informações não são expostas na área pública.
/// </summary>
public static class PropertyRecommendation
{
    public const int SponsoredBonus = PropertyTemperature.VeryHotThreshold;

    /// <summary>
    /// Expressão, e não método, para que o EF Core traduza a pontuação em SQL e a paginação ocorra no banco.
    /// </summary>
    public static readonly Expression<Func<PropertyRecommendationSignals, int>> Score =
        signals => signals.RecentViews + (signals.IsSponsored ? SponsoredBonus : 0);

    /// <summary>
    /// Destaques da vitrine: se houver patrocinados, só eles; senão, os de maior pontuação.
    /// </summary>
    public static IReadOnlyList<Property> SelectHighlights(IReadOnlyList<Property> sponsored, IReadOnlyList<Property> topScored) =>
        sponsored.Count > 0 ? sponsored : topScored;
}
