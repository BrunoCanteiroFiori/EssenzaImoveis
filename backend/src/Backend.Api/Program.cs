using System.Text.Json.Serialization;
using Backend.Api.Authentication;
using Backend.Api.Authorization;
using Backend.Api.RateLimiting;
using Backend.Api.Seeding;
using Backend.Application;
using Backend.Domain.Abstractions;
using Backend.Infrastructure;
using Backend.Infrastructure.Security;
using Backend.Infrastructure.Storage;
using Backend.Repository;
using Microsoft.AspNetCore.HttpOverrides;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var host = builder.Configuration["HOST"] ?? "localhost";
var port = builder.Configuration["PORT"] ?? "4000";
builder.WebHost.UseUrls($"http://{host}:{port}");

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("A connection string 'Default' não foi configurada. Verifique o arquivo .env.");

var jwtOptions = new JwtOptions(
    builder.Configuration["Jwt:Issuer"] ?? "backend-api",
    builder.Configuration["Jwt:Audience"] ?? "frontend",
    builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("O segredo 'Jwt__Secret' não foi configurado. Verifique o arquivo .env."),
    TimeSpan.FromMinutes(builder.Configuration.GetValue("Jwt:AccessTokenMinutes", 15)));

var storageProvider = builder.Configuration["Storage:Provider"] ?? "GoogleDrive";
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var secureCookies = builder.Configuration.GetValue("Auth:SecureCookies", !builder.Environment.IsDevelopment());
var trustedProxyNetworks = (builder.Configuration["ForwardedHeaders:KnownNetworks"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(network => System.Net.IPNetwork.Parse(network));

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddJwtTokenGenerator(jwtOptions);
if (storageProvider == "GoogleDrive")
{
    builder.Services.AddGoogleDriveFileStorage(new GoogleDriveOptions(
        RequiredSetting("GoogleDrive:ClientId"),
        RequiredSetting("GoogleDrive:ClientSecret"),
        RequiredSetting("GoogleDrive:RefreshToken"),
        builder.Configuration["GoogleDrive:RootFolderName"] ?? "SiteCorretora"));
}
else if (storageProvider == "Local")
{
    builder.Services.AddLocalFileStorage(
        Path.GetFullPath(builder.Configuration["Storage:RootPath"] ?? "uploads", builder.Environment.ContentRootPath));
}
else
{
    throw new InvalidOperationException($"Storage__Provider '{storageProvider}' inválido: use GoogleDrive ou Local.");
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddRepository();
builder.Services.AddApplication();

builder.Services.AddSingleton(new AuthCookies(secureCookies));
builder.Services.AddJwtCookieAuthentication(jwtOptions);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddRateLimitPolicies();
builder.Services.AddHostedService<SuperAdminSeeder>();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Backend API";
        document.Info.Version = "v1";
        document.Info.Description = "API da corretora de imóveis: área pública (busca, detalhe, contato) e área administrativa.";
        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
foreach (var network in trustedProxyNetworks)
{
    forwardedHeadersOptions.KnownIPNetworks.Add(network);
}

app.UseForwardedHeaders(forwardedHeadersOptions);
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Backend API v1");
        options.RoutePrefix = "swagger";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription().AllowAnonymous();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

string RequiredSetting(string key) =>
    builder.Configuration[key] is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"A configuração '{key.Replace(":", "__", StringComparison.Ordinal)}' não foi definida. Verifique o arquivo .env.");
