namespace Backend.Application.Auth;

/// <summary>
/// Refresh token em texto (só existe na resposta para o navegador; o banco guarda apenas o hash).
/// </summary>
public sealed record IssuedRefreshToken(string Token, DateTimeOffset ExpiresAt);
