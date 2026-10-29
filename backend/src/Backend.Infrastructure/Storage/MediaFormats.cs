using System.Text;
using Backend.Infrastructure.Enums;

namespace Backend.Infrastructure.Storage;

public static class MediaFormats
{
    /// <summary>
    /// Quantidade de bytes do início do arquivo necessária para <see cref="Detect"/>.
    /// </summary>
    public const int HeaderLength = 64;

    /// <summary>
    /// Marcas (brands) do cabeçalho <c>ftyp</c> aceitas como MP4. Ficam de fora o QuickTime (<c>qt</c>, arquivos .mov) e o HEIF/HEIC.
    /// </summary>
    private static readonly string[] _mp4Brands = ["isom", "iso2", "iso4", "iso5", "iso6", "mp41", "mp42", "avc1", "M4V ", "MSNV", "dash"];

    public static MediaFormat Jpeg { get; } = new(".jpg", "image/jpeg", MediaKind.Image);

    public static MediaFormat Png { get; } = new(".png", "image/png", MediaKind.Image);

    public static MediaFormat Webp { get; } = new(".webp", "image/webp", MediaKind.Image);

    public static MediaFormat Mp4 { get; } = new(".mp4", "video/mp4", MediaKind.Video);

    public static MediaFormat Webm { get; } = new(".webm", "video/webm", MediaKind.Video);

    public static IReadOnlyList<MediaFormat> All { get; } = [Jpeg, Png, Webp, Mp4, Webm];

    /// <summary>
    /// Identifica JPEG, PNG, WebP, MP4 ou WebM pela assinatura do arquivo (não pela extensão informada).
    /// Retorna nulo se não for um desses formatos.
    /// </summary>
    public static MediaFormat? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return Jpeg;
        }

        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return Png;
        }

        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
        {
            return Webp;
        }

        if (header.Length >= 12 && header[4..8].SequenceEqual("ftyp"u8) && _mp4Brands.Contains(Encoding.ASCII.GetString(header[8..12])))
        {
            return Mp4;
        }

        if (header.Length >= 4 && header[..4].SequenceEqual(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }) && header.IndexOf("webm"u8) >= 0)
        {
            return Webm;
        }

        return null;
    }

    /// <summary>
    /// Formato pela extensão do nome do arquivo (ex.: <c>.mp4</c>), ou nulo se não for um dos formatos conhecidos.
    /// </summary>
    public static MediaFormat? FromFileName(string fileName) =>
        All.FirstOrDefault(format => fileName.EndsWith(format.Extension, StringComparison.OrdinalIgnoreCase));
}
