using NetArchTest.Rules;

namespace Backend.ArchitectureTests;

internal static class ArchitectureAssert
{
    public static void Success(TestResult result)
    {
        var failingTypes = result.FailingTypeNames ?? [];
        Assert.True(result.IsSuccessful, $"Tipos que violam a regra: {string.Join(", ", failingTypes)}");
    }
}
