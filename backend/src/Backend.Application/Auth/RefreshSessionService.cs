using Backend.Application.Common;
using Backend.Domain.Abstractions;
using Backend.Domain.Auth;
using Backend.Infrastructure.Conversions;
using Backend.Infrastructure.Security;

namespace Backend.Application.Auth;

public sealed class RefreshSessionService(
    IRefreshSessionRepository refreshSessionRepository,
    IUnitOfWork unitOfWork,
    ISecureTokenGenerator secureTokenGenerator,
    TimeProvider timeProvider) : IRefreshSessionService
{
    public async Task<IssuedRefreshToken> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var issued = await AddSessionAsync(userId, timeProvider.GetUtcNow(), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return issued;
    }

    public async Task<Result<RefreshSession>> ValidateAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var session = await refreshSessionRepository.GetByTokenHashAsync(refreshToken.ToSha256Hex(), cancellationToken);
        if (session is null)
        {
            return AuthErrors.InvalidSession();
        }

        var now = timeProvider.GetUtcNow();
        if (session.IsSuspiciousReuse(now))
        {
            var activeSessions = await refreshSessionRepository.GetActiveByUserIdAsync(session.UserId, now, cancellationToken);
            foreach (var activeSession in activeSessions)
            {
                activeSession.Revoke(now);
                refreshSessionRepository.Update(activeSession);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthErrors.InvalidSession();
        }

        return session.IsActive(now) ? session : AuthErrors.InvalidSession();
    }

    public async Task<IssuedRefreshToken> RotateAsync(RefreshSession session, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        session.Revoke(now);
        refreshSessionRepository.Update(session);

        var issued = await AddSessionAsync(session.UserId, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return issued;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var session = await refreshSessionRepository.GetByTokenHashAsync(refreshToken.ToSha256Hex(), cancellationToken);
        if (session is null || session.IsRevoked)
        {
            return;
        }

        session.Revoke(timeProvider.GetUtcNow());
        refreshSessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<IssuedRefreshToken> AddSessionAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var token = secureTokenGenerator.Generate();
        var session = RefreshSession.Create(userId, token.ToSha256Hex(), now);
        await refreshSessionRepository.AddAsync(session, cancellationToken);
        return new IssuedRefreshToken(token, session.ExpiresAt);
    }
}
