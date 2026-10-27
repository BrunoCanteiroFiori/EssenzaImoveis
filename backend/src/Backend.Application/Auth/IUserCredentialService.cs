using Backend.Application.Auth.Dtos;
using Backend.Application.Common;
using Backend.Domain.Users;

namespace Backend.Application.Auth;

public interface IUserCredentialService
{
    /// <summary>
    /// Confere e-mail e senha, aplicando o bloqueio por tentativas. Retorna o usuário autenticado.
    /// </summary>
    Task<Result<User>> ValidateAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
