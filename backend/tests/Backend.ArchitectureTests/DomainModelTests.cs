using System.Reflection;
using Backend.Domain.Common;
using NetArchTest.Rules;

namespace Backend.ArchitectureTests;

public sealed class DomainModelTests
{
    [Fact]
    public void AggregateRoots_ShouldBeSealed()
    {
        var result = Types.InAssembly(Layers.Domain)
            .That()
            .Inherit(typeof(AggregateRoot))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void ValueObjects_ShouldBeSealed()
    {
        var result = Types.InAssembly(Layers.Domain)
            .That()
            .Inherit(typeof(ValueObject))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Entities_ShouldNotHavePublicSetters()
    {
        var violations = Types.InAssembly(Layers.Domain)
            .That()
            .Inherit(typeof(Entity))
            .GetTypes()
            .SelectMany(type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(property => property.SetMethod?.IsPublic == true)
                .Select(property => $"{type.Name}.{property.Name}"))
            .ToList();

        Assert.Empty(violations);
    }
}
