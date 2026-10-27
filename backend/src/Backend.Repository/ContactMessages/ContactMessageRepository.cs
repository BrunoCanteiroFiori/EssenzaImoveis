using Backend.Domain.Common;
using Backend.Domain.ContactMessages;
using Backend.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.ContactMessages;

public sealed class ContactMessageRepository(AppDbContext dbContext) : RepositoryBase<ContactMessage>(dbContext), IContactMessageRepository
{
    public async Task<PagedList<ContactMessage>> SearchAsync(
        ContactMessageStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Entities.AsNoTracking();
        if (status is { } value)
        {
            query = query.Where(contactMessage => contactMessage.Status == value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(contactMessage => contactMessage.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<ContactMessage>(items, totalCount, page, pageSize);
    }

    public async Task<int> CountByStatusAsync(ContactMessageStatus status, CancellationToken cancellationToken = default) =>
        await Entities.CountAsync(contactMessage => contactMessage.Status == status, cancellationToken);
}
