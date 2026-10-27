@.claude/skills/config-project-fullstack/SKILL.md

Eu quero que você extenda a capacidade dessa skill para fazer a configuração inicial do Swagger no backend. Ela deve continuar fazendo exatamente tudo que ela já faz; apenas adicionar os passos abaixo no final da configuração do backend, antes da verificação final.

Contexto: no .NET 10 o template webapi não vem mais com o Swashbuckle completo. O documento OpenAPI é gerado pelo pacote Microsoft.AspNetCore.OpenApi (já incluso no template, em /openapi/v1.json) e o Swagger UI apenas lê esse documento.

Passos (dentro de backend/):
- dotnet add src/Backend.Api package Swashbuckle.AspNetCore.SwaggerUI

- No arquivo (backend/src/Backend.Api/Backend.Api.csproj), adicionar no primeiro PropertyGroup, se ainda não existirem, as linhas abaixo. Os comentários XML (///) dos controllers passam a aparecer como descrição no Swagger; o 1591 evita que membro público sem comentário quebre o build:
```xml
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
```

- Substituir o arquivo (backend/src/Backend.Api/Program.cs) pelo conteúdo abaixo (é o Program.cs anterior + Swagger):
	- Título, versão e descrição do documento OpenAPI
	- Swagger UI em /swagger, apenas em Development
	- A rota / redireciona para /swagger em Development
```csharp
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["PORT"] ?? "4000";
builder.WebHost.UseUrls($"http://localhost:{port}");

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

- Adicionar na verificação final, com o backend rodando:
	- GET http://localhost:4000/swagger/index.html retorna 200
	- GET http://localhost:4000/openapi/v1.json retorna 200 e o campo info.title é "Backend API"

Não mudar absolutamente nada do que já foi feito, simplesmente adicionar essa nova capacidade na skill.
