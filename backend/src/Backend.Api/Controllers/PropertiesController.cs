using Backend.Api.Authorization;
using Backend.Api.RateLimiting;
using Backend.Application.Common.Dtos;
using Backend.Application.Properties;
using Backend.Application.Properties.Dtos;
using Backend.Application.PropertyViews;
using Backend.Application.PropertyViews.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Api.Controllers;

/// <summary>
/// Imóveis: rotas públicas (busca, destaques, detalhe, visualização) e de gestão (exigem permissão).
/// </summary>
[Route("properties")]
public sealed class PropertiesController(
    IPropertyService propertyService,
    IPropertySearchService propertySearchService,
    IPropertyViewService propertyViewService) : ApiControllerBase
{
    /// <summary>
    /// Busca pública de imóveis disponíveis por localidade, tipo, finalidade, quartos, vagas, metragem e valor.
    /// A ordenação por relevância usa o algoritmo de recomendação.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResponse<PropertySummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<PropertySummaryResponse>>> Search(
        [FromQuery] SearchPropertiesRequest request,
        CancellationToken cancellationToken)
    {
        var properties = await propertySearchService.SearchAsync(request, cancellationToken);
        return Ok(properties);
    }

    /// <summary>
    /// Vitrine da página inicial: destaques, compra, aluguel, mais buscados e mais recentes.
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType<FeaturedPropertiesResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FeaturedPropertiesResponse>> GetFeatured(CancellationToken cancellationToken)
    {
        var featured = await propertySearchService.GetFeaturedAsync(cancellationToken);
        return Ok(featured);
    }

    /// <summary>
    /// Bairros, cidades e UFs com imóveis disponíveis (para sugestões na busca).
    /// </summary>
    [HttpGet("locations")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<PropertyLocationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PropertyLocationResponse>>> GetLocations(CancellationToken cancellationToken)
    {
        var locations = await propertySearchService.GetLocationsAsync(cancellationToken);
        return Ok(locations);
    }

    /// <summary>
    /// Detalhe público de um imóvel disponível.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<PropertyDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDetailsResponse>> GetDetails(Guid id, CancellationToken cancellationToken)
    {
        var result = await propertySearchService.GetDetailsAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Registra o acesso de um visitante ao imóvel (esquenta o imóvel). Conta no máximo uma vez por visitante por dia.
    /// </summary>
    [HttpPost("{id:guid}/views")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PropertyViews)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RegisterView(Guid id, RegisterPropertyViewRequest request, CancellationToken cancellationToken)
    {
        var result = await propertyViewService.RegisterAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToProblem(result.Error);
    }

    /// <summary>
    /// Lista os imóveis de todos os status para a gestão.
    /// </summary>
    [HttpGet("manage")]
    [Authorize(Policy = Policies.ReadProperties)]
    [ProducesResponseType<PagedResponse<PropertyManagementSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<PropertyManagementSummaryResponse>>> List(
        [FromQuery] ListPropertiesRequest request,
        CancellationToken cancellationToken)
    {
        var properties = await propertySearchService.ListAsync(request, cancellationToken);
        return Ok(properties);
    }

    /// <summary>
    /// Busca um imóvel completo (qualquer status) para a gestão.
    /// </summary>
    [HttpGet("manage/{id:guid}")]
    [Authorize(Policy = Policies.ReadProperties)]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await propertyService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Cadastra um imóvel (status inicial: disponível).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.WriteProperties)]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PropertyResponse>> Create(CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        var result = await propertyService.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : ToProblem(result.Error);
    }

    /// <summary>
    /// Atualiza os dados de um imóvel.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.WriteProperties)]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponse>> Update(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        var result = await propertyService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Altera o status (disponível, reservado, vendido, alugado ou inativo).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Policies.WriteProperties)]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponse>> ChangeStatus(Guid id, ChangePropertyStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await propertyService.ChangeStatusAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Patrocina o imóvel por alguns dias: ele aparece em destaque e primeiro na busca.
    /// </summary>
    [HttpPut("{id:guid}/sponsorship")]
    [Authorize(Policy = Policies.WriteProperties)]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponse>> Sponsor(Guid id, SponsorPropertyRequest request, CancellationToken cancellationToken)
    {
        var result = await propertyService.SponsorAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Encerra o patrocínio do imóvel.
    /// </summary>
    [HttpDelete("{id:guid}/sponsorship")]
    [Authorize(Policy = Policies.WriteProperties)]
    [ProducesResponseType<PropertyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponse>> RemoveSponsorship(Guid id, CancellationToken cancellationToken)
    {
        var result = await propertyService.RemoveSponsorshipAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Remove o imóvel e suas imagens.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.DeleteProperties)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await propertyService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToProblem(result.Error);
    }
}
