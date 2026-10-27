namespace Backend.Application.Auth.Dtos;

/// <summary>
/// Sessão emitida no login ou na renovação. Os tokens vão em cookies httpOnly; só <see cref="User"/> vai no corpo da resposta.
/// </summary>
public sealed record AuthSessionResponse(
    AuthUserResponse User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
