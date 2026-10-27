using Backend.Application.Common;
using Backend.Application.PropertyViews;
using Backend.Application.PropertyViews.Dtos;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;
using Backend.Infrastructure.Conversions;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.PropertyViews;

public sealed class PropertyViewServiceTests
{
    private const string VisitorId = "visitante-123";

    private readonly IPropertyViewRepository _propertyViewRepository = Substitute.For<IPropertyViewRepository>();
    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly PropertyViewService _propertyViewService;

    public PropertyViewServiceTests()
    {
        _propertyViewService = new PropertyViewService(_propertyViewRepository, _propertyRepository, _timeProvider);
    }

    [Fact]
    public async Task RegisterAsync_WithVisitor_ShouldAddHashedViewForToday()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyViewService.RegisterAsync(property.Id, new RegisterPropertyViewRequest(VisitorId));

        Assert.True(result.IsSuccess);
        await _propertyViewRepository.Received(1).AddIfNotExistsAsync(
            Arg.Is<PropertyView>(view =>
                view.PropertyId == property.Id
                && view.VisitorHash == VisitorId.ToSha256Hex()
                && view.ViewedOn == DateOnly.FromDateTime(TestData.Now.UtcDateTime)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenVisitorAlreadyViewedToday_ShouldStillSucceed()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        _propertyViewRepository.AddIfNotExistsAsync(Arg.Any<PropertyView>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _propertyViewService.RegisterAsync(property.Id, new RegisterPropertyViewRequest(VisitorId));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_WhenPropertyIsNotAvailable_ShouldReturnNotFound()
    {
        var property = TestData.Property();
        property.ChangeStatus(PropertyStatus.Sold, TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyViewService.RegisterAsync(property.Id, new RegisterPropertyViewRequest(VisitorId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _propertyViewRepository.DidNotReceive().AddIfNotExistsAsync(Arg.Any<PropertyView>(), Arg.Any<CancellationToken>());
    }
}
