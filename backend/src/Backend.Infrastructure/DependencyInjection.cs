using Backend.Infrastructure.Database;
using Backend.Infrastructure.Http;
using Backend.Infrastructure.Security;
using Backend.Infrastructure.Storage;
using Backend.Infrastructure.ZipCodes;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Backend.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os recursos reutilizáveis: cliente de conexão com o PostgreSQL (<see cref="NpgsqlDataSource"/>),
    /// fábrica de conexões, cliente HTTP genérico, consulta de CEP, relógio (<see cref="TimeProvider"/>), hash de senha e gerador de tokens seguros.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddHttpClient<IHttpRequestClient, HttpRequestClient>();
        services.AddTransient<IZipCodeClient, ViaCepZipCodeClient>();
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

    /// <summary>
    /// Registra o armazenamento de arquivos no Google Drive de uma conta pessoal (OAuth com refresh token, escopo <c>drive.file</c>).
    /// O token de acesso é renovado sozinho a partir do refresh token.
    /// </summary>
    public static IServiceCollection AddGoogleDriveFileStorage(this IServiceCollection services, GoogleDriveOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton(_ => new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = new UserCredential(
                new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets { ClientId = options.ClientId, ClientSecret = options.ClientSecret },
                    Scopes = [DriveService.Scope.DriveFile],
                }),
                options.ClientId,
                new TokenResponse { RefreshToken = options.RefreshToken }),
        }));
        services.AddSingleton<IFileStorage, GoogleDriveFileStorage>();
        return services;
    }
}
