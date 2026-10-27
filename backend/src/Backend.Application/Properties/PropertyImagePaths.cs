namespace Backend.Application.Properties;

/// <summary>
/// Caminhos das imagens: relativo no armazenamento (<c>properties/{id}/{arquivo}</c>) e URL pública (<c>/uploads/properties/{id}/{arquivo}</c>).
/// </summary>
public static class PropertyImagePaths
{
    public const string PublicBasePath = "/uploads";

    public static string GetDirectory(Guid propertyId) => $"properties/{propertyId}";

    public static string GetRelativePath(Guid propertyId, string fileName) => $"{GetDirectory(propertyId)}/{fileName}";

    public static string GetUrl(Guid propertyId, string fileName) => $"{PublicBasePath}/{GetRelativePath(propertyId, fileName)}";
}
