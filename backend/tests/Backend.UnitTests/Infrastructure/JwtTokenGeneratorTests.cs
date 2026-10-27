using System.Security.Claims;
using Backend.Infrastructure.Security;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Backend.UnitTests.Infrastructure;

public sealed class JwtTokenGeneratorTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly JwtOptions _options = new("emissor", "publico", new string('s', JwtOptions.MinSecretBytes), TimeSpan.FromMinutes(15));

    [Fact]
    public void Generate_WithClaims_ShouldCreateSignedTokenWithExpiration()
    {
        var generator = new JwtTokenGenerator(_options, new FakeTimeProvider(_now));

        var token = generator.Generate([new Claim("sub", "123"), new Claim("permission", "A"), new Claim("permission", "B")]);

        var jwt = new JsonWebToken(token.Value);
        Assert.Equal(_now.AddMinutes(15), token.ExpiresAt);
        Assert.Equal("emissor", jwt.Issuer);
        Assert.Equal("123", jwt.Subject);
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Alg);
        Assert.Equal(["A", "B"], jwt.Claims.Where(claim => claim.Type == "permission").Select(claim => claim.Value));
    }

    [Fact]
    public void CreateSigningKey_WithShortSecret_ShouldThrowInvalidOperationException()
    {
        var options = _options with { Secret = "curto" };

        Assert.Throws<InvalidOperationException>(options.CreateSigningKey);
    }
}
