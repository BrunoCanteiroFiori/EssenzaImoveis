using Backend.Application.Common;
using Backend.Application.Properties;
using Backend.Application.Properties.Dtos;
using Backend.Domain.Common;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.Properties;

public sealed class PropertySearchServiceTests
{
    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly IPropertyViewRepository _propertyViewRepository = Substitute.For<IPropertyViewRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly PropertySearchService _propertySearchService;

    public PropertySearchServiceTests()
    {
        _propertySearchService = new PropertySearchService(_propertyRepository, _propertyViewRepository, _timeProvider);
    }

    [Fact]
    public async Task SearchAsync_WithRequest_ShouldOnlySearchAvailableAndMapTemperature()
    {
        var property = TestData.Property();
        _propertyRepository
            .SearchAsync(Arg.Any<PropertySearchFilter>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<Property>([property], 1, 1, 12));
        _propertyViewRepository
            .CountByPropertySinceAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [property.Id] = PropertyTemperature.HotThreshold });

        var page = await _propertySearchService.SearchAsync(new SearchPropertiesRequest(Location: "Centro"));

        var summary = Assert.Single(page.Items);
        Assert.Equal(TemperatureLevel.Hot, summary.Temperature.Level);
        await _propertyRepository.Received(1).SearchAsync(
            Arg.Is<PropertySearchFilter>(filter => filter.Status == PropertyStatus.Available && filter.Text == "Centro"),
            TestData.Now,
            TestData.Now - PropertyTemperature.Window,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDetailsAsync_WhenPropertyIsNotAvailable_ShouldReturnNotFound()
    {
        var property = TestData.Property();
        property.ChangeStatus(PropertyStatus.Inactive, TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertySearchService.GetDetailsAsync(property.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenPropertyIsAvailable_ShouldHideFullAddress()
    {
        var property = TestData.Property();
        property.AddMedia("foto.jpg", TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        _propertyViewRepository
            .CountByPropertySinceAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        var result = await _propertySearchService.GetDetailsAsync(property.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Rua das Flores", result.Value.Street);
        Assert.Equal(PropertyMediaPaths.GetUrl(property.Id, "foto.jpg"), Assert.Single(result.Value.Media).Url);
        Assert.Equal(PropertyMediaPaths.GetUrl(property.Id, "foto.jpg"), result.Value.CoverImageUrl);
        Assert.Equal(TemperatureLevel.Cold, result.Value.Temperature.Level);
    }

    [Fact]
    public async Task GetFeaturedAsync_ShouldReturnSponsoredHottestAndNewest()
    {
        var sponsored = TestData.Property(title: "Patrocinado");
        sponsored.Sponsor(TestData.Now.AddDays(5), TestData.Now);
        var hottest = TestData.Property(title: "Quente");
        var newest = TestData.Property(title: "Novo");
        _propertyRepository.GetSponsoredAsync(TestData.Now, PropertySearchService.FeaturedLimit, Arg.Any<CancellationToken>()).Returns([sponsored]);
        _propertyRepository.GetHottestAsync(Arg.Any<DateTimeOffset>(), PropertySearchService.FeaturedLimit, Arg.Any<CancellationToken>()).Returns([hottest]);
        _propertyRepository
            .SearchAsync(Arg.Any<PropertySearchFilter>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<Property>([newest], 1, 1, PropertySearchService.FeaturedLimit));
        _propertyViewRepository
            .CountByPropertySinceAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [hottest.Id] = 50 });

        var featured = await _propertySearchService.GetFeaturedAsync();

        Assert.True(Assert.Single(featured.Sponsored).IsSponsored);
        Assert.Equal(50, Assert.Single(featured.Hottest).Temperature.RecentViews);
        Assert.Equal("Novo", Assert.Single(featured.Newest).Title);
    }
}
