namespace Backend.Application.Properties;

/// <summary>
/// Caminhos das fotos e vídeos: relativo no armazenamento (<c>properties/{id}/{arquivo}</c>, uma pasta por imóvel)
/// e URL pública na API (<c>/properties/{id}/media/{arquivo}</c>).
/// </summary>
public static class PropertyMediaPaths
{
    public static string GetDirectory(Guid propertyId) => $"properties/{propertyId}";

    public static string GetRelativePath(Guid propertyId, string fileName) => $"{GetDirectory(propertyId)}/{fileName}";

    public static string GetUrl(Guid propertyId, string fileName) => $"/properties/{propertyId}/media/{fileName}";
}
