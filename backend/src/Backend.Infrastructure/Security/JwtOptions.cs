using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Infrastructure.Security;

/// <summary>
/// Configuração do JWT. O segredo precisa ter pelo menos <see cref="MinSecretBytes"/> bytes (HMAC-SHA256).
/// </summary>
public sealed record JwtOptions(string Issuer, string Audience, string Secret, TimeSpan AccessTokenLifetime)
{
    public const int MinSecretBytes = 32;

    public SymmetricSecurityKey CreateSigningKey()
    {
        var secretBytes = Encoding.UTF8.GetBytes(Secret);
        if (secretBytes.Length < MinSecretBytes)
        {
            throw new InvalidOperationException($"O segredo do JWT precisa ter pelo menos {MinSecretBytes} bytes.");
        }

        return new SymmetricSecurityKey(secretBytes);
    }
}
