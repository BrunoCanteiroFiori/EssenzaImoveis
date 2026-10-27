namespace Backend.Infrastructure.Storage;

public static class ImageFormats
{
    /// <summary>
    /// Quantidade de bytes do início do arquivo necessária para <see cref="DetectExtension"/>.
    /// </summary>
    public const int HeaderLength = 12;

    /// <summary>
    /// Identifica JPEG, PNG ou WebP pela assinatura do arquivo (não pela extensão informada).
    /// Retorna a extensão (".jpg", ".png" ou ".webp") ou nulo se não for um desses formatos.
    /// </summary>
    public static string? DetectExtension(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }

        if (header.Length >= HeaderLength && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        return null;
    }
}
