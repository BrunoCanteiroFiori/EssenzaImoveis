namespace Backend.Infrastructure.Storage;

/// <summary>
/// Armazenamento em disco. Rejeita caminhos que saiam da pasta raiz (path traversal) e nunca sobrescreve arquivo existente.
/// O tipo do conteúdo não é gravado: na leitura ele vem da extensão do arquivo (<see cref="MediaFormats"/>).
/// </summary>
public sealed class LocalFileStorage(string rootPath) : IFileStorage
{
    private const string DefaultContentType = "application/octet-stream";
    private const int BufferSize = 81920;

    private readonly string _rootPath = Path.GetFullPath(rootPath);

    public async Task SaveAsync(string relativePath, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<StoredFile?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<StoredFile?>(null);
        }

        var content = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        var contentType = MediaFormats.FromFileName(fullPath)?.ContentType ?? DefaultContentType;
        return Task.FromResult<StoredFile?>(new StoredFile(content, contentType));
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("O caminho informado está fora da pasta de armazenamento.", nameof(relativePath));
        }

        return fullPath;
    }
}
