using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Infrastructure.Security;

public sealed class JwtTokenGenerator(JwtOptions options, TimeProvider timeProvider) : IJwtTokenGenerator
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public JwtToken Generate(IEnumerable<Claim> claims)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(options.AccessTokenLifetime);

        var token = _tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(options.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });

        return new JwtToken(token, expiresAt);
    }
}
