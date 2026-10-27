using Backend.Application.Dashboard;
using Backend.Application.Dashboard.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>
/// Resumo da área administrativa.
/// </summary>
[Route("dashboard")]
public sealed class DashboardController(IDashboardService dashboardService) : ApiControllerBase
{
    /// <summary>
    /// Totais de imóveis por status, patrocinados ativos, mensagens novas, visualizações recentes e imóveis mais quentes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<DashboardSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}
