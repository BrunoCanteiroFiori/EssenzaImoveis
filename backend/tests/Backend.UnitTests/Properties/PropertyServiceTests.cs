using Backend.Application.Common;
using Backend.Application.Properties;
using Backend.Application.Properties.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Properties;
using Backend.Infrastructure.Storage;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.Properties;

public sealed class PropertyServiceTests
{
    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly PropertyService _propertyService;

    public PropertyServiceTests()
    {
        _propertyService = new PropertyService(_propertyRepository, _unitOfWork, _fileStorage, _timeProvider);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPropertyDoesNotExist_ShouldReturnNotFound()
    {
        var id = Guid.CreateVersion7();
        _propertyRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Property?)null);

        var result = await _propertyService.GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldPersistProperty()
    {
        var result = await _propertyService.CreateAsync(TestData.CreatePropertyRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(PropertyStatus.Available, result.Value.Status);
        Assert.Equal("SP", result.Value.Address.State);
        await _propertyRepository.Received(1).AddAsync(Arg.Any<Property>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ShouldReturnValidationError()
    {
        var result = await _propertyService.CreateAsync(TestData.CreatePropertyRequest(title: string.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenPropertyDoesNotExist_ShouldReturnNotFound()
    {
        var id = Guid.CreateVersion7();
        _propertyRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Property?)null);
        var request = new UpdatePropertyRequest(
            "Casa", "Descrição", PropertyType.House, PropertyPurpose.Sale, 1m, null, null, 1m, 1, 1, 1, TestData.AddressRequest());

        var result = await _propertyService.UpdateAsync(id, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithStatusIncompatibleWithPurpose_ShouldReturnValidationError()
    {
        var property = TestData.Property(PropertyPurpose.Rent);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyService.ChangeStatusAsync(property.Id, new ChangePropertyStatusRequest(PropertyStatus.Sold));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SponsorAsync_WithDays_ShouldSponsorUntilNowPlusDays()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyService.SponsorAsync(property.Id, new SponsorPropertyRequest(10));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsSponsored);
        Assert.Equal(TestData.Now.AddDays(10), result.Value.SponsoredUntil);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenPropertyExists_ShouldRemovePropertyAndMediaFolder()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyService.DeleteAsync(property.Id);

        Assert.True(result.IsSuccess);
        _propertyRepository.Received(1).Remove(property);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _fileStorage.Received(1).DeleteDirectoryAsync(PropertyMediaPaths.GetDirectory(property.Id), Arg.Any<CancellationToken>());
    }
}
