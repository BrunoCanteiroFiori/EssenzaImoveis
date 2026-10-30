using System.ComponentModel.DataAnnotations;
using Backend.Api.RateLimiting;
using Backend.Api.Visitors;
using Backend.Application.Common;
using Backend.Application.Properties.Dtos;
using Backend.Application.SavedProperties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Api.Controllers;

/// <summary>
/// Imóveis salvos por um visitante. Não há login público: o visitante é identificado pelo cabeçalho X-Visitor-Id.
/// </summary>
[Route("saved-properties")]
public sealed class SavedPropertiesController(ISavedPropertyService savedPropertyService) : ApiControllerBase
{
    /// <summary>
    /// Lista os salvos do visitante, do último para o primeiro, inclusive os que não estão mais disponíveis.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<PropertySummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PropertySummaryResponse>>> List(
        [FromHeader(Name = VisitorHeaders.Id), Required, MinLength(VisitorIdentity.IdMinLength), MaxLength(VisitorIdentity.IdMaxLength)] string visitorId,
        CancellationToken cancellationToken)
    {
        var properties = await savedPropertyService.ListAsync(visitorId, cancellationToken);
        return Ok(properties);
    }

    /// <summary>
    /// Salva um imóvel disponível. Salvar de novo não muda nada.
    /// </summary>
    [HttpPut("{propertyId:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.SavedProperties)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Save(
        Guid propertyId,
        [FromHeader(Name = VisitorHeaders.Id), Required, MinLength(VisitorIdentity.IdMinLength), MaxLength(VisitorIdentity.IdMaxLength)] string visitorId,
        CancellationToken cancellationToken)
    {
        var result = await savedPropertyService.SaveAsync(propertyId, visitorId, cancellationToken);
        return result.IsSuccess ? NoContent() : ToProblem(result.Error);
    }

    /// <summary>
    /// Tira o imóvel dos salvos. Remover um imóvel que não está salvo não muda nada.
    /// </summary>
    [HttpDelete("{propertyId:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.SavedProperties)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Remove(
        Guid propertyId,
        [FromHeader(Name = VisitorHeaders.Id), Required, MinLength(VisitorIdentity.IdMinLength), MaxLength(VisitorIdentity.IdMaxLength)] string visitorId,
        CancellationToken cancellationToken)
    {
        await savedPropertyService.RemoveAsync(propertyId, visitorId, cancellationToken);
        return NoContent();
    }
}
