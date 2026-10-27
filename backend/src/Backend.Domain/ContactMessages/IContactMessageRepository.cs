using Backend.Domain.Abstractions;
using Backend.Domain.Common;

namespace Backend.Domain.ContactMessages;

public interface IContactMessageRepository : IRepository<ContactMessage>
{
    Task<PagedList<ContactMessage>> SearchAsync(
        ContactMessageStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(ContactMessageStatus status, CancellationToken cancellationToken = default);
}
