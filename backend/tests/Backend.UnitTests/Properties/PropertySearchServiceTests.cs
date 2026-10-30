using System.Linq.Expressions;
using System.Text.Json;
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
    public async Task SearchAsync_WithRequest_ShouldOnlySearchAvailableWithoutInternalData()
    {
        var property = TestData.Property();
        property.Sponsor(TestData.Now.AddDays(5), TestData.Now);
        _propertyRepository
            .SearchAsync(Arg.Any<PropertySearchFilter>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<Property>([property], 1, 1, 12));

        var page = await _propertySearchService.SearchAsync(new SearchPropertiesRequest(Location: "Centro"));

        AssertHasNoInternalData(Assert.Single(page.Items));
        await _propertyRepository.Received(1).SearchAsync(
            Arg.Is<PropertySearchFilter>(filter => filter.Status == PropertyStatus.Available && filter.Text == "Centro"),
            TestData.Now,
            TestData.Now - PropertyTemperature.Window,
            Arg.Any<CancellationToken>());
        await _propertyViewRepository.DidNotReceiveWithAnyArgs().CountByPropertySinceAsync(default!, default, default);
    }

    [Fact]
    public async Task SearchAsync_WhenSortIsHottest_ShouldUseRelevance()
    {
        _propertyRepository
            .SearchAsync(Arg.Any<PropertySearchFilter>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<Property>([], 0, 1, 12));

        await _propertySearchService.SearchAsync(new SearchPropertiesRequest(Sort: PropertySortOrder.Hottest));

        await _propertyRepository.Received(1).SearchAsync(
            Arg.Is<PropertySearchFilter>(filter => filter.Sort == PropertySortOrder.Relevance),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithRequest_ShouldMapTemperatureAndSponsorship()
    {
        var property = TestData.Property();
        property.Sponsor(TestData.Now.AddDays(5), TestData.Now);
        _propertyRepository
            .SearchAsync(Arg.Any<PropertySearchFilter>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<Property>([property], 1, 1, 20));
        _propertyViewRepository
            .CountByPropertySinceAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [property.Id] = PropertyTemperature.HotThreshold });

        var page = await _propertySearchService.ListAsync(new ListPropertiesRequest());

        var summary = Assert.Single(page.Items);
        Assert.Equal(TemperatureLevel.Hot, summary.Temperature.Level);
        Assert.True(summary.IsSponsored);
    }

    [Fact]
    public async Task ListAsync_WithSortField_ShouldForwardSortToRepository()
    {
        _propertyRepository
            .SearchAsync(Arg.Any<PropertySearchFilter>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<Property>([], 0, 1, 20));

        await _propertySearchService.ListAsync(new ListPropertiesRequest(SortField: PropertySortField.Title, SortDescending: true));

        await _propertyRepository.Received(1).SearchAsync(
            Arg.Is<PropertySearchFilter>(filter => filter.SortField == PropertySortField.Title && filter.SortDescending),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithoutSortField_ShouldSortByNewest()
    {
        _propertyRepository
            .SearchAsync(Arg.Any<PropertySearchFilter>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<Property>([], 0, 1, 20));

        await _propertySearchService.ListAsync(new ListPropertiesRequest());

        await _propertyRepository.Received(1).SearchAsync(
            Arg.Is<PropertySearchFilter>(filter => filter.SortField == null && filter.Sort == PropertySortOrder.Newest),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
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
    public async Task GetDetailsAsync_WhenPropertyIsAvailable_ShouldExposeOnlyNeighborhoodCityAndState()
    {
        var property = TestData.Property();
        property.AddMedia("foto.jpg", TestData.Now);
        property.Sponsor(TestData.Now.AddDays(5), TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertySearchService.GetDetailsAsync(property.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Centro", result.Value.Neighborhood);
        Assert.Equal("São Paulo", result.Value.City);
        Assert.Equal("SP", result.Value.State);
        Assert.Equal(PropertyMediaPaths.GetUrl(property.Id, "foto.jpg"), Assert.Single(result.Value.Media).Url);
        Assert.Equal(PropertyMediaPaths.GetUrl(property.Id, "foto.jpg"), result.Value.CoverImageUrl);
        AssertHasNoInternalData(result.Value);
    }

    [Fact]
    public async Task GetFeaturedAsync_WhenThereAreSponsored_ShouldHighlightOnlySponsored()
    {
        ArrangeSearch(filter => true);
        ArrangeSearch(filter => filter.Sort == PropertySortOrder.Relevance, TestData.Property(title: "Mais pontos"));
        ArrangeSearch(filter => filter.SponsoredOnly, TestData.Property(title: "Patrocinado"));

        var featured = await _propertySearchService.GetFeaturedAsync();

        Assert.Equal("Patrocinado", Assert.Single(featured.Highlights).Title);
    }

    [Fact]
    public async Task GetFeaturedAsync_WhenThereAreNoSponsored_ShouldHighlightTopScored()
    {
        ArrangeSearch(filter => true);
        ArrangeSearch(filter => filter.Sort == PropertySortOrder.Relevance && !filter.SponsoredOnly, TestData.Property(title: "Mais pontos"));

        var featured = await _propertySearchService.GetFeaturedAsync();

        Assert.Equal("Mais pontos", Assert.Single(featured.Highlights).Title);
    }

    [Fact]
    public async Task GetFeaturedAsync_ShouldBuildEachSectionWithItsOwnCriteria()
    {
        ArrangeSearch(filter => true);
        ArrangeSearch(filter => filter.Sort == PropertySortOrder.Relevance && filter.Purpose == PropertyPurpose.Sale, TestData.Property(title: "Compra"));
        ArrangeSearch(filter => filter.Sort == PropertySortOrder.Relevance && filter.Purpose == PropertyPurpose.Rent, TestData.Property(title: "Aluguel"));
        ArrangeSearch(filter => filter.Sort == PropertySortOrder.Newest, TestData.Property(title: "Novo"));
        _propertyRepository
            .GetHottestAsync(TestData.Now - PropertyTemperature.Window, PropertySearchService.FeaturedLimit, Arg.Any<CancellationToken>())
            .Returns([TestData.Property(title: "Buscado")]);

        var featured = await _propertySearchService.GetFeaturedAsync();

        Assert.Equal("Compra", Assert.Single(featured.ForSale).Title);
        Assert.Equal("Aluguel", Assert.Single(featured.ForRent).Title);
        Assert.Equal("Buscado", Assert.Single(featured.MostSearched).Title);
        Assert.Equal("Novo", Assert.Single(featured.Newest).Title);
        await _propertyRepository.DidNotReceive().SearchAsync(
            Arg.Is<PropertySearchFilter>(filter => filter.Status != PropertyStatus.Available || filter.PageSize != PropertySearchService.FeaturedLimit),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    private void ArrangeSearch(Expression<Predicate<PropertySearchFilter>> matches, params Property[] properties) =>
        _propertyRepository
            .SearchAsync(Arg.Is(matches), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<Property>(properties, properties.Length, 1, PropertySearchService.FeaturedLimit));

    private static void AssertHasNoInternalData<TResponse>(TResponse response)
    {
        var json = JsonSerializer.Serialize(response, JsonSerializerOptions.Web);

        Assert.DoesNotContain("temperature", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("isSponsored", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sponsoredUntil", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("street", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Rua das Flores", json, StringComparison.OrdinalIgnoreCase);
    }
}
