using Backend.Api.Authorization;
using Backend.Application.Properties;
using Backend.Application.Properties.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Backend.Api.Controllers;

/// <summary>
/// Fotos e vídeos de um imóvel (carrossel). A capa do anúncio é a primeira foto.
/// </summary>
[Route("properties/{propertyId:guid}/media")]
[Authorize(Policy = Policies.WriteProperties)]
public sealed class PropertyMediaController(IPropertyMediaService propertyMediaService) : ApiControllerBase
{
    private const long MaxRequestBytes = PropertyMediaService.MaxVideoSizeBytes + (1024L * 1024);

    /// <summary>
    /// O nome do arquivo é gerado no envio e nunca reutilizado, então o navegador pode guardar o arquivo por um ano.
    /// </summary>
    private const string MediaCacheControl = "public, max-age=31536000, immutable";

    /// <summary>
    /// Envia de 1 a 10 arquivos no campo de formulário <c>files</c>: fotos JPG, PNG ou WebP (até 10 MB cada) e vídeos MP4 ou
    /// WebM (até 200 MB cada). A requisição inteira tem até 201 MB, então vídeos vão um por vez. Vídeos entram no começo do carrossel.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<PropertyResponse>> Upload(
        Guid propertyId,
        [FromForm] IFormFileCollection files,
        CancellationToken cancellationToken)
    {
        var uploads = files.Select(file => new PropertyMediaUpload(file.FileName, file.OpenReadStream())).ToList();
        try
        {
            var result = await propertyMediaService.AddAsync(propertyId, uploads, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
        }
        finally
        {
            foreach (var upload in uploads)
            {
                await upload.Content.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Foto ou vídeo do imóvel, lido do armazenamento. Público. Aceita pedidos parciais (cabeçalho Range), usados pelo player
    /// de vídeo, e <c>If-None-Match</c>.
    /// </summary>
    [HttpGet("{fileName}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
    public async Task<IActionResult> Download(Guid propertyId, string fileName, CancellationToken cancellationToken)
    {
        var result = await propertyMediaService.OpenAsync(propertyId, fileName, cancellationToken);
        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        Response.Headers.CacheControl = MediaCacheControl;
        return File(
            result.Value.Content,
            result.Value.ContentType,
            lastModified: null,
            new EntityTagHeaderValue($"\"{fileName}\""),
            enableRangeProcessing: true);
    }

    /// <summary>
    /// Remove uma foto ou vídeo do imóvel.
    /// </summary>
    [HttpDelete("{fileName}")]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponse>> Remove(Guid propertyId, string fileName, CancellationToken cancellationToken)
    {
        var result = await propertyMediaService.RemoveAsync(propertyId, fileName, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Define a nova ordem das fotos e vídeos do carrossel. A capa é a primeira foto da lista.
    /// </summary>
    [HttpPut("order")]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponse>> Reorder(
        Guid propertyId,
        ReorderPropertyMediaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await propertyMediaService.ReorderAsync(propertyId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }
}
