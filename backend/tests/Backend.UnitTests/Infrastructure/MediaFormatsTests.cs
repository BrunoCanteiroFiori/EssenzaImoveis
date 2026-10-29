using System.Text;
using Backend.Infrastructure.Enums;
using Backend.Infrastructure.Storage;

namespace Backend.UnitTests.Infrastructure;

public sealed class MediaFormatsTests
{
    [Fact]
    public void Detect_WithJpegHeader_ShouldReturnJpeg()
    {
        Assert.Equal(MediaFormats.Jpeg, MediaFormats.Detect([0xFF, 0xD8, 0xFF, 0xE0]));
    }

    [Fact]
    public void Detect_WithPngHeader_ShouldReturnPng()
    {
        Assert.Equal(MediaFormats.Png, MediaFormats.Detect([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
    }

    [Fact]
    public void Detect_WithWebpHeader_ShouldReturnWebp()
    {
        Assert.Equal(MediaFormats.Webp, MediaFormats.Detect("RIFF\0\0\0\0WEBP"u8));
    }

    [Theory]
    [InlineData("isom")]
    [InlineData("mp42")]
    [InlineData("avc1")]
    public void Detect_WithMp4Header_ShouldReturnMp4Video(string brand)
    {
        var format = MediaFormats.Detect([0x00, 0x00, 0x00, 0x20, .. "ftyp"u8.ToArray(), .. Encoding.ASCII.GetBytes(brand)]);

        Assert.Equal(MediaFormats.Mp4, format);
        Assert.Equal(MediaKind.Video, format?.Kind);
    }

    [Theory]
    [InlineData("qt  ")]
    [InlineData("heic")]
    public void Detect_WithQuickTimeOrHeicHeader_ShouldReturnNull(string brand)
    {
        Assert.Null(MediaFormats.Detect([0x00, 0x00, 0x00, 0x20, .. "ftyp"u8.ToArray(), .. Encoding.ASCII.GetBytes(brand)]));
    }

    [Fact]
    public void Detect_WithWebmHeader_ShouldReturnWebm()
    {
        Assert.Equal(MediaFormats.Webm, MediaFormats.Detect([0x1A, 0x45, 0xDF, 0xA3, 0x9F, 0x42, 0x86, 0x81, 0x01, 0x42, 0x82, 0x84, .. "webm"u8.ToArray()]));
    }

    [Fact]
    public void Detect_WithMatroskaHeader_ShouldReturnNull()
    {
        Assert.Null(MediaFormats.Detect([0x1A, 0x45, 0xDF, 0xA3, 0x42, 0x82, 0x88, .. "matroska"u8.ToArray()]));
    }

    [Fact]
    public void Detect_WithUnknownHeader_ShouldReturnNull()
    {
        Assert.Null(MediaFormats.Detect("GIF89a"u8));
    }

    [Theory]
    [InlineData("foto.JPG", "image/jpeg")]
    [InlineData("video.webm", "video/webm")]
    public void FromFileName_WithKnownExtension_ShouldReturnFormat(string fileName, string contentType)
    {
        Assert.Equal(contentType, MediaFormats.FromFileName(fileName)?.ContentType);
    }
}
