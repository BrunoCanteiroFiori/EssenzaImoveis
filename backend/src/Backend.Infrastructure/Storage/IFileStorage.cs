namespace Backend.Infrastructure.Storage;

/// <summary>
/// Armazenamento de arquivos por caminho relativo (ex.: <c>properties/{id}/foto.jpg</c>).
/// </summary>
public interface IFileStorage
{
    Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default);
}
