using Backend.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Common;

public abstract class RepositoryBase<TEntity>(AppDbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    protected DbSet<TEntity> Entities { get; } = dbContext.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Entities.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Entities.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Entities.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) => Entities.Update(entity);

    public void Remove(TEntity entity) => Entities.Remove(entity);
}
