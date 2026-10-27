@.claude/skills/config-project-fullstack/SKILL.md

Eu quero que você extenda a capacidade dessa skill para criar as camadas do backend seguindo DDD: camada de negócio (Domain), camada de services (Application), camada de repository (Repository, somente responsável pela interação com o banco via EF Core + PostgreSQL) e camada de infraestrutura (Infrastructure, com tudo que pode ser reutilizado em todos os projetos: cliente de conexão com o banco, requisições HTTP, enums e conversões genéricas). Ela deve continuar fazendo exatamente tudo que ela já faz (inclusive o Swagger); apenas adicionar os passos abaixo no final da configuração do backend, antes da verificação final.

Estrutura do backend depois desta extensão:
```
backend/
  Backend.slnx
  Directory.Build.props
  docker-compose.yml          (PostgreSQL local)
  dotnet-tools.json           (dotnet-ef como ferramenta local)
  .env.example / .env
  src/
    Backend.Api/              (controllers, Swagger, composição das camadas)
    Backend.Application/      (services e DTOs)
    Backend.Domain/           (entidades, regras de negócio, interfaces de repository)
    Backend.Repository/       (somente banco: DbContext, repositories, configurações do EF Core, migrations)
    Backend.Infrastructure/   (reutilizável: conexão com o banco, HTTP, enums, conversões)
```

Regra de dependência entre camadas (não pode ser violada):
| Projeto | Pode referenciar | Não pode |
| --- | --- | --- |
| Backend.Domain | nenhum projeto e nenhum framework | EF Core, ASP.NET, Npgsql, Infrastructure (o domínio fica puro) |
| Backend.Infrastructure | só pacotes NuGet | qualquer projeto da solution (para poder ser copiada para outro projeto), EF Core, ASP.NET |
| Backend.Application | Backend.Domain, Backend.Infrastructure | Backend.Repository, Backend.Api, EF Core, ASP.NET, Npgsql |
| Backend.Repository | Backend.Domain, Backend.Infrastructure | Backend.Application, Backend.Api |
| Backend.Api | Backend.Application, Backend.Repository, Backend.Infrastructure (composition root) | controllers não usam Domain, Repository nem EF Core |

O que fica em cada uma das duas camadas novas:
- Backend.Repository: AppDbContext (que é o IUnitOfWork), a base genérica RepositoryBase<TEntity>, uma pasta por módulo com o repository e a configuração do EF Core da entidade, e as migrations. Nada além de acesso a dados.
- Backend.Infrastructure: código genérico, sem regra de negócio e sem conhecer nenhum módulo: Database (NpgsqlDataSource e fábrica de conexões), Http (cliente HTTP genérico), Enums (enums reutilizáveis; enum de regra de negócio fica no Domain) e Conversions (texto, enum, JSON).

Passos (dentro de backend/):
- dotnet new classlib -n Backend.Domain -o src/Backend.Domain --framework net10.0
- dotnet new classlib -n Backend.Application -o src/Backend.Application --framework net10.0
- dotnet new classlib -n Backend.Repository -o src/Backend.Repository --framework net10.0
- dotnet new classlib -n Backend.Infrastructure -o src/Backend.Infrastructure --framework net10.0
- Remover o arquivo Class1.cs dos quatro projetos
- dotnet sln Backend.slnx add src/Backend.Domain/Backend.Domain.csproj src/Backend.Application/Backend.Application.csproj src/Backend.Repository/Backend.Repository.csproj src/Backend.Infrastructure/Backend.Infrastructure.csproj
- dotnet add src/Backend.Application reference src/Backend.Domain src/Backend.Infrastructure
- dotnet add src/Backend.Repository reference src/Backend.Domain src/Backend.Infrastructure
- dotnet add src/Backend.Api reference src/Backend.Application src/Backend.Repository src/Backend.Infrastructure
- dotnet add src/Backend.Application package Microsoft.Extensions.DependencyInjection.Abstractions
- dotnet add src/Backend.Infrastructure package Npgsql
- dotnet add src/Backend.Infrastructure package Microsoft.Extensions.Http
- dotnet add src/Backend.Repository package Npgsql.EntityFrameworkCore.PostgreSQL
- dotnet add src/Backend.Repository package EFCore.NamingConventions
- dotnet add src/Backend.Api package Microsoft.EntityFrameworkCore.Design
- dotnet new tool-manifest
- dotnet tool install dotnet-ef

## Domain

- Criar o arquivo (backend/src/Backend.Domain/Abstractions/IRepository.cs):
```csharp
namespace Backend.Domain.Abstractions;

public interface IRepository<TEntity>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
```

