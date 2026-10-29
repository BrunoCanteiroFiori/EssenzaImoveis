using Backend.Infrastructure.Enums;

namespace Backend.Infrastructure.Storage;

/// <summary>
/// Formato de imagem ou vídeo: extensão usada no nome do arquivo (ex.: <c>.jpg</c>) e tipo do conteúdo (ex.: <c>image/jpeg</c>).
/// </summary>
public sealed record MediaFormat(string Extension, string ContentType, MediaKind Kind);
