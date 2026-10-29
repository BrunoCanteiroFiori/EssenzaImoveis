using System.Net;
using Backend.Infrastructure.Http;

namespace Backend.UnitTests.Infrastructure;

public sealed class HttpRangeReadStreamTests
{
    private static readonly byte[] _content = [.. Enumerable.Range(0, 100).Select(value => (byte)value)];

    [Fact]
    public async Task ReadAsync_FromStart_ShouldReturnWholeContent()
    {
        var handler = new RangeHttpMessageHandler(_content);
        using var httpClient = new HttpClient(handler);
        await using var stream = new HttpRangeReadStream(httpClient, "https://example.com/file", _content.Length);
        using var destination = new MemoryStream();

        await stream.CopyToAsync(destination);

        Assert.Equal(_content, destination.ToArray());
        Assert.Equal(["bytes=0-"], handler.Ranges);
    }

    [Fact]
    public async Task Seek_ThenReadAsync_ShouldRequestOnlyFromNewPosition()
    {
        var handler = new RangeHttpMessageHandler(_content);
        using var httpClient = new HttpClient(handler);
        await using var stream = new HttpRangeReadStream(httpClient, "https://example.com/file", _content.Length);
        var buffer = new byte[10];

        stream.Seek(40, SeekOrigin.Begin);
        var read = await stream.ReadAsync(buffer);

        Assert.Equal(10, read);
        Assert.Equal(_content[40..50], buffer);
        Assert.Equal(50, stream.Position);
        Assert.Equal(["bytes=40-"], handler.Ranges);
    }

    [Fact]
    public void Seek_BeforeAnyRead_ShouldNotSendRequest()
    {
        var handler = new RangeHttpMessageHandler(_content);
        using var httpClient = new HttpClient(handler);
        using var stream = new HttpRangeReadStream(httpClient, "https://example.com/file", _content.Length);

        stream.Seek(-10, SeekOrigin.End);

        Assert.Equal(90, stream.Position);
        Assert.Empty(handler.Ranges);
    }

    [Fact]
    public async Task ReadAsync_WhenServerIgnoresRange_ShouldThrowIOException()
    {
        var handler = new RangeHttpMessageHandler(_content, ignoreRange: true);
        using var httpClient = new HttpClient(handler);
        await using var stream = new HttpRangeReadStream(httpClient, "https://example.com/file", _content.Length);
        stream.Seek(10, SeekOrigin.Begin);

        await Assert.ThrowsAsync<IOException>(async () => await stream.ReadExactlyAsync(new byte[10]));
    }

    private sealed class RangeHttpMessageHandler(byte[] content, bool ignoreRange = false) : HttpMessageHandler
    {
        public List<string> Ranges { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var range = request.Headers.Range!;
            Ranges.Add(range.ToString());
            var from = (int)range.Ranges.Single().From!.Value;
            var response = ignoreRange
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) }
                : new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(content[from..]) };
            return Task.FromResult(response);
        }
    }
}
