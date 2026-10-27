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

public sealed class PropertyImageServiceTests
{
    private static readonly byte[] _pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly PropertyImageService _propertyImageService;

    public PropertyImageServiceTests()
    {
        _propertyImageService = new PropertyImageService(_propertyRepository, _unitOfWork, _fileStorage, _timeProvider);
    }

    [Fact]
    public async Task AddAsync_WithValidImage_ShouldSaveFileAndAddToProperty()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        using var content = new MemoryStream(_pngBytes);

        var result = await _propertyImageService.AddAsync(property.Id, [new PropertyImageUpload("foto.png", content)]);

        Assert.True(result.IsSuccess);
        var image = Assert.Single(result.Value.Images);
        Assert.EndsWith(".png", image.FileName, StringComparison.Ordinal);
        await _fileStorage.Received(1).SaveAsync(
            PropertyImagePaths.GetRelativePath(property.Id, image.FileName),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithFileThatIsNotImage_ShouldReturnValidationErrorAndNotSave()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        using var content = new MemoryStream("<script>alert(1)</script>"u8.ToArray());

        var result = await _propertyImageService.AddAsync(property.Id, [new PropertyImageUpload("foto.png", content)]);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _fileStorage.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithInvalidSecondFile_ShouldDeleteFirstSavedFile()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        using var valid = new MemoryStream(_pngBytes);
        using var invalid = new MemoryStream([0x00, 0x01, 0x02]);

        var result = await _propertyImageService.AddAsync(
            property.Id,
            [new PropertyImageUpload("a.png", valid), new PropertyImageUpload("b.png", invalid)]);

        Assert.True(result.IsFailure);
        await _fileStorage.Received(1).DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithoutFiles_ShouldReturnValidationError()
    {
        var result = await _propertyImageService.AddAsync(Guid.CreateVersion7(), []);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task RemoveAsync_WithUnknownImage_ShouldReturnNotFound()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyImageService.RemoveAsync(property.Id, "inexistente.jpg");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task RemoveAsync_WithExistingImage_ShouldDeleteFile()
    {
        var property = TestData.Property();
        property.AddImage("foto.jpg", TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyImageService.RemoveAsync(property.Id, "foto.jpg");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Images);
        await _fileStorage.Received(1).DeleteAsync(PropertyImagePaths.GetRelativePath(property.Id, "foto.jpg"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderAsync_WithDifferentImages_ShouldReturnValidationError()
    {
        var property = TestData.Property();
        property.AddImage("foto.jpg", TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyImageService.ReorderAsync(property.Id, new ReorderPropertyImagesRequest(["outra.jpg"]));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
