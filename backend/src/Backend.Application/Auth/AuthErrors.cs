using Backend.Application.Common;

namespace Backend.Application.Auth;

public static class AuthErrors
{
    public static Error InvalidCredentials() => Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha inválidos.");

    public static Error LockedOut() =>
        Error.Unauthorized("Auth.LockedOut", "Acesso bloqueado temporariamente por excesso de tentativas. Tente novamente em alguns minutos.");

    public static Error InvalidSession() => Error.Unauthorized("Auth.InvalidSession", "Sessão expirada. Faça login novamente.");
}
