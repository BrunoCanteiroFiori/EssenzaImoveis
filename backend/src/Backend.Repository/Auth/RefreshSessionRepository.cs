using Backend.Domain.Auth;
using Backend.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Auth;

public sealed class RefreshSessionRepository(AppDbContext dbContext) : RepositoryBase<RefreshSession>(dbContext), IRefreshSessionRepository
{
    public async Task<RefreshSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await Entities.FirstOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshSession>> GetActiveByUserIdAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var nowUtc = now.ToUniversalTime();
        return await Entities
            .Where(session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > nowUtc)
            .ToListAsync(cancellationToken);
    }
}
