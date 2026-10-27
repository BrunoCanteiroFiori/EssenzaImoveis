using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Api.RateLimiting;

/// <summary>
/// Limites por IP para as rotas sensíveis a força bruta e spam. Excedido o limite, a resposta é 429.
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Refresh = "refresh";
    public const string ContactMessages = "contact-messages";
    public const string PropertyViews = "property-views";

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddFixedWindow(options, Login, permitLimit: 5, TimeSpan.FromMinutes(1));
            AddFixedWindow(options, Refresh, permitLimit: 30, TimeSpan.FromMinutes(1));
            AddFixedWindow(options, ContactMessages, permitLimit: 5, TimeSpan.FromMinutes(10));
            AddFixedWindow(options, PropertyViews, permitLimit: 60, TimeSpan.FromMinutes(1));
        });

        return services;
    }

    private static void AddFixedWindow(RateLimiterOptions options, string policyName, int permitLimit, TimeSpan window) =>
        options.AddPolicy(policyName, httpContext => RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));
}
