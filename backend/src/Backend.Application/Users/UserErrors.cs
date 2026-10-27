using Backend.Application.Common;

namespace Backend.Application.Users;

public static class UserErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("User.NotFound", $"Usuário com id '{id}' não foi encontrado.");

    public static Error Invalid(string message) => Error.Validation("User.Invalid", message);

    public static Error InvalidPermissions() => Error.Validation("User.InvalidPermissions", "Uma ou mais permissões informadas não existem.");

    public static Error EmailInUse(string email) => Error.Conflict("User.EmailInUse", $"Já existe um usuário com o e-mail '{email}'.");

    public static Error WrongCurrentPassword() => Error.Validation("User.WrongCurrentPassword", "A senha atual está incorreta.");
}
