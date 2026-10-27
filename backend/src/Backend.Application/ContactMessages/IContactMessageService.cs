using Backend.Application.Common;
using Backend.Application.Common.Dtos;
using Backend.Application.ContactMessages.Dtos;

namespace Backend.Application.ContactMessages;

public interface IContactMessageService
{
    Task<Result> CreateAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<ContactMessageResponse>> ListAsync(ListContactMessagesRequest request, CancellationToken cancellationToken = default);

    Task<Result<ContactMessageResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ContactMessageResponse>> UpdateAsync(Guid id, UpdateContactMessageRequest request, CancellationToken cancellationToken = default);
}
