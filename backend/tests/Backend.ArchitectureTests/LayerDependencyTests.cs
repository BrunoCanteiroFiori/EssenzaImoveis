using NetArchTest.Rules;

namespace Backend.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string EntityFramework = "Microsoft.EntityFrameworkCore";
    private const string AspNetCore = "Microsoft.AspNetCore";
    private const string Npgsql = "Npgsql";

    [Fact]
    public void Domain_ShouldNotDependOnOtherLayers()
    {
        var result = Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.ApplicationNamespace, Layers.RepositoryNamespace, Layers.InfrastructureNamespace, Layers.ApiNamespace)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Domain_ShouldNotDependOnFrameworks()
    {
        var result = Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(EntityFramework, AspNetCore, Npgsql)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnProjectLayers()
    {
        var result = Types.InAssembly(Layers.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.DomainNamespace, Layers.ApplicationNamespace, Layers.RepositoryNamespace, Layers.ApiNamespace)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnEntityFrameworkOrAspNetCore()
    {
        var result = Types.InAssembly(Layers.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(EntityFramework, AspNetCore)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Application_ShouldNotDependOnRepositoryOrApi()
    {
        var result = Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.RepositoryNamespace, Layers.ApiNamespace)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Application_ShouldNotDependOnFrameworks()
    {
        var result = Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(EntityFramework, AspNetCore, Npgsql)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Repository_ShouldNotDependOnApplicationOrApi()
    {
        var result = Types.InAssembly(Layers.Repository)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.ApplicationNamespace, Layers.ApiNamespace)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Controllers_ShouldNotDependOnDomainOrRepository()
    {
        var result = Types.InAssembly(Layers.Api)
            .That()
            .ResideInNamespace($"{Layers.ApiNamespace}.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny(Layers.DomainNamespace, Layers.RepositoryNamespace, EntityFramework)
            .GetResult();

        ArchitectureAssert.Success(result);
    }
}
