namespace Backend.Application.Properties;

/// <summary>
/// Foto ou vídeo enviado para um imóvel. <see cref="FileName"/> é o nome original, usado só em mensagens.
/// <see cref="Content"/> precisa permitir Seek: o começo do arquivo é lido para identificar o formato.
/// </summary>
public sealed record PropertyMediaUpload(string FileName, Stream Content);
