using Backend.Application.Common;
using Backend.Domain.Abstractions;
using Backend.Domain.Common;
using Backend.Domain.Users;
using Backend.Infrastructure.Security;

namespace Backend.Application.Users;

public sealed class SuperAdminService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : ISuperAdminService
{
    public async Task<Result> EnsureCreatedAsync(string name, string email, string password, CancellationToken cancellationToken = default)
    {
        if (await userRepository.ExistsSuperAdminAsync(cancellationToken))
        {
            return Result.Success();
        }

        User superAdmin;
        try
        {
            PasswordPolicy.EnsureIsValid(password);
            var normalizedEmail = EmailAddress.Normalize(email);
            if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken: cancellationToken))
            {
                return UserErrors.EmailInUse(normalizedEmail);
            }

            superAdmin = User.CreateSuperAdmin(name, normalizedEmail, passwordHasher.Hash(password), timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return UserErrors.Invalid(exception.Message);
        }

        await userRepository.AddAsync(superAdmin, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
