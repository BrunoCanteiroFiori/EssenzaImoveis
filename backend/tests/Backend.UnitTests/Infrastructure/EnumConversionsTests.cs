using Backend.Infrastructure.Conversions;
using Backend.Infrastructure.Enums;

namespace Backend.UnitTests.Infrastructure;

public sealed class EnumConversionsTests
{
    [Fact]
    public void GetDescription_WithDescriptionAttribute_ShouldReturnDescription()
    {
        var result = SortDirection.Descending.GetDescription();

        Assert.Equal("Decrescente", result);
    }

    [Theory]
    [InlineData("ascending", SortDirection.Ascending)]
    [InlineData("Descending", SortDirection.Descending)]
    public void ToEnum_WithValidName_ShouldReturnEnumValue(string value, SortDirection expected)
    {
        var result = value.ToEnum<SortDirection>();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("99")]
    public void ToEnum_WithInvalidValue_ShouldThrowArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => value.ToEnum<SortDirection>());
    }
}
