using Backend.Application.Users;

namespace Backend.Api.Seeding;

/// <summary>
/// Ao iniciar a API, cria a conta fixa de super admin com os dados do .env (<c>SuperAdmin__Name</c>, <c>SuperAdmin__Email</c>
/// e <c>SuperAdmin__Password</c>) se ela ainda não existir. Roda em segundo plano para a API subir mesmo sem banco.
/// </summary>
public sealed class SuperAdminSeeder(
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration,
    ILogger<SuperAdminSeeder> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var name = configuration["SuperAdmin:Name"] ?? "Super Admin";
        var email = configuration["SuperAdmin:Email"];
        var password = configuration["SuperAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SuperAdmin__Email e SuperAdmin__Password não configurados no .env: o super admin não foi criado.");
            return;
        }

        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var superAdminService = scope.ServiceProvider.GetRequiredService<ISuperAdminService>();
            var result = await superAdminService.EnsureCreatedAsync(name, email, password, stoppingToken);
            if (result.IsFailure)
            {
                logger.LogError("Não foi possível criar o super admin: {Message}", result.Error.Message);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Não foi possível garantir o super admin. Verifique se o banco está no ar e com as migrations aplicadas.");
        }
    }
}
