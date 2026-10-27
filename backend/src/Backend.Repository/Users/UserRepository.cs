using Backend.Domain.Users;
using Backend.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Users;

public sealed class UserRepository(AppDbContext dbContext) : RepositoryBase<User>(dbContext), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await Entities.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, Guid? exceptId = null, CancellationToken cancellationToken = default) =>
        await Entities.AnyAsync(user => user.Email == email && (exceptId == null || user.Id != exceptId), cancellationToken);

    public async Task<bool> ExistsSuperAdminAsync(CancellationToken cancellationToken = default) =>
        await Entities.AnyAsync(user => user.Role == UserRole.SuperAdmin, cancellationToken);
}
