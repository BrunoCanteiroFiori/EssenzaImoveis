using Backend.Application.Common;
using Backend.Application.Users.Dtos;

namespace Backend.Application.Users;

/// <summary>
/// Troca de senha. Depois da troca, as sessões abertas antes dela deixam de renovar o login.
/// </summary>
public interface IUserPasswordService
{
    Task<Result> ChangeOwnPasswordAsync(Guid userId, ChangeOwnPasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(Guid userId, ResetUserPasswordRequest request, CancellationToken cancellationToken = default);
}
