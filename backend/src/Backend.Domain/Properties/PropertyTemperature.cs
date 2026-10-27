using Backend.Domain.Common;

namespace Backend.Domain.Properties;

/// <summary>
/// Temperatura do imóvel: quanto mais visitantes diferentes acessam o imóvel na janela recente, mais quente ele fica.
/// </summary>
public sealed class PropertyTemperature : ValueObject
{
    public const int WarmThreshold = 10;
    public const int HotThreshold = 30;
    public const int VeryHotThreshold = 100;

    public static readonly TimeSpan Window = TimeSpan.FromDays(30);

    private PropertyTemperature(int recentViews, TemperatureLevel level)
    {
        RecentViews = recentViews;
        Level = level;
    }

    public int RecentViews { get; }

    public TemperatureLevel Level { get; }

    public static PropertyTemperature FromRecentViews(int recentViews)
    {
        if (recentViews < 0)
        {
            throw new DomainException("A quantidade de visualizações não pode ser negativa.");
        }

        var level = recentViews switch
        {
            >= VeryHotThreshold => TemperatureLevel.VeryHot,
            >= HotThreshold => TemperatureLevel.Hot,
            >= WarmThreshold => TemperatureLevel.Warm,
            _ => TemperatureLevel.Cold,
        };

        return new PropertyTemperature(recentViews, level);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RecentViews;
        yield return Level;
    }
}
