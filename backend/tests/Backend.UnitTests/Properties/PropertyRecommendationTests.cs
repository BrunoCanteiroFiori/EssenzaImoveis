using Backend.Domain.Properties;
using Backend.UnitTests.Common;

namespace Backend.UnitTests.Properties;

public sealed class PropertyRecommendationTests
{
    private static readonly Func<PropertyRecommendationSignals, int> _score = PropertyRecommendation.Score.Compile();

    [Fact]
    public void Score_WhenNotSponsored_ShouldBeTheRecentViews()
    {
        Assert.Equal(40, _score(Signals(recentViews: 40, isSponsored: false)));
    }

    [Fact]
    public void Score_WhenSponsored_ShouldAddBonusToRecentViews()
    {
        Assert.Equal(40 + PropertyRecommendation.SponsoredBonus, _score(Signals(recentViews: 40, isSponsored: true)));
    }

    [Fact]
    public void Score_WhenSponsoredWithoutViews_ShouldBeatHotProperty()
    {
        var sponsored = Signals(recentViews: 0, isSponsored: true);
        var hot = Signals(recentViews: PropertyTemperature.HotThreshold, isSponsored: false);

        Assert.True(_score(sponsored) > _score(hot));
    }

    [Fact]
    public void Score_WhenViewsExceedBonus_ShouldBeatSponsoredWithoutViews()
    {
        var sponsored = Signals(recentViews: 0, isSponsored: true);
        var veryHot = Signals(recentViews: PropertyRecommendation.SponsoredBonus + 1, isSponsored: false);

        Assert.True(_score(veryHot) > _score(sponsored));
    }

    [Fact]
    public void SelectHighlights_WhenThereAreSponsored_ShouldReturnOnlySponsored()
    {
        var sponsored = TestData.Property(title: "Patrocinado");

        var highlights = PropertyRecommendation.SelectHighlights([sponsored], [TestData.Property(title: "Mais pontos")]);

        Assert.Same(sponsored, Assert.Single(highlights));
    }

    [Fact]
    public void SelectHighlights_WhenThereAreNoSponsored_ShouldReturnTopScored()
    {
        var topScored = TestData.Property(title: "Mais pontos");

        var highlights = PropertyRecommendation.SelectHighlights([], [topScored]);

        Assert.Same(topScored, Assert.Single(highlights));
    }

    private static PropertyRecommendationSignals Signals(int recentViews, bool isSponsored) =>
        new() { Property = TestData.Property(), RecentViews = recentViews, IsSponsored = isSponsored };
}
