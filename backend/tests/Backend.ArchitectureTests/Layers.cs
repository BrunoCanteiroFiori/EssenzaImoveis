using System.Reflection;

namespace Backend.ArchitectureTests;

internal static class Layers
{
    public const string DomainNamespace = "Backend.Domain";
    public const string ApplicationNamespace = "Backend.Application";
    public const string RepositoryNamespace = "Backend.Repository";
    public const string InfrastructureNamespace = "Backend.Infrastructure";
    public const string ApiNamespace = "Backend.Api";

    public static readonly Assembly Domain = typeof(global::Backend.Domain.Abstractions.IUnitOfWork).Assembly;
    public static readonly Assembly Application = typeof(global::Backend.Application.DependencyInjection).Assembly;
    public static readonly Assembly Repository = typeof(global::Backend.Repository.DependencyInjection).Assembly;
    public static readonly Assembly Infrastructure = typeof(global::Backend.Infrastructure.DependencyInjection).Assembly;
    public static readonly Assembly Api = typeof(Program).Assembly;
}
