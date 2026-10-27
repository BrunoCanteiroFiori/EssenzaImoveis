using Backend.Application.Auth.Dtos;
using Backend.Application.Common;

namespace Backend.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthSessionResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuthSessionResponse>> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default);

    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

    Task<Result<AuthUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
