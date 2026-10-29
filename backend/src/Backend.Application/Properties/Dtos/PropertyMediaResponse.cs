using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Foto ou vídeo do carrossel do imóvel.
/// </summary>
public sealed record PropertyMediaResponse(string FileName, PropertyMediaType Type, string Url);
