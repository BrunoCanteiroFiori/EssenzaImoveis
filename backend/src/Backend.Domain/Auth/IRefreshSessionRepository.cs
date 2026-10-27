using Backend.Domain.Abstractions;

namespace Backend.Domain.Auth;

public interface IRefreshSessionRepository : IRepository<RefreshSession>
{
    Task<RefreshSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshSession>> GetActiveByUserIdAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
