using Backend.Domain.Abstractions;
using Backend.Repository.AuditLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Backend.Repository;

public static class DependencyInjection
{
    /// <summary>
    /// Registra o DbContext (EF Core sobre o NpgsqlDataSource registrado pela Infrastructure) com a auditoria automática,
    /// o unit of work e os repositories por convenção: toda classe concreta <c>XRepository</c> é registrada pela interface <c>IXRepository</c>.
    /// Requer um <see cref="ICurrentUser"/> registrado pela Api.
    /// </summary>
    public static IServiceCollection AddRepository(this IServiceCollection services)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

        var implementations = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith("Repository", StringComparison.Ordinal));

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
