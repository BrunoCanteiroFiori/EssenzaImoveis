using Backend.Infrastructure.Conversions;

namespace Backend.UnitTests.Infrastructure;

public sealed class StringConversionsTests
{
    [Theory]
    [InlineData("UserProfile", "user_profile")]
    [InlineData("userProfile", "user_profile")]
    [InlineData("user-profiles", "user_profiles")]
    [InlineData("HTTPRequest", "http_request")]
    [InlineData("Report", "report")]
    public void ToSnakeCase_WithValue_ShouldReturnSnakeCase(string value, string expected)
    {
        var result = value.ToSnakeCase();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("UserProfiles", "user-profiles")]
    [InlineData("user_profiles", "user-profiles")]
    [InlineData("Reports", "reports")]
    public void ToKebabCase_WithValue_ShouldReturnKebabCase(string value, string expected)
    {
        var result = value.ToKebabCase();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveDiacritics_WithAccents_ShouldReturnTextWithoutAccents()
    {
        var result = "Relatório de transações".RemoveDiacritics();

        Assert.Equal("Relatorio de transacoes", result);
    }
}
