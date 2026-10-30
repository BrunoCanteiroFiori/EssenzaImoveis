using Backend.Domain.Properties;
using Backend.UnitTests.Common;

namespace Backend.UnitTests.Properties;

public sealed class PropertySuggestionTests
{
    [Fact]
    public void Score_WhenPurposeDiffers_ShouldBeZero()
    {
        var sale = TestData.Property(purpose: PropertyPurpose.Sale);
        var rent = TestData.Property(purpose: PropertyPurpose.Rent);

        Assert.Equal(0, PropertySuggestion.Score(rent, sale));
    }

    [Fact]
    public void Score_WhenEverythingMatches_ShouldSumAllPoints()
    {
        var expected = PropertySuggestion.SamePurposePoints
            + PropertySuggestion.SameCityPoints
            + PropertySuggestion.SameNeighborhoodPoints
            + PropertySuggestion.SameTypePoints
            + PropertySuggestion.SimilarPricePoints;

        Assert.Equal(expected, PropertySuggestion.Score(TestData.Property(), TestData.Property()));
    }

    [Fact]
    public void Score_WhenOnlyCityMatches_ShouldNotCountNeighborhood()
    {
        var viewed = TestData.Property(neighborhood: "Centro");
        var candidate = TestData.Property(neighborhood: "Jardins");

        var difference = PropertySuggestion.Score(viewed, viewed) - PropertySuggestion.Score(candidate, viewed);

        Assert.Equal(PropertySuggestion.SameNeighborhoodPoints, difference);
    }

    [Fact]
    public void Score_WhenNeighborhoodHasSameNameInOtherCity_ShouldNotCountLocation()
    {
        var viewed = TestData.Property(neighborhood: "Centro", city: "São Paulo");
        var candidate = TestData.Property(neighborhood: "Centro", city: "Campinas");

        var difference = PropertySuggestion.Score(viewed, viewed) - PropertySuggestion.Score(candidate, viewed);

        Assert.Equal(PropertySuggestion.SameCityPoints + PropertySuggestion.SameNeighborhoodPoints, difference);
    }

    [Theory]
    [InlineData(650_000, true)]
    [InlineData(350_000, true)]
    [InlineData(650_001, false)]
    [InlineData(349_999, false)]
    public void Score_WithPriceAroundTolerance_ShouldCountOnlyInsideTheRange(int price, bool isSimilar)
    {
        var viewed = TestData.Property(price: 500_000m);
        var candidate = TestData.Property(price: price);

        var difference = PropertySuggestion.Score(viewed, viewed) - PropertySuggestion.Score(candidate, viewed);

        Assert.Equal(isSimilar ? 0 : PropertySuggestion.SimilarPricePoints, difference);
    }

    [Fact]
    public void Rank_WithViewedProperty_ShouldPutTheMostSimilarFirst()
    {
        var viewed = TestData.Property(type: PropertyType.Apartment, neighborhood: "Jardins");
        var similar = TestData.Property(type: PropertyType.Apartment, neighborhood: "Jardins", title: "Parecido");
        var different = TestData.Property(type: PropertyType.House, neighborhood: "Centro", title: "Diferente");

        var ranked = PropertySuggestion.Rank([different, similar], [viewed], limit: 2);

        Assert.Equal([similar, different], ranked);
    }

    [Fact]
    public void Rank_WithRepeatedInterest_ShouldFollowWhatTheVisitorViewedMost()
    {
        var apartment = TestData.Property(type: PropertyType.Apartment, title: "Apartamento");
        var house = TestData.Property(type: PropertyType.House, title: "Casa");
        var viewed = new[]
        {
            TestData.Property(type: PropertyType.House),
            TestData.Property(type: PropertyType.Apartment),
            TestData.Property(type: PropertyType.Apartment),
        };

        var ranked = PropertySuggestion.Rank([house, apartment], viewed, limit: 2);

        Assert.Equal([apartment, house], ranked);
    }

    [Fact]
    public void Rank_WhenScoresTie_ShouldPutTheNewestFirst()
    {
        var older = TestData.Property(createdAt: TestData.Now.AddDays(-1));
        var newer = TestData.Property(createdAt: TestData.Now);

        var ranked = PropertySuggestion.Rank([older, newer], [TestData.Property()], limit: 2);

        Assert.Equal([newer, older], ranked);
    }

    [Fact]
    public void Rank_WithLimit_ShouldReturnOnlyThatMany()
    {
        var candidates = new[] { TestData.Property(), TestData.Property(), TestData.Property() };

        var ranked = PropertySuggestion.Rank(candidates, [TestData.Property()], limit: 2);

        Assert.Equal(2, ranked.Count);
    }
}
