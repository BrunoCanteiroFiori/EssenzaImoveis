using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace Backend.ArchitectureTests;

public sealed class NamingConventionTests
{
    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        var result = Types.InAssemblies([Layers.Domain, Layers.Application, Layers.Repository, Layers.Infrastructure, Layers.Api])
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Controllers_ShouldEndWithControllerAndResideInControllersNamespace()
    {
        var result = Types.InAssembly(Layers.Api)
            .That()
            .Inherit(typeof(ControllerBase))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Controller")
            .And()
            .ResideInNamespace($"{Layers.ApiNamespace}.Controllers")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Services_ShouldBeSealed()
    {
        var result = Types.InAssembly(Layers.Application)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Service")
            .Should()
            .BeSealed()
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Dtos_ShouldBeSealedAndResideInDtosNamespace()
    {
        var result = Types.InAssembly(Layers.Application)
            .That()
            .HaveNameEndingWith("Request")
            .Or()
            .HaveNameEndingWith("Response")
            .Should()
            .BeSealed()
            .And()
            .ResideInNamespaceMatching(@"^Backend\.Application\.\w+\.Dtos$")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void RepositoryInterfaces_ShouldResideInDomain()
    {
        var result = Types.InAssemblies([Layers.Application, Layers.Repository, Layers.Infrastructure, Layers.Api])
            .That()
            .AreInterfaces()
            .ShouldNot()
            .HaveNameEndingWith("Repository")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Repositories_ShouldBeSealedAndResideInModuleNamespace()
    {
        var result = Types.InAssembly(Layers.Repository)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Repository")
            .Should()
            .BeSealed()
            .And()
            .ResideInNamespaceMatching(@"^Backend\.Repository\.\w+$")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void EntityConfigurations_ShouldBeSealedAndResideInModuleNamespace()
    {
        var result = Types.InAssembly(Layers.Repository)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Configuration")
            .Should()
            .BeSealed()
            .And()
            .ResideInNamespaceMatching(@"^Backend\.Repository\.\w+$")
            .GetResult();

        ArchitectureAssert.Success(result);
    }
}
