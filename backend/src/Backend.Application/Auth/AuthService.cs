using Backend.Application.Auth.Dtos;
using Backend.Application.Common;
using Backend.Domain.Users;
using Backend.Infrastructure.Security;

namespace Backend.Application.Auth;

public sealed class AuthService(
    IUserCredentialService userCredentialService,
    IRefreshSessionService refreshSessionService,
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator) : IAuthService
{
    public async Task<Result<AuthSessionResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var credentials = await userCredentialService.ValidateAsync(request, cancellationToken);
        if (credentials.IsFailure)
        {
            return credentials.Error;
        }

        var refreshToken = await refreshSessionService.CreateAsync(credentials.Value.Id, cancellationToken);
        return CreateSession(credentials.Value, refreshToken);
    }

    public async Task<Result<AuthSessionResponse>> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthErrors.InvalidSession();
        }

        var session = await refreshSessionService.ValidateAsync(refreshToken, cancellationToken);
        if (session.IsFailure)
        {
            return session.Error;
        }

        var user = await userRepository.GetByIdAsync(session.Value.UserId, cancellationToken);
        if (user is null || !user.AcceptsSessionCreatedAt(session.Value.CreatedAt))
        {
            await refreshSessionService.RevokeAsync(refreshToken, cancellationToken);
            return AuthErrors.InvalidSession();
        }

        var rotated = await refreshSessionService.RotateAsync(session.Value, cancellationToken);
        return CreateSession(user, rotated);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await refreshSessionService.RevokeAsync(refreshToken, cancellationToken);
        }
    }

    public async Task<Result<AuthUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null || !user.IsActive ? AuthErrors.InvalidSession() : user.ToAuthUserResponse();
    }

    private AuthSessionResponse CreateSession(User user, IssuedRefreshToken refreshToken)
    {
        var accessToken = jwtTokenGenerator.Generate(user.ToClaims());
        return new AuthSessionResponse(
            user.ToAuthUserResponse(),
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt);
    }
}
