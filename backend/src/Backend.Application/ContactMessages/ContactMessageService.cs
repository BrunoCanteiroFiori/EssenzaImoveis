using Backend.Application.Common;
using Backend.Application.Common.Dtos;
using Backend.Application.ContactMessages.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Common;
using Backend.Domain.ContactMessages;
using Backend.Domain.Properties;

namespace Backend.Application.ContactMessages;

public sealed class ContactMessageService(
    IContactMessageRepository contactMessageRepository,
    IPropertyRepository propertyRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IContactMessageService
{
    public async Task<Result> CreateAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(request.Website))
        {
            return Result.Success();
        }

        if (request.PropertyId is { } propertyId)
        {
            var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken);
            if (property is null || !property.IsPubliclyVisible)
            {
                return ContactMessageErrors.PropertyNotFound(propertyId);
            }
        }

        ContactMessage contactMessage;
        try
        {
            contactMessage = ContactMessage.Create(
                request.PropertyId,
                request.Name,
                request.Email,
                request.Phone,
                request.Message,
                timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return ContactMessageErrors.Invalid(exception.Message);
        }

        await contactMessageRepository.AddAsync(contactMessage, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<PagedResponse<ContactMessageResponse>> ListAsync(
        ListContactMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = await contactMessageRepository.SearchAsync(request.Status, request.Page, request.PageSize, cancellationToken);
        var titles = await GetPropertyTitlesAsync(page.Items, cancellationToken);
        return page.ToResponse(contactMessage => contactMessage.ToResponse(TitleOf(contactMessage, titles)));
    }

    public async Task<Result<ContactMessageResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contactMessage = await contactMessageRepository.GetByIdAsync(id, cancellationToken);
        if (contactMessage is null)
        {
            return ContactMessageErrors.NotFound(id);
        }

        var titles = await GetPropertyTitlesAsync([contactMessage], cancellationToken);
        return contactMessage.ToResponse(TitleOf(contactMessage, titles));
    }

    public async Task<Result<ContactMessageResponse>> UpdateAsync(
        Guid id,
        UpdateContactMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var contactMessage = await contactMessageRepository.GetByIdAsync(id, cancellationToken);
        if (contactMessage is null)
        {
            return ContactMessageErrors.NotFound(id);
        }

        var now = timeProvider.GetUtcNow();
        try
        {
            contactMessage.ChangeStatus(request.Status, now);
            contactMessage.UpdateInternalNotes(request.InternalNotes, now);
        }
        catch (DomainException exception)
        {
            return ContactMessageErrors.Invalid(exception.Message);
        }

        contactMessageRepository.Update(contactMessage);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var titles = await GetPropertyTitlesAsync([contactMessage], cancellationToken);
        return contactMessage.ToResponse(TitleOf(contactMessage, titles));
    }

    private static string? TitleOf(ContactMessage contactMessage, IReadOnlyDictionary<Guid, string> titles) =>
        contactMessage.PropertyId is { } propertyId ? titles.GetValueOrDefault(propertyId) : null;

    private async Task<IReadOnlyDictionary<Guid, string>> GetPropertyTitlesAsync(
        IEnumerable<ContactMessage> contactMessages,
        CancellationToken cancellationToken)
    {
        var propertyIds = contactMessages
            .Where(contactMessage => contactMessage.PropertyId is not null)
            .Select(contactMessage => contactMessage.PropertyId!.Value)
            .Distinct()
            .ToList();

        if (propertyIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await propertyRepository.GetTitlesAsync(propertyIds, cancellationToken);
    }
}
