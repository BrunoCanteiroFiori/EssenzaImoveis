using Backend.Api.Authorization;
using Backend.Api.RateLimiting;
using Backend.Application.Common.Dtos;
using Backend.Application.ContactMessages;
using Backend.Application.ContactMessages.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Api.Controllers;

/// <summary>
/// Mensagens diretas do site para retorno manual da corretora.
/// </summary>
[Route("contact-messages")]
public sealed class ContactMessagesController(IContactMessageService contactMessageService) : ApiControllerBase
{
    /// <summary>
    /// Envia uma mensagem pelo site (informar e-mail ou telefone para retorno).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ContactMessages)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create(CreateContactMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await contactMessageService.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? Accepted() : ToProblem(result.Error);
    }

    /// <summary>
    /// Lista as mensagens recebidas, das mais novas para as mais antigas.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.ManageMessages)]
    [ProducesResponseType<PagedResponse<ContactMessageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ContactMessageResponse>>> List(
        [FromQuery] ListContactMessagesRequest request,
        CancellationToken cancellationToken)
    {
        var contactMessages = await contactMessageService.ListAsync(request, cancellationToken);
        return Ok(contactMessages);
    }

    /// <summary>
    /// Busca uma mensagem pelo id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ManageMessages)]
    [ProducesResponseType<ContactMessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactMessageResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await contactMessageService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Atualiza o status do atendimento e as anotações internas.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ManageMessages)]
    [ProducesResponseType<ContactMessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactMessageResponse>> Update(
        Guid id,
        UpdateContactMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await contactMessageService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }
}
