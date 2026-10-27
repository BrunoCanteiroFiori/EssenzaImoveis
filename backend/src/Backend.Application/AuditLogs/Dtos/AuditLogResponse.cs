using System.Text.Json;
using Backend.Domain.AuditLogs;

namespace Backend.Application.AuditLogs.Dtos;

public sealed record AuditLogResponse(
    Guid Id,
    string EntityName,
    string EntityId,
    AuditAction Action,
    JsonElement Changes,
    Guid? UserId,
    string? UserEmail,
    DateTimeOffset OccurredAt);
