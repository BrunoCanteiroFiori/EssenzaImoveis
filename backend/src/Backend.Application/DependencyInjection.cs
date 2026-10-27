using Microsoft.Extensions.DependencyInjection;

namespace Backend.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os services da camada de aplicação por convenção:
    /// toda classe concreta <c>XService</c> é registrada pela interface <c>IXService</c>.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var implementations = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith("Service", StringComparison.Ordinal));

        foreach (var implementation in implementations)
        {
            var contract = implementation.GetInterface($"I{implementation.Name}");
            if (contract is not null)
            {
                services.AddScoped(contract, implementation);
            }
        }

        return services;
    }
}
