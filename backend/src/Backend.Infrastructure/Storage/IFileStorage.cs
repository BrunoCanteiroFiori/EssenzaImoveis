namespace Backend.Infrastructure.Storage;

/// <summary>
/// Armazenamento de arquivos por caminho relativo (ex.: <c>properties/{id}/foto.jpg</c>).
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Grava um arquivo novo. Nunca sobrescreve: o caminho deve ser único.
    /// </summary>
    Task SaveAsync(string relativePath, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre o arquivo para leitura, ou retorna nulo se ele não existir. O conteúdo permite Seek (leitura por faixa de bytes).
    /// </summary>
    Task<StoredFile?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default);
}
