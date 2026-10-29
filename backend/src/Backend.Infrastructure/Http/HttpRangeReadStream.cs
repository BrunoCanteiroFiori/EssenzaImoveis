using System.Net;
using System.Net.Http.Headers;

namespace Backend.Infrastructure.Http;

/// <summary>
/// Leitura de um arquivo remoto por faixas de bytes (cabeçalho Range), sem baixar o arquivo inteiro. A requisição só é feita
/// na primeira leitura, a partir da posição atual; um <see cref="Seek"/> fecha a resposta aberta e a próxima leitura recomeça
/// na nova posição. Assim, quem lê só uma parte (ex.: resposta 206 de um vídeo) só transfere essa parte.
/// </summary>
public sealed class HttpRangeReadStream(HttpClient httpClient, string requestUri, long length) : Stream
{
    private HttpResponseMessage? _response;
    private Stream? _content;
    private long _position;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => length;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.IsEmpty || _position >= length)
        {
            return 0;
        }

        _content ??= await OpenAsync(cancellationToken);
        var read = await _content.ReadAsync(buffer, cancellationToken);
        _position += read;
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <summary>
    /// Leitura síncrona, exigida pelo contrato de <see cref="Stream"/>. Prefira <see cref="ReadAsync(Memory{byte}, CancellationToken)"/>.
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        ArgumentOutOfRangeException.ThrowIfNegative(position, nameof(offset));

        if (position != _position)
        {
            CloseResponse();
            _position = position;
        }

        return _position;
    }

    /// <summary>
    /// Stream somente leitura: não há dados pendentes de gravação.
    /// </summary>
    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException("Stream somente leitura.");

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Stream somente leitura.");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CloseResponse();
        }

        base.Dispose(disposing);
    }

    private async Task<Stream> OpenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Range = new RangeHeaderValue(_position, null);
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode || (_position > 0 && response.StatusCode != HttpStatusCode.PartialContent))
        {
            var statusCode = (int)response.StatusCode;
            response.Dispose();
            throw new IOException($"Não foi possível ler o arquivo remoto a partir do byte {_position} (HTTP {statusCode}).");
        }

        _response = response;
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private void CloseResponse()
    {
        _content?.Dispose();
        _content = null;
        _response?.Dispose();
        _response = null;
    }
}
