using Backend.Domain.Users;

namespace Backend.Application.Users;

/// <summary>
/// Nomes das permissões usados na API (DTOs, claims do JWT e políticas de autorização).
/// </summary>
public static class UserPermissionNames
{
    public const string ReadProperties = nameof(UserPermissions.ReadProperties);
    public const string WriteProperties = nameof(UserPermissions.WriteProperties);
    public const string DeleteProperties = nameof(UserPermissions.DeleteProperties);
    public const string ManageMessages = nameof(UserPermissions.ManageMessages);

    private static readonly UserPermissions[] _singlePermissions =
    [
        UserPermissions.ReadProperties,
        UserPermissions.WriteProperties,
        UserPermissions.DeleteProperties,
        UserPermissions.ManageMessages,
    ];

    public static IReadOnlyList<string> ToNames(this UserPermissions permissions) =>
        [.. _singlePermissions.Where(permission => permissions.HasFlag(permission)).Select(permission => permission.ToString())];

    /// <summary>
    /// Converte uma lista de nomes em permissões. Retorna falso se algum nome não existir.
    /// </summary>
    public static bool TryParse(IEnumerable<string> names, out UserPermissions permissions)
    {
        permissions = UserPermissions.None;
        foreach (var name in names)
        {
            var permission = _singlePermissions.FirstOrDefault(single => string.Equals(single.ToString(), name, StringComparison.OrdinalIgnoreCase));
            if (permission == UserPermissions.None)
            {
                return false;
            }

            permissions |= permission;
        }

        return true;
    }
}
