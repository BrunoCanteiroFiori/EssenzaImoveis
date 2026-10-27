using System.Security.Claims;
using Backend.Application.Auth;
using Backend.Domain.Abstractions;

namespace Backend.Api.Authentication;

/// <summary>
/// Usuário autenticado da requisição HTTP atual, lido das claims do JWT. Usado pela auditoria.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(AuthClaimTypes.Subject), out var userId) ? userId : null;

    public string? Email => Principal?.FindFirstValue(AuthClaimTypes.Email);

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
}
