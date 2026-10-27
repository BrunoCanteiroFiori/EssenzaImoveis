using Backend.Infrastructure.Database;
using Backend.Infrastructure.Http;
using Backend.Infrastructure.Security;
using Backend.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Backend.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os recursos reutilizáveis: cliente de conexão com o PostgreSQL (<see cref="NpgsqlDataSource"/>),
    /// fábrica de conexões, cliente HTTP genérico, relógio (<see cref="TimeProvider"/>), hash de senha e gerador de tokens seguros.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddHttpClient<IHttpRequestClient, HttpRequestClient>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        return services;
    }

    /// <summary>
    /// Registra o gerador de JWT assinado com HMAC-SHA256.
    /// </summary>
    public static IServiceCollection AddJwtTokenGenerator(this IServiceCollection services, JwtOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        return services;
    }

    /// <summary>
    /// Registra o armazenamento de arquivos em disco, dentro da pasta <paramref name="rootPath"/>.
    /// </summary>
    public static IServiceCollection AddLocalFileStorage(this IServiceCollection services, string rootPath)
    {
        services.AddSingleton<IFileStorage>(new LocalFileStorage(rootPath));
        return services;
    }
}
