namespace Backend.Infrastructure.Storage;

/// <summary>
/// Arquivo aberto para leitura. <see cref="Content"/> permite Seek e deve ser descartado por quem o recebe.
/// </summary>
public sealed record StoredFile(Stream Content, string ContentType);
