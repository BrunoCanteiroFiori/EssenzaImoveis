using Backend.Application.Auth;
using Backend.Application.Users;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Api.Authorization;

/// <summary>
/// Políticas de autorização. Toda rota exige login, exceto as marcadas com <see cref="AllowAnonymousAttribute"/>.
/// </summary>
public static class Policies
{
    public const string ReadProperties = UserPermissionNames.ReadProperties;
    public const string WriteProperties = UserPermissionNames.WriteProperties;
    public const string DeleteProperties = UserPermissionNames.DeleteProperties;
    public const string ManageMessages = UserPermissionNames.ManageMessages;
    public const string SuperAdmin = UserRoleNames.SuperAdmin;

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(ReadProperties, policy => policy.RequireClaim(AuthClaimTypes.Permission, ReadProperties))
            .AddPolicy(WriteProperties, policy => policy.RequireClaim(AuthClaimTypes.Permission, WriteProperties))
            .AddPolicy(DeleteProperties, policy => policy.RequireClaim(AuthClaimTypes.Permission, DeleteProperties))
            .AddPolicy(ManageMessages, policy => policy.RequireClaim(AuthClaimTypes.Permission, ManageMessages))
            .AddPolicy(SuperAdmin, policy => policy.RequireClaim(AuthClaimTypes.Role, SuperAdmin));

        return services;
    }
}
