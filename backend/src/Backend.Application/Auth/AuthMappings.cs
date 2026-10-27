using System.Security.Claims;
using Backend.Application.Auth.Dtos;
using Backend.Application.Users;
using Backend.Domain.Users;

namespace Backend.Application.Auth;

public static class AuthMappings
{
    public static AuthUserResponse ToAuthUserResponse(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.Permissions.ToNames());

    public static IEnumerable<Claim> ToClaims(this User user)
    {
        yield return new Claim(AuthClaimTypes.Subject, user.Id.ToString());
        yield return new Claim(AuthClaimTypes.Name, user.Name);
        yield return new Claim(AuthClaimTypes.Email, user.Email);
        yield return new Claim(AuthClaimTypes.Role, user.Role.ToString());

        foreach (var permission in user.Permissions.ToNames())
        {
            yield return new Claim(AuthClaimTypes.Permission, permission);
        }
    }
}
