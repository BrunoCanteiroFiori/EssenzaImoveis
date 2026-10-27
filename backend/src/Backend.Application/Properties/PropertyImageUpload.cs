namespace Backend.Application.Properties;

/// <summary>
/// Arquivo de imagem enviado para um imóvel. <see cref="FileName"/> é o nome original, usado só em mensagens.
/// </summary>
public sealed record PropertyImageUpload(string FileName, Stream Content);
