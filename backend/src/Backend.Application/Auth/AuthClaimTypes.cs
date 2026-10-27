namespace Backend.Application.Auth;

/// <summary>
/// Nomes curtos das claims do JWT (padrão RFC 7519 quando existe).
/// </summary>
public static class AuthClaimTypes
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";
    public const string Permission = "permission";
}
