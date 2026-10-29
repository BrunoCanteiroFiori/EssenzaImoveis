using System.Collections.Concurrent;
using System.Net;
using Backend.Infrastructure.Http;
using Google;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using DrivePermission = Google.Apis.Drive.v3.Data.Permission;

namespace Backend.Infrastructure.Storage;

/// <summary>
/// Armazenamento no Google Drive. Cada pasta do caminho relativo vira uma pasta do Drive dentro da pasta raiz
/// (<see cref="GoogleDriveOptions.RootFolderName"/>), criada na primeira gravação com leitura pública para quem tiver o link;
/// subpastas e arquivos herdam essa permissão. Excluir move para a lixeira do Drive, que guarda os itens por 30 dias.
/// Os Ids de pastas e arquivos ficam em memória: os caminhos são únicos e os arquivos nunca são sobrescritos.
/// </summary>
public sealed class GoogleDriveFileStorage(DriveService driveService, GoogleDriveOptions options) : IFileStorage, IDisposable
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";
    private const string MyDriveRootId = "root";
    private const string FileFields = "id, size, mimeType";

    private readonly ConcurrentDictionary<string, string> _folderIds = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DriveFile> _files = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _folderLock = new(1, 1);

    public async Task SaveAsync(string relativePath, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var (folders, fileName) = SplitFilePath(relativePath);
        var folderId = await ResolveFolderAsync(folders, create: true, cancellationToken)
            ?? throw new IOException($"Não foi possível criar a pasta de '{relativePath}' no Google Drive.");

        var upload = driveService.Files.Create(new DriveFile { Name = fileName, Parents = [folderId] }, content, contentType);
        upload.Fields = FileFields;
        var progress = await upload.UploadAsync(cancellationToken);
        if (progress.Status != UploadStatus.Completed)
        {
            throw new IOException($"Não foi possível enviar '{relativePath}' ao Google Drive.", progress.Exception);
        }

        _files[relativePath] = upload.ResponseBody;
    }

    public async Task<StoredFile?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var file = await FindFileAsync(relativePath, cancellationToken);
        if (file is null)
        {
            return null;
        }

        var downloadUri = $"{driveService.BaseUri}files/{Uri.EscapeDataString(file.Id)}?alt=media";
        return new StoredFile(new HttpRangeReadStream(driveService.HttpClient, downloadUri, file.Size ?? 0), file.MimeType);
    }

    public async Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var file = await FindFileAsync(relativePath, cancellationToken);
        if (file is null)
        {
            return;
        }

        await TrashAsync(file.Id, cancellationToken);
        _files.TryRemove(relativePath, out _);
    }

    public async Task DeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var folders = SplitPath(relativePath);
        var folderId = await ResolveFolderAsync(folders, create: false, cancellationToken);
        if (folderId is null)
        {
            return;
        }

        await TrashAsync(folderId, cancellationToken);
        var folderPath = string.Join('/', folders);
        var prefix = $"{folderPath}/";
        foreach (var path in _folderIds.Keys.Where(path => path == folderPath || path.StartsWith(prefix, StringComparison.Ordinal)))
        {
            _folderIds.TryRemove(path, out _);
        }

        foreach (var path in _files.Keys.Where(path => path.StartsWith(prefix, StringComparison.Ordinal)))
        {
            _files.TryRemove(path, out _);
        }
    }

    public void Dispose() => _folderLock.Dispose();

    private static string[] SplitPath(string relativePath)
    {
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Caminho de arquivo inválido.", nameof(relativePath));
        }

        return segments;
    }

    private static (string[] Folders, string FileName) SplitFilePath(string relativePath)
    {
        var segments = SplitPath(relativePath);
        return (segments[..^1], segments[^1]);
    }

    private static string EscapeQueryValue(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal);

    private async Task<DriveFile?> FindFileAsync(string relativePath, CancellationToken cancellationToken)
    {
        if (_files.TryGetValue(relativePath, out var cachedFile))
        {
            return cachedFile;
        }

        var (folders, fileName) = SplitFilePath(relativePath);
        var folderId = await ResolveFolderAsync(folders, create: false, cancellationToken);
        if (folderId is null)
        {
            return null;
        }

        var file = await FindChildAsync(folderId, fileName, isFolder: false, cancellationToken);
        if (file is not null)
        {
            _files[relativePath] = file;
        }

        return file;
    }

    /// <summary>
    /// Id da pasta do caminho, a partir da pasta raiz. Com <paramref name="create"/>, cria as pastas que faltam; sem ele,
    /// retorna nulo se alguma não existir.
    /// </summary>
    private async Task<string?> ResolveFolderAsync(string[] folders, bool create, CancellationToken cancellationToken)
    {
        var folderId = await ResolveChildFolderAsync(string.Empty, MyDriveRootId, options.RootFolderName, create, isPublic: true, cancellationToken);
        for (var index = 0; index < folders.Length && folderId is not null; index++)
        {
            var path = string.Join('/', folders[..(index + 1)]);
            folderId = await ResolveChildFolderAsync(path, folderId, folders[index], create, isPublic: false, cancellationToken);
        }

        return folderId;
    }

    private async Task<string?> ResolveChildFolderAsync(
        string path,
        string parentId,
        string name,
        bool create,
        bool isPublic,
        CancellationToken cancellationToken)
    {
        if (_folderIds.TryGetValue(path, out var folderId))
        {
            return folderId;
        }

        await _folderLock.WaitAsync(cancellationToken);
        try
        {
            if (_folderIds.TryGetValue(path, out folderId))
            {
                return folderId;
            }

            folderId = (await FindChildAsync(parentId, name, isFolder: true, cancellationToken))?.Id;
            if (folderId is null && create)
            {
                folderId = await CreateFolderAsync(parentId, name, isPublic, cancellationToken);
            }

            if (folderId is not null)
            {
                _folderIds[path] = folderId;
            }

            return folderId;
        }
        finally
        {
            _folderLock.Release();
        }
    }

    private async Task<DriveFile?> FindChildAsync(string parentId, string name, bool isFolder, CancellationToken cancellationToken)
    {
        var request = driveService.Files.List();
        var mimeTypeOperator = isFolder ? "=" : "!=";
        request.Q = $"name = '{EscapeQueryValue(name)}' and '{parentId}' in parents and trashed = false and mimeType {mimeTypeOperator} '{FolderMimeType}'";
        request.Fields = $"files({FileFields})";
        request.PageSize = 1;
        var result = await request.ExecuteAsync(cancellationToken);
        return result.Files?.FirstOrDefault();
    }

    private async Task<string> CreateFolderAsync(string parentId, string name, bool isPublic, CancellationToken cancellationToken)
    {
        var request = driveService.Files.Create(new DriveFile { Name = name, MimeType = FolderMimeType, Parents = [parentId] });
        request.Fields = "id";
        var folder = await request.ExecuteAsync(cancellationToken);
        if (isPublic)
        {
            var permission = new DrivePermission { Type = "anyone", Role = "reader" };
            await driveService.Permissions.Create(permission, folder.Id).ExecuteAsync(cancellationToken);
        }

        return folder.Id;
    }

    private async Task TrashAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            await driveService.Files.Update(new DriveFile { Trashed = true }, id).ExecuteAsync(cancellationToken);
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
            // Já foi removido direto no Drive: não há o que excluir.
        }
    }
}