- Criar o arquivo (backend/src/Backend.Domain/Abstractions/IUnitOfWork.cs):
```csharp
namespace Backend.Domain.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## Application

- Criar o arquivo (backend/src/Backend.Application/DependencyInjection.cs). Os services são registrados por convenção, então criar um módulo novo não exige editar este arquivo:
```csharp
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
```

## Infrastructure (reutilizável)

- Criar o arquivo (backend/src/Backend.Infrastructure/DependencyInjection.cs). Registra o cliente de conexão com o banco (um NpgsqlDataSource único para a aplicação inteira, usado pelo EF Core da camada Repository e por quem precisar de conexão direta) e o cliente HTTP:
```csharp
using Backend.Infrastructure.Database;
using Backend.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Backend.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os recursos reutilizáveis: cliente de conexão com o PostgreSQL (<see cref="NpgsqlDataSource"/>),
    /// fábrica de conexões e cliente HTTP genérico.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddHttpClient<IHttpRequestClient, HttpRequestClient>();
        return services;
    }
}
```

- Criar o arquivo (backend/src/Backend.Infrastructure/Database/IDbConnectionFactory.cs):
```csharp
using System.Data.Common;

namespace Backend.Infrastructure.Database;

public interface IDbConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
```

- Criar o arquivo (backend/src/Backend.Infrastructure/Database/NpgsqlConnectionFactory.cs):
```csharp
using System.Data.Common;
using Npgsql;

namespace Backend.Infrastructure.Database;

public sealed class NpgsqlConnectionFactory(NpgsqlDataSource dataSource) : IDbConnectionFactory
{
    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
        await dataSource.OpenConnectionAsync(cancellationToken);
}
```

- Criar o arquivo (backend/src/Backend.Infrastructure/Http/IHttpRequestClient.cs):
```csharp
namespace Backend.Infrastructure.Http;

public interface IHttpRequestClient
{
    Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

    Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);
}
```

- Criar o arquivo (backend/src/Backend.Infrastructure/Http/HttpRequestClient.cs):
```csharp
using System.Net;
using System.Net.Http.Json;
using Backend.Infrastructure.Conversions;

namespace Backend.Infrastructure.Http;

/// <summary>
/// Cliente HTTP genérico com JSON. Lança <see cref="HttpRequestException"/> quando a resposta não é de sucesso.
/// </summary>
public sealed class HttpRequestClient(HttpClient httpClient) : IHttpRequestClient
{
    public async Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        return await ReadAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(requestUri, body, JsonConversions.DefaultOptions, cancellationToken);
        return await ReadAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(requestUri, body, JsonConversions.DefaultOptions, cancellationToken);
        return await ReadAsync<TResponse>(response, cancellationToken);
    }

    public async Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<TResponse?> ReadAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(JsonConversions.DefaultOptions, cancellationToken);
    }
}
```

- Criar o arquivo (backend/src/Backend.Infrastructure/Enums/SortDirection.cs). Exemplo de enum reutilizável; novos enums genéricos entram nesta pasta, com `[Description]` em pt-BR:
```csharp
using System.ComponentModel;

namespace Backend.Infrastructure.Enums;

public enum SortDirection
{
    [Description("Crescente")]
    Ascending,

    [Description("Decrescente")]
    Descending,
}
```

- Criar o arquivo (backend/src/Backend.Infrastructure/Conversions/EnumConversions.cs):
```csharp
using System.ComponentModel;
using System.Reflection;

namespace Backend.Infrastructure.Conversions;

public static class EnumConversions
{
    /// <summary>
    /// Retorna o texto do atributo <see cref="DescriptionAttribute"/> do valor ou, se não houver, o nome do valor.
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var name = value.ToString();
        var field = value.GetType().GetField(name);
        return field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;
    }

    /// <summary>
    /// Converte um texto (nome do valor, sem diferenciar maiúsculas) para o enum. Lança <see cref="ArgumentException"/> se não existir.
    /// </summary>
    public static TEnum ToEnum<TEnum>(this string value)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) && Enum.IsDefined(result))
        {
            return result;
        }

        throw new ArgumentException($"O valor '{value}' não é válido para {typeof(TEnum).Name}.", nameof(value));
    }
}
```

- Criar o arquivo (backend/src/Backend.Infrastructure/Conversions/StringConversions.cs):
```csharp
using System.Globalization;
using System.Text;

namespace Backend.Infrastructure.Conversions;

public static class StringConversions
{
    /// <summary>
    /// Converte PascalCase, camelCase, kebab-case ou texto com espaços para snake_case (ex.: UserProfile → user_profile).
    /// </summary>
    public static string ToSnakeCase(this string value) => ToSeparatedLowerCase(value, '_');

    /// <summary>
    /// Converte PascalCase, camelCase, snake_case ou texto com espaços para kebab-case (ex.: UserProfiles → user-profiles).
    /// </summary>
    public static string ToKebabCase(this string value) => ToSeparatedLowerCase(value, '-');

