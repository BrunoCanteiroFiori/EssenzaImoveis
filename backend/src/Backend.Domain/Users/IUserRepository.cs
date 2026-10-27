using Backend.Domain.Abstractions;

namespace Backend.Domain.Users;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, Guid? exceptId = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsSuperAdminAsync(CancellationToken cancellationToken = default);
}
