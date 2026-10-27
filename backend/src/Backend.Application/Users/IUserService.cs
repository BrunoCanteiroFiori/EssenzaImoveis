using Backend.Application.Common;
using Backend.Application.Users.Dtos;

namespace Backend.Application.Users;

/// <summary>
/// Gestão de usuários pelo super admin.
/// </summary>
public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
