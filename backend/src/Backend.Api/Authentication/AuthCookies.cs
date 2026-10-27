using Backend.Application.Auth.Dtos;

namespace Backend.Api.Authentication;

/// <summary>
/// Cookies da sessão: httpOnly (inacessíveis ao JavaScript), SameSite=Strict (proteção contra CSRF) e Secure fora de
/// desenvolvimento. O refresh token só é enviado para as rotas de <c>/auth</c>.
/// </summary>
public sealed class AuthCookies(bool secure)
{
    public const string AccessTokenName = "access_token";
    public const string RefreshTokenName = "refresh_token";
    public const string RefreshTokenPath = "/auth";

    public static string? ReadRefreshToken(HttpRequest request) => request.Cookies[RefreshTokenName];

    public void Write(HttpResponse response, AuthSessionResponse session)
    {
        response.Cookies.Append(AccessTokenName, session.AccessToken, CreateOptions("/", session.AccessTokenExpiresAt));
        response.Cookies.Append(RefreshTokenName, session.RefreshToken, CreateOptions(RefreshTokenPath, session.RefreshTokenExpiresAt));
    }

    public void Clear(HttpResponse response)
    {
        response.Cookies.Delete(AccessTokenName, CreateOptions("/", expires: null));
        response.Cookies.Delete(RefreshTokenName, CreateOptions(RefreshTokenPath, expires: null));
    }

    private CookieOptions CreateOptions(string path, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Strict,
        Path = path,
        Expires = expires,
        IsEssential = true,
    };
}
