using Backend.Domain.Common;
using Backend.Domain.Properties;

namespace Backend.UnitTests.Properties;

public sealed class PropertyTemperatureTests
{
    [Theory]
    [InlineData(0, TemperatureLevel.Cold)]
    [InlineData(PropertyTemperature.WarmThreshold - 1, TemperatureLevel.Cold)]
    [InlineData(PropertyTemperature.WarmThreshold, TemperatureLevel.Warm)]
    [InlineData(PropertyTemperature.HotThreshold, TemperatureLevel.Hot)]
    [InlineData(PropertyTemperature.VeryHotThreshold, TemperatureLevel.VeryHot)]
    [InlineData(1_000, TemperatureLevel.VeryHot)]
    public void FromRecentViews_WithViews_ShouldReturnLevel(int recentViews, TemperatureLevel expected)
    {
        var temperature = PropertyTemperature.FromRecentViews(recentViews);

        Assert.Equal(expected, temperature.Level);
        Assert.Equal(recentViews, temperature.RecentViews);
    }

    [Fact]
    public void FromRecentViews_WithNegativeViews_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => PropertyTemperature.FromRecentViews(-1));
    }
}
