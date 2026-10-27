using System.Security.Claims;

namespace Backend.Infrastructure.Security;

public interface IJwtTokenGenerator
{
    JwtToken Generate(IEnumerable<Claim> claims);
}
