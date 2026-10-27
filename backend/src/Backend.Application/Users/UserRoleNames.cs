using Backend.Domain.Users;

namespace Backend.Application.Users;

/// <summary>
/// Nomes dos papéis usados na claim <c>role</c> do JWT e nas políticas de autorização.
/// </summary>
public static class UserRoleNames
{
    public const string SuperAdmin = nameof(UserRole.SuperAdmin);
}
