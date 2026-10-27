using Backend.Application.Common;

namespace Backend.Application.Users;

public interface ISuperAdminService
{
    /// <summary>
    /// Cria a conta fixa de super admin se ela ainda não existir. Se já existir, não altera nada.
    /// </summary>
    Task<Result> EnsureCreatedAsync(string name, string email, string password, CancellationToken cancellationToken = default);
}
