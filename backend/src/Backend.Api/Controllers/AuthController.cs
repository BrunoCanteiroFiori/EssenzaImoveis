using Backend.Api.Authentication;
using Backend.Api.RateLimiting;
using Backend.Application.Auth;
using Backend.Application.Auth.Dtos;
using Backend.Application.Users;
using Backend.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Api.Controllers;

/// <summary>
/// Login da área administrativa. Os tokens trafegam só em cookies httpOnly; o corpo traz apenas os dados do usuário.
/// </summary>
[Route("auth")]
public sealed class AuthController(
    IAuthService authService,
    IUserPasswordService userPasswordService,
    AuthCookies authCookies) : ApiControllerBase
{
    /// <summary>
    /// Entra com e-mail e senha. Após 5 senhas erradas a conta fica bloqueada por 15 minutos.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<AuthUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthUserResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        authCookies.Write(Response, result.Value);
        return Ok(result.Value.User);
    }

    /// <summary>
    /// Renova a sessão com o refresh token do cookie (rotação: o token antigo deixa de valer).
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Refresh)]
    [ProducesResponseType<AuthUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthUserResponse>> Refresh(CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(AuthCookies.ReadRefreshToken(Request), cancellationToken);
        if (result.IsFailure)
        {
            authCookies.Clear(Response);
            return ToProblem(result.Error);
        }

        authCookies.Write(Response, result.Value);
        return Ok(result.Value.User);
    }

    /// <summary>
    /// Sai da área administrativa: revoga o refresh token e apaga os cookies.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(AuthCookies.ReadRefreshToken(Request), cancellationToken);
        authCookies.Clear(Response);
        return NoContent();
    }

    /// <summary>
    /// Dados do usuário logado e suas permissões.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<AuthUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserResponse>> Me(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
        {
            return Unauthorized();
        }

        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Troca a senha do usuário logado. Todas as sessões são encerradas e é preciso entrar de novo.
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(ChangeOwnPasswordRequest request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
        {
            return Unauthorized();
        }

        var result = await userPasswordService.ChangeOwnPasswordAsync(userId, request, cancellationToken);
        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        await authService.LogoutAsync(AuthCookies.ReadRefreshToken(Request), cancellationToken);
        authCookies.Clear(Response);
        return NoContent();
    }
}
