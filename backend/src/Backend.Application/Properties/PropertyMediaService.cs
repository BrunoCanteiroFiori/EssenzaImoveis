using Backend.Application.Common;
using Backend.Application.Properties.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Common;
using Backend.Domain.Properties;
using Backend.Infrastructure.Enums;
using Backend.Infrastructure.Storage;

namespace Backend.Application.Properties;

public sealed class PropertyMediaService(
    IPropertyRepository propertyRepository,
    IUnitOfWork unitOfWork,
    IFileStorage fileStorage,
    TimeProvider timeProvider) : IPropertyMediaService
{
    public const int MaxPhotoSizeBytes = 10 * 1024 * 1024;
    public const int MaxVideoSizeBytes = 200 * 1024 * 1024;
    public const int MaxFilesPerUpload = 10;

    /// <summary>
    /// Valida o formato real de cada arquivo (foto JPG, PNG ou WebP; vídeo MP4 ou WebM) e o tamanho, adiciona ao imóvel com
    /// nome gerado e grava no armazenamento. Se algum arquivo for inválido, nada é gravado; se a gravação falhar no meio,
    /// os arquivos já gravados são apagados.
    /// </summary>
    public async Task<Result<PropertyResponse>> AddAsync(
        Guid propertyId,
        IReadOnlyList<PropertyMediaUpload> uploads,
        CancellationToken cancellationToken = default)
    {
        if (uploads.Count is 0 or > MaxFilesPerUpload)
        {
            return PropertyErrors.InvalidMedia($"Envie de 1 a {MaxFilesPerUpload} arquivos por vez.");
        }

        var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return PropertyErrors.NotFound(propertyId);
        }

        var now = timeProvider.GetUtcNow();
        var files = new List<(string Path, Stream Content, string ContentType)>();
        foreach (var upload in uploads)
        {
            var format = await DetectFormatAsync(upload.Content, cancellationToken);
            if (format is null)
            {
                return PropertyErrors.InvalidMedia($"O arquivo '{upload.FileName}' não é uma foto (JPG, PNG ou WebP) nem um vídeo (MP4 ou WebM).");
            }

            var maxSizeBytes = format.Kind == MediaKind.Video ? MaxVideoSizeBytes : MaxPhotoSizeBytes;
            if (upload.Content.Length > maxSizeBytes)
            {
                return PropertyErrors.InvalidMedia($"O arquivo '{upload.FileName}' deve ter até {maxSizeBytes / 1024 / 1024} MB.");
            }

            var fileName = $"{Guid.CreateVersion7():N}{format.Extension}";
            try
            {
                property.AddMedia(fileName, now);
            }
            catch (DomainException exception)
            {
                return PropertyErrors.InvalidMedia(exception.Message);
            }

            files.Add((PropertyMediaPaths.GetRelativePath(property.Id, fileName), upload.Content, format.ContentType));
        }

        await SaveAsync(property, files, cancellationToken);
        return property.ToResponse(now);
    }

    public async Task<Result<PropertyResponse>> RemoveAsync(Guid propertyId, string fileName, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return PropertyErrors.NotFound(propertyId);
        }

        if (!property.Media.Contains(fileName))
        {
            return PropertyErrors.MediaNotFound(fileName);
        }

        var now = timeProvider.GetUtcNow();
        property.RemoveMedia(fileName, now);
        propertyRepository.Update(property);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteAsync(PropertyMediaPaths.GetRelativePath(propertyId, fileName), cancellationToken);
        return property.ToResponse(now);
    }

    public async Task<Result<PropertyResponse>> ReorderAsync(
        Guid propertyId,
        ReorderPropertyMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return PropertyErrors.NotFound(propertyId);
        }

        var now = timeProvider.GetUtcNow();
        try
        {
            property.ReorderMedia(request.FileNames, now);
        }
        catch (DomainException exception)
        {
            return PropertyErrors.InvalidMedia(exception.Message);
        }

        propertyRepository.Update(property);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return property.ToResponse(now);
    }

    public async Task<Result<StoredFile>> OpenAsync(Guid propertyId, string fileName, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return PropertyErrors.NotFound(propertyId);
        }

        if (!property.Media.Contains(fileName))
        {
            return PropertyErrors.MediaNotFound(fileName);
        }

        var file = await fileStorage.OpenReadAsync(PropertyMediaPaths.GetRelativePath(propertyId, fileName), cancellationToken);
        if (file is null)
        {
            return PropertyErrors.MediaNotFound(fileName);
        }

        return file;
    }

    private static async Task<MediaFormat?> DetectFormatAsync(Stream content, CancellationToken cancellationToken)
    {
        var header = new byte[MediaFormats.HeaderLength];
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;
        return MediaFormats.Detect(header.AsSpan(0, read));
    }

    /// <summary>
    /// Grava os arquivos e depois o imóvel. Se algo falhar, apaga os arquivos já gravados para não deixar arquivo órfão.
    /// </summary>
    private async Task SaveAsync(
        Property property,
        IReadOnlyList<(string Path, Stream Content, string ContentType)> files,
        CancellationToken cancellationToken)
    {
        var savedPaths = new List<string>();
        try
        {
            foreach (var file in files)
            {
                await fileStorage.SaveAsync(file.Path, file.Content, file.ContentType, cancellationToken);
                savedPaths.Add(file.Path);
            }

            propertyRepository.Update(property);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var path in savedPaths)
            {
                await fileStorage.DeleteAsync(path, CancellationToken.None);
            }

            throw;
        }
    }
}
