using Backend.Application.Common;
using Backend.Application.Users.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Common;
using Backend.Domain.Users;
using Backend.Infrastructure.Security;

namespace Backend.Application.Users;

public sealed class UserPasswordService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IUserPasswordService
{
    public async Task<Result> ChangeOwnPasswordAsync(Guid userId, ChangeOwnPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(userId);
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return UserErrors.WrongCurrentPassword();
        }

        return await ChangePasswordAsync(user, request.NewPassword, cancellationToken);
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, ResetUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(userId);
        }

        return await ChangePasswordAsync(user, request.NewPassword, cancellationToken);
    }

    private async Task<Result> ChangePasswordAsync(User user, string newPassword, CancellationToken cancellationToken)
    {
        try
        {
            PasswordPolicy.EnsureIsValid(newPassword);
            user.ChangePasswordHash(passwordHasher.Hash(newPassword), timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return UserErrors.Invalid(exception.Message);
        }

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
