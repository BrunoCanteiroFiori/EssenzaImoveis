using System.ComponentModel.DataAnnotations;
using Backend.Domain.ContactMessages;

namespace Backend.Application.ContactMessages.Dtos;

public sealed record ListContactMessagesRequest(
    ContactMessageStatus? Status = null,
    [Range(1, 10_000)] int Page = 1,
    [Range(1, 100)] int PageSize = 20);
