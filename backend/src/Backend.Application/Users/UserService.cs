using Backend.Application.Common;
using Backend.Application.Users.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Common;
using Backend.Domain.Users;
using Backend.Infrastructure.Security;

namespace Backend.Application.Users;

public sealed class UserService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IUserService
{
    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return
        [
            .. users
                .OrderByDescending(user => user.IsSuperAdmin)
                .ThenBy(user => user.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(user => user.ToResponse()),
        ];
    }

    public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? UserErrors.NotFound(id) : user.ToResponse();
    }

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!UserPermissionNames.TryParse(request.Permissions, out var permissions))
        {
            return UserErrors.InvalidPermissions();
        }

        User user;
        try
        {
            PasswordPolicy.EnsureIsValid(request.Password);
            var email = EmailAddress.Normalize(request.Email);
            if (await userRepository.ExistsByEmailAsync(email, cancellationToken: cancellationToken))
            {
                return UserErrors.EmailInUse(email);
            }

            user = User.Create(request.Name, email, passwordHasher.Hash(request.Password), permissions, timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return UserErrors.Invalid(exception.Message);
        }

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }

    public async Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!UserPermissionNames.TryParse(request.Permissions, out var permissions))
        {
            return UserErrors.InvalidPermissions();
        }

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(id);
        }

        var now = timeProvider.GetUtcNow();
        try
        {
            var email = EmailAddress.Normalize(request.Email);
            if (await userRepository.ExistsByEmailAsync(email, id, cancellationToken))
            {
                return UserErrors.EmailInUse(email);
            }

            user.Update(request.Name, email, now);
            if (!user.IsSuperAdmin)
            {
                user.ChangePermissions(permissions, now);
            }
        }
        catch (DomainException exception)
        {
            return UserErrors.Invalid(exception.Message);
        }

        return await SaveAsync(user, cancellationToken);
    }

    public Task<Result<UserResponse>> ActivateAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, (user, now) => user.Activate(now), cancellationToken);

    public Task<Result<UserResponse>> DeactivateAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, (user, now) => user.Deactivate(now), cancellationToken);

    private async Task<Result<UserResponse>> ChangeAsync(Guid id, Action<User, DateTimeOffset> change, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(id);
        }

        try
        {
            change(user, timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return UserErrors.Invalid(exception.Message);
        }

        return await SaveAsync(user, cancellationToken);
    }

    private async Task<Result<UserResponse>> SaveAsync(User user, CancellationToken cancellationToken)
    {
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }
}