    /// <summary>
    /// Remove acentos e cedilha (ex.: "Relatório de transações" → "Relatorio de transacoes").
    /// </summary>
    public static string RemoveDiacritics(this string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string ToSeparatedLowerCase(string value, char separator)
    {
        var builder = new StringBuilder(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (character is '-' or '_' or ' ')
            {
                if (builder.Length > 0 && builder[^1] != separator)
                {
                    builder.Append(separator);
                }

                continue;
            }

            if (char.IsUpper(character) && builder.Length > 0 && builder[^1] != separator)
            {
                var previous = value[index - 1];
                var nextIsLower = index + 1 < value.Length && char.IsLower(value[index + 1]);
                if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
                {
                    builder.Append(separator);
                }
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
```

- Criar o arquivo (backend/src/Backend.Infrastructure/Conversions/JsonConversions.cs):
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Infrastructure.Conversions;

public static class JsonConversions
{
    /// <summary>
    /// Opções padrão de JSON do projeto: camelCase, sem diferenciar maiúsculas na leitura e enums como texto.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = CreateDefaultOptions();

    public static string ToJson<TValue>(this TValue value) => JsonSerializer.Serialize(value, DefaultOptions);

    public static TValue? FromJson<TValue>(this string json) => JsonSerializer.Deserialize<TValue>(json, DefaultOptions);

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
```

## Repository (somente banco)

- Criar o arquivo (backend/src/Backend.Repository/AppDbContext.cs). As configurações das entidades são carregadas automaticamente de todas as classes IEntityTypeConfiguration deste projeto:
```csharp
using Backend.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- Criar o arquivo (backend/src/Backend.Repository/Common/RepositoryBase.cs). O nome tem sufixo Base para não confundir com o namespace Backend.Repository:
```csharp
using Backend.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository.Common;

public abstract class RepositoryBase<TEntity>(AppDbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    protected DbSet<TEntity> Entities { get; } = dbContext.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Entities.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Entities.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Entities.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) => Entities.Update(entity);

    public void Remove(TEntity entity) => Entities.Remove(entity);
}
```

- Criar o arquivo (backend/src/Backend.Repository/DependencyInjection.cs). O EF Core usa o NpgsqlDataSource registrado pela Infrastructure; tabelas e colunas em snake_case; repositories registrados por convenção:
```csharp
using Backend.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Backend.Repository;

public static class DependencyInjection
{
    /// <summary>
    /// Registra o DbContext (EF Core sobre o NpgsqlDataSource registrado pela Infrastructure), o unit of work
    /// e os repositories por convenção: toda classe concreta <c>XRepository</c> é registrada pela interface <c>IXRepository</c>.
    /// </summary>
    public static IServiceCollection AddRepository(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>()).UseSnakeCaseNamingConvention());
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
```

## Api

- Substituir o arquivo (backend/src/Backend.Api/Program.cs) pelo conteúdo abaixo (é o Program.cs anterior com Swagger + registro das camadas). A ordem importa: AddInfrastructure registra o NpgsqlDataSource que o AddRepository usa. A API sobe mesmo sem o PostgreSQL rodando; o banco só é acessado quando um endpoint usa um repository:
```csharp
using Backend.Application;
using Backend.Infrastructure;
using Backend.Repository;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["PORT"] ?? "4000";
builder.WebHost.UseUrls($"http://localhost:{port}");

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("A connection string 'Default' não foi configurada. Verifique o arquivo .env.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddRepository();
builder.Services.AddApplication();

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Backend API";
        document.Info.Version = "v1";
        document.Info.Description = "API do backend do projeto.";
        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Backend API v1");
        options.RoutePrefix = "swagger";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

## Banco local

- Criar o arquivo (backend/docker-compose.yml). O Docker Compose lê as variáveis do backend/.env automaticamente:
```yaml
services:
  postgres:
    image: postgres:18
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    ports:
      - "${POSTGRES_PORT}:5432"
    volumes:
      - postgres-data:/var/lib/postgresql

volumes:
  postgres-data:
```

- O arquivo backend/.env.example criado pela skill passa a ter o conteúdo abaixo (credenciais apenas de desenvolvimento local). O passo de copiar backend/.env.example para backend/.env continua igual:
```env
PORT=4000
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=backend
POSTGRES_PORT=5432
ConnectionStrings__Default="Host=localhost;Port=5432;Database=backend;Username=postgres;Password=postgres"
```

- Adicionar na verificação final:
	- dotnet build backend/Backend.slnx sem erros e sem warnings
	- O backend sobe e GET /health retorna 200 mesmo sem o PostgreSQL rodando
	- No resumo final, informar o comando para subir o banco (não executar automaticamente): `docker compose up -d` dentro de backend/

Não mudar absolutamente nada do que já foi feito, simplesmente adicionar essa nova capacidade na skill. Não criar entidades, services, repositories concretos nem migrations nesta etapa: isso é responsabilidade das skills de DTOs/entidades e de novo módulo.
