using Backend.Application.Common;
using Backend.Domain.Auth;

namespace Backend.Application.Auth;

public interface IRefreshSessionService
{
    Task<IssuedRefreshToken> CreateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca a sessão ativa do token. Se o token já tinha sido revogado (reuso suspeito), revoga todas as sessões do usuário.
    /// </summary>
    Task<Result<RefreshSession>> ValidateAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga a sessão atual e cria uma nova (rotação do refresh token).
    /// </summary>
    Task<IssuedRefreshToken> RotateAsync(RefreshSession session, CancellationToken cancellationToken = default);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
