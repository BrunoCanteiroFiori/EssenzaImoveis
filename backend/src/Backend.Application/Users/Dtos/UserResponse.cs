using Backend.Domain.Users;

namespace Backend.Application.Users.Dtos;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    IReadOnlyList<string> Permissions,
    bool IsActive,
    DateTimeOffset? LastSignInAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
