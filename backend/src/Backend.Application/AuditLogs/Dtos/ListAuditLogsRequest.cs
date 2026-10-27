using System.ComponentModel.DataAnnotations;
using Backend.Domain.AuditLogs;

namespace Backend.Application.AuditLogs.Dtos;

public sealed record ListAuditLogsRequest(
    [MaxLength(AuditLog.EntityNameMaxLength)] string? EntityName = null,
    [MaxLength(AuditLog.EntityIdMaxLength)] string? EntityId = null,
    AuditAction? Action = null,
    Guid? UserId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    [Range(1, 10_000)] int Page = 1,
    [Range(1, 100)] int PageSize = 50);
