using Backend.Application.Auth.Dtos;
using Backend.Application.Common;
using Backend.Domain.Abstractions;
using Backend.Domain.Users;
using Backend.Infrastructure.Security;

namespace Backend.Application.Auth;

public sealed class UserCredentialService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IUserCredentialService
{
    public async Task<Result<User>> ValidateAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            // Calcula um hash mesmo sem usuário para o tempo de resposta não revelar quais e-mails existem.
            passwordHasher.Hash(request.Password);
            return AuthErrors.InvalidCredentials();
        }

        var now = timeProvider.GetUtcNow();
        if (user.IsLockedOut(now))
        {
            return AuthErrors.LockedOut();
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash) || !user.IsActive)
        {
            user.RegisterFailedSignIn(now);
            await SaveAsync(user, cancellationToken);
            return user.IsLockedOut(now) ? AuthErrors.LockedOut() : AuthErrors.InvalidCredentials();
        }

        user.RegisterSuccessfulSignIn(now);
        await SaveAsync(user, cancellationToken);
        return user;
    }

    private async Task SaveAsync(User user, CancellationToken cancellationToken)
    {
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
