using System.Security.Claims;
using Backend.Application.Auth;
using Backend.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>
/// Base dos controllers da API. Converte um <see cref="Error"/> da camada de aplicação em ProblemDetails.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Id do usuário autenticado (claim <c>sub</c> do JWT), ou nulo se não houver.
    /// </summary>
    protected Guid? CurrentUserId => Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var userId) ? userId : null;

    protected ObjectResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest,
        };

        return Problem(title: error.Code, detail: error.Message, statusCode: statusCode);
    }
}
