using Backend.Api.Authorization;
using Backend.Application.Properties;
using Backend.Application.Properties.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>
/// Imagens de um imóvel (carrossel). A primeira imagem é a capa.
/// </summary>
[Route("properties/{propertyId:guid}/images")]
[Authorize(Policy = Policies.WriteProperties)]
public sealed class PropertyImagesController(IPropertyImageService propertyImageService) : ApiControllerBase
{
    private const long MaxRequestBytes = (PropertyImageService.MaxFileSizeBytes * (long)PropertyImageService.MaxFilesPerUpload) + (1024 * 1024);

    /// <summary>
    /// Envia de 1 a 10 imagens (JPG, PNG ou WebP, até 10 MB cada) no campo de formulário <c>files</c>.
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
    public async Task<ActionResult<PropertyResponse>> Upload(
        Guid propertyId,
        [FromForm] IFormFileCollection files,
        CancellationToken cancellationToken)
    {
        var uploads = files.Select(file => new PropertyImageUpload(file.FileName, file.OpenReadStream())).ToList();
        try
        {
            var result = await propertyImageService.AddAsync(propertyId, uploads, cancellationToken);
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
    /// Remove uma imagem do imóvel.
    /// </summary>
    [HttpDelete("{fileName}")]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponse>> Remove(Guid propertyId, string fileName, CancellationToken cancellationToken)
    {
        var result = await propertyImageService.RemoveAsync(propertyId, fileName, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Define a nova ordem das imagens. A primeira passa a ser a capa.
    /// </summary>
    [HttpPut("order")]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponse>> Reorder(
        Guid propertyId,
        ReorderPropertyImagesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await propertyImageService.ReorderAsync(propertyId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }
}
