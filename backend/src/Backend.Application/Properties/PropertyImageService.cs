using Backend.Application.Common;
using Backend.Application.Properties.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Common;
using Backend.Domain.Properties;
using Backend.Infrastructure.Storage;

namespace Backend.Application.Properties;

public sealed class PropertyImageService(
    IPropertyRepository propertyRepository,
    IUnitOfWork unitOfWork,
    IFileStorage fileStorage,
    TimeProvider timeProvider) : IPropertyImageService
{
    public const int MaxFileSizeBytes = 10 * 1024 * 1024;
    public const int MaxFilesPerUpload = 10;

    /// <summary>
    /// Valida o formato real de cada arquivo (JPG, PNG ou WebP) e o tamanho, grava com nome gerado e adiciona ao imóvel.
    /// Se algum arquivo for inválido, nada é adicionado e os arquivos já gravados são apagados.
    /// </summary>
    public async Task<Result<PropertyResponse>> AddAsync(
        Guid propertyId,
        IReadOnlyList<PropertyImageUpload> uploads,
        CancellationToken cancellationToken = default)
    {
        if (uploads.Count is 0 or > MaxFilesPerUpload)
        {
            return PropertyErrors.InvalidImage($"Envie de 1 a {MaxFilesPerUpload} imagens por vez.");
        }

        var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return PropertyErrors.NotFound(propertyId);
        }

        var now = timeProvider.GetUtcNow();
        var savedPaths = new List<string>();
        foreach (var upload in uploads)
        {
            var error = await SaveAsync(property, upload, now, savedPaths, cancellationToken);
            if (error is not null)
            {
                foreach (var path in savedPaths)
                {
                    await fileStorage.DeleteAsync(path, cancellationToken);
                }

                return error;
            }
        }

        propertyRepository.Update(property);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return property.ToResponse(now);
    }

    public async Task<Result<PropertyResponse>> RemoveAsync(Guid propertyId, string fileName, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return PropertyErrors.NotFound(propertyId);
        }

        if (!property.Images.Contains(fileName))
        {
            return PropertyErrors.ImageNotFound(fileName);
        }

        var now = timeProvider.GetUtcNow();
        property.RemoveImage(fileName, now);
        propertyRepository.Update(property);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteAsync(PropertyImagePaths.GetRelativePath(propertyId, fileName), cancellationToken);
        return property.ToResponse(now);
    }

    public async Task<Result<PropertyResponse>> ReorderAsync(
        Guid propertyId,
        ReorderPropertyImagesRequest request,
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
            property.ReorderImages(request.FileNames, now);
        }
        catch (DomainException exception)
        {
            return PropertyErrors.InvalidImage(exception.Message);
        }

        propertyRepository.Update(property);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return property.ToResponse(now);
    }

    private async Task<Error?> SaveAsync(
        Property property,
        PropertyImageUpload upload,
        DateTimeOffset now,
        List<string> savedPaths,
        CancellationToken cancellationToken)
    {
        using var content = new MemoryStream();
        await upload.Content.CopyToAsync(content, cancellationToken);
        if (content.Length is 0 or > MaxFileSizeBytes)
        {
            return PropertyErrors.InvalidImage($"A imagem '{upload.FileName}' deve ter até {MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        var header = content.GetBuffer().AsSpan(0, (int)Math.Min(content.Length, ImageFormats.HeaderLength));
        var extension = ImageFormats.DetectExtension(header);
        if (extension is null)
        {
            return PropertyErrors.InvalidImage($"O arquivo '{upload.FileName}' não é uma imagem JPG, PNG ou WebP.");
        }

        var fileName = $"{Guid.CreateVersion7():N}{extension}";
        try
        {
            property.AddImage(fileName, now);
        }
        catch (DomainException exception)
        {
            return PropertyErrors.InvalidImage(exception.Message);
        }

        var path = PropertyImagePaths.GetRelativePath(property.Id, fileName);
        content.Position = 0;
        await fileStorage.SaveAsync(path, content, cancellationToken);
        savedPaths.Add(path);
        return null;
    }
}
