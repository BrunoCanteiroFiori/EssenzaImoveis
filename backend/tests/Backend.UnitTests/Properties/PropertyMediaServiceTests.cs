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

public sealed class PropertyMediaServiceTests
{
    private static readonly byte[] _pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] _mp4Bytes = [0x00, 0x00, 0x00, 0x18, .. "ftypmp42"u8.ToArray(), 0x00, 0x00, 0x00, 0x00];

    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly PropertyMediaService _propertyMediaService;

    public PropertyMediaServiceTests()
    {
        _propertyMediaService = new PropertyMediaService(_propertyRepository, _unitOfWork, _fileStorage, _timeProvider);
    }

    [Fact]
    public async Task AddAsync_WithValidPhoto_ShouldSaveFileAndAddToProperty()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        using var content = new MemoryStream(_pngBytes);

        var result = await _propertyMediaService.AddAsync(property.Id, [new PropertyMediaUpload("foto.png", content)]);

        Assert.True(result.IsSuccess);
        var media = Assert.Single(result.Value.Media);
        Assert.EndsWith(".png", media.FileName, StringComparison.Ordinal);
        Assert.Equal(PropertyMediaType.Photo, media.Type);
        await _fileStorage.Received(1).SaveAsync(
            PropertyMediaPaths.GetRelativePath(property.Id, media.FileName),
            Arg.Is<Stream>(stream => stream.Position == 0),
            "image/png",
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithVideo_ShouldPlaceVideoBeforePhotos()
    {
        var property = TestData.Property();
        property.AddMedia("foto.jpg", TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        using var content = new MemoryStream(_mp4Bytes);

        var result = await _propertyMediaService.AddAsync(property.Id, [new PropertyMediaUpload("tour.mp4", content)]);

        Assert.True(result.IsSuccess);
        Assert.Equal([PropertyMediaType.Video, PropertyMediaType.Photo], result.Value.Media.Select(media => media.Type));
        await _fileStorage.Received(1).SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), "video/mp4", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithPhotoAboveSizeLimit_ShouldReturnValidationError()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        var bytes = new byte[PropertyMediaService.MaxPhotoSizeBytes + 1];
        _pngBytes.CopyTo(bytes, 0);
        using var content = new MemoryStream(bytes);

        var result = await _propertyMediaService.AddAsync(property.Id, [new PropertyMediaUpload("grande.png", content)]);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _fileStorage.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithFileThatIsNotPhotoNorVideo_ShouldReturnValidationErrorAndNotSave()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        using var content = new MemoryStream("<script>alert(1)</script>"u8.ToArray());

        var result = await _propertyMediaService.AddAsync(property.Id, [new PropertyMediaUpload("foto.png", content)]);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _fileStorage.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithInvalidSecondFile_ShouldNotSaveAnyFile()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        using var valid = new MemoryStream(_pngBytes);
        using var invalid = new MemoryStream([0x00, 0x01, 0x02]);

        var result = await _propertyMediaService.AddAsync(
            property.Id,
            [new PropertyMediaUpload("a.png", valid), new PropertyMediaUpload("b.png", invalid)]);

        Assert.True(result.IsFailure);
        await _fileStorage.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WhenStorageFailsOnSecondFile_ShouldDeleteFirstSavedFileAndRethrow()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        _fileStorage.SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask, Task.FromException(new IOException("falha no armazenamento")));
        using var first = new MemoryStream(_pngBytes);
        using var second = new MemoryStream(_pngBytes);

        await Assert.ThrowsAsync<IOException>(() => _propertyMediaService.AddAsync(
            property.Id,
            [new PropertyMediaUpload("a.png", first), new PropertyMediaUpload("b.png", second)]));

        await _fileStorage.Received(1).DeleteAsync(
            PropertyMediaPaths.GetRelativePath(property.Id, property.Media[0]),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithoutFiles_ShouldReturnValidationError()
    {
        var result = await _propertyMediaService.AddAsync(Guid.CreateVersion7(), []);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task RemoveAsync_WithUnknownFile_ShouldReturnNotFound()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyMediaService.RemoveAsync(property.Id, "inexistente.jpg");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task RemoveAsync_WithExistingFile_ShouldDeleteFile()
    {
        var property = TestData.Property();
        property.AddMedia("foto.jpg", TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyMediaService.RemoveAsync(property.Id, "foto.jpg");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Media);
        await _fileStorage.Received(1).DeleteAsync(PropertyMediaPaths.GetRelativePath(property.Id, "foto.jpg"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderAsync_WithDifferentFiles_ShouldReturnValidationError()
    {
        var property = TestData.Property();
        property.AddMedia("foto.jpg", TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyMediaService.ReorderAsync(property.Id, new ReorderPropertyMediaRequest(["outra.jpg"]));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task OpenAsync_WithFileOfProperty_ShouldReturnStoredFile()
    {
        var property = TestData.Property();
        property.AddMedia("foto.jpg", TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        using var content = new MemoryStream(_pngBytes);
        var storedFile = new StoredFile(content, "image/jpeg");
        _fileStorage.OpenReadAsync(PropertyMediaPaths.GetRelativePath(property.Id, "foto.jpg"), Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _propertyMediaService.OpenAsync(property.Id, "foto.jpg");

        Assert.True(result.IsSuccess);
        Assert.Same(storedFile, result.Value);
    }

    [Fact]
    public async Task OpenAsync_WithFileOfAnotherProperty_ShouldReturnNotFoundWithoutReadingStorage()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _propertyMediaService.OpenAsync(property.Id, "foto.jpg");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _fileStorage.DidNotReceive().OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenAsync_WhenFileIsMissingInStorage_ShouldReturnNotFound()
    {
        var property = TestData.Property();
        property.AddMedia("foto.jpg", TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        _fileStorage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _propertyMediaService.OpenAsync(property.Id, "foto.jpg");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
