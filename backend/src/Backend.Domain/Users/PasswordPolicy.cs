using Backend.Domain.Common;

namespace Backend.Domain.Users;

/// <summary>
/// Regra de senha: entre <see cref="MinLength"/> e <see cref="MaxLength"/> caracteres, com pelo menos uma letra e um número.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;
    public const int MaxLength = 128;

    public static void EnsureIsValid(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength || password.Length > MaxLength)
        {
            throw new DomainException($"A senha deve ter entre {MinLength} e {MaxLength} caracteres.");
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new DomainException("A senha deve ter pelo menos uma letra e um número.");
        }
    }
}
