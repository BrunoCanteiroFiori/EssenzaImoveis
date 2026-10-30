using Backend.Application.Common;
using Backend.Application.SavedProperties;
using Backend.Domain.Properties;
using Backend.Domain.SavedProperties;
using Backend.Infrastructure.Conversions;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.SavedProperties;

public sealed class SavedPropertyServiceTests
{
    private const string VisitorId = "visitante-123";

    private readonly ISavedPropertyRepository _savedPropertyRepository = Substitute.For<ISavedPropertyRepository>();
    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly SavedPropertyService _savedPropertyService;

    public SavedPropertyServiceTests()
    {
        _savedPropertyService = new SavedPropertyService(_savedPropertyRepository, _propertyRepository, _timeProvider);
    }

    [Fact]
    public async Task ListAsync_WhenPropertyLeftTheSite_ShouldKeepItWithItsStatus()
    {
        var available = TestData.Property();
        var sold = TestData.Property(title: "Casa vendida");
        sold.ChangeStatus(PropertyStatus.Sold, TestData.Now);
        _propertyRepository
            .GetSavedAsync(VisitorId.ToSha256Hex(), SavedPropertyService.ListLimit, Arg.Any<CancellationToken>())
            .Returns([sold, available]);

        var properties = await _savedPropertyService.ListAsync(VisitorId);

        Assert.Equal([sold.Id, available.Id], properties.Select(property => property.Id));
        Assert.Equal(PropertyStatus.Sold, properties[0].Status);
        Assert.Equal(PropertyStatus.Available, properties[1].Status);
    }

    [Fact]
    public async Task SaveAsync_WithAvailableProperty_ShouldAddHashedVisitor()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _savedPropertyService.SaveAsync(property.Id, VisitorId);

        Assert.True(result.IsSuccess);
        await _savedPropertyRepository.Received(1).AddIfNotExistsAsync(
            Arg.Is<SavedProperty>(savedProperty =>
                savedProperty.PropertyId == property.Id
                && savedProperty.VisitorHash == VisitorId.ToSha256Hex()
                && savedProperty.SavedAt == TestData.Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WhenPropertyIsNotAvailable_ShouldReturnNotFound()
    {
        var property = TestData.Property();
        property.ChangeStatus(PropertyStatus.Inactive, TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _savedPropertyService.SaveAsync(property.Id, VisitorId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _savedPropertyRepository.DidNotReceive().AddIfNotExistsAsync(Arg.Any<SavedProperty>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WhenPropertyDoesNotExist_ShouldReturnNotFound()
    {
        var result = await _savedPropertyService.SaveAsync(Guid.CreateVersion7(), VisitorId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task RemoveAsync_WithVisitor_ShouldRemoveByHash()
    {
        var propertyId = Guid.CreateVersion7();

        await _savedPropertyService.RemoveAsync(propertyId, VisitorId);

        await _savedPropertyRepository.Received(1).RemoveAsync(VisitorId.ToSha256Hex(), propertyId, Arg.Any<CancellationToken>());
    }
}
