using Backend.Api.Authorization;
using Backend.Application.AuditLogs;
using Backend.Application.AuditLogs.Dtos;
using Backend.Application.Common.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>
/// Auditoria de inclusões, alterações e exclusões. Somente leitura e somente o super admin.
/// </summary>
[Route("audit-logs")]
[Authorize(Policy = Policies.SuperAdmin)]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ApiControllerBase
{
    /// <summary>
    /// Lista os registros de auditoria, dos mais recentes para os mais antigos.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<AuditLogResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> List(
        [FromQuery] ListAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var auditLogs = await auditLogService.ListAsync(request, cancellationToken);
        return Ok(auditLogs);
    }

    /// <summary>
    /// Busca um registro de auditoria pelo id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AuditLogResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await auditLogService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }
}
