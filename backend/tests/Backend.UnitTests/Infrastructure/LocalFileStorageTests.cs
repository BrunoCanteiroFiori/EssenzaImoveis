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

        await _fileStorage.SaveAsync("properties/1/foto.jpg", content);
        var saved = File.Exists(Path.Combine(_rootPath, "properties", "1", "foto.jpg"));
        await _fileStorage.DeleteDirectoryAsync("properties/1");

        Assert.True(saved);
        Assert.False(Directory.Exists(Path.Combine(_rootPath, "properties", "1")));
    }

    [Theory]
    [InlineData("../fora.txt")]
    [InlineData("properties/../../fora.txt")]
    public async Task SaveAsync_WithPathOutsideRoot_ShouldThrowArgumentException(string relativePath)
    {
        using var content = new MemoryStream([1]);

        await Assert.ThrowsAsync<ArgumentException>(() => _fileStorage.SaveAsync(relativePath, content));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
