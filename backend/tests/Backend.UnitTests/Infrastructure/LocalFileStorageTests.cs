using Backend.Infrastructure.Storage;

namespace Backend.UnitTests.Infrastructure;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"storage-tests-{Guid.CreateVersion7():N}");
    private readonly LocalFileStorage _fileStorage;

    public LocalFileStorageTests()
    {
        _fileStorage = new LocalFileStorage(_rootPath);
    }

    [Fact]
    public async Task SaveAsync_ThenDeleteDirectoryAsync_ShouldCreateAndRemoveFiles()
    {
        using var content = new MemoryStream([1, 2, 3]);

        await _fileStorage.SaveAsync("properties/1/foto.jpg", content, "image/jpeg");
        var saved = File.Exists(Path.Combine(_rootPath, "properties", "1", "foto.jpg"));
        await _fileStorage.DeleteDirectoryAsync("properties/1");

        Assert.True(saved);
        Assert.False(Directory.Exists(Path.Combine(_rootPath, "properties", "1")));
    }

    [Fact]
    public async Task OpenReadAsync_WithSavedFile_ShouldReturnSeekableContentAndTypeFromExtension()
    {
        using var content = new MemoryStream([1, 2, 3]);
        await _fileStorage.SaveAsync("properties/1/video.mp4", content, "video/mp4");

        var file = await _fileStorage.OpenReadAsync("properties/1/video.mp4");

        Assert.NotNull(file);
        await using var stored = file.Content;
        Assert.Equal("video/mp4", file.ContentType);
        Assert.True(stored.CanSeek);
        Assert.Equal(3, stored.Length);
    }

    [Fact]
    public async Task OpenReadAsync_WithMissingFile_ShouldReturnNull()
    {
        Assert.Null(await _fileStorage.OpenReadAsync("properties/1/inexistente.jpg"));
    }

    [Theory]
    [InlineData("../fora.txt")]
    [InlineData("properties/../../fora.txt")]
    public async Task SaveAsync_WithPathOutsideRoot_ShouldThrowArgumentException(string relativePath)
    {
        using var content = new MemoryStream([1]);

        await Assert.ThrowsAsync<ArgumentException>(() => _fileStorage.SaveAsync(relativePath, content, "text/plain"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
