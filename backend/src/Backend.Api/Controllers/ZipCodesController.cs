using Backend.Api.Authorization;
using Backend.Api.RateLimiting;
using Backend.Application.ZipCodes;
using Backend.Application.ZipCodes.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Api.Controllers;

/// <summary>
/// Consulta de CEP para preencher o endereço no cadastro de imóveis.
/// </summary>
[Route("zip-codes")]
public sealed class ZipCodesController(IZipCodeService zipCodeService) : ApiControllerBase
{
    /// <summary>
    /// Busca logradouro, bairro, cidade e UF de um CEP (com ou sem máscara), com o texto já normalizado.
    /// Logradouro e bairro voltam nulos em CEP geral de cidade.
    /// </summary>
    [HttpGet("{zipCode}")]
    [Authorize(Policy = Policies.WriteProperties)]
    [EnableRateLimiting(RateLimitPolicies.ZipCodes)]
    [ProducesResponseType<ZipCodeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ZipCodeResponse>> GetAddress(string zipCode, CancellationToken cancellationToken)
    {
        var result = await zipCodeService.GetAddressAsync(zipCode, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }
}
