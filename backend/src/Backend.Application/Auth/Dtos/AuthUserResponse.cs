using Backend.Domain.Users;

namespace Backend.Application.Auth.Dtos;

public sealed record AuthUserResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    IReadOnlyList<string> Permissions);
