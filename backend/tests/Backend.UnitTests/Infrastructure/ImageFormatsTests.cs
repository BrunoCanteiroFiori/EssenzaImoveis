using Backend.Infrastructure.Storage;

namespace Backend.UnitTests.Infrastructure;

public sealed class ImageFormatsTests
{
    [Fact]
    public void DetectExtension_WithJpegHeader_ShouldReturnJpg()
    {
        Assert.Equal(".jpg", ImageFormats.DetectExtension([0xFF, 0xD8, 0xFF, 0xE0]));
    }

    [Fact]
    public void DetectExtension_WithPngHeader_ShouldReturnPng()
    {
        Assert.Equal(".png", ImageFormats.DetectExtension([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
    }

    [Fact]
    public void DetectExtension_WithWebpHeader_ShouldReturnWebp()
    {
        Assert.Equal(".webp", ImageFormats.DetectExtension("RIFF\0\0\0\0WEBP"u8));
    }

    [Fact]
    public void DetectExtension_WithUnknownHeader_ShouldReturnNull()
    {
        Assert.Null(ImageFormats.DetectExtension("GIF89a"u8));
    }
}
