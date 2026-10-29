using Backend.Domain.Common;
using Backend.Domain.Properties;

namespace Backend.UnitTests.Properties;

public sealed class AddressTests
{
    [Fact]
    public void Create_WithValidData_ShouldNormalizeStateAndZipCode()
    {
        var address = Address.Create(" Rua A ", "  ", null, "Centro", "Campinas", " sp ", "13010-050");

        Assert.Equal("Rua A", address.Street);
        Assert.Null(address.Number);
        Assert.Equal("SP", address.State);
        Assert.Equal("13010050", address.ZipCode);
    }

    [Fact]
    public void Create_WithInvalidState_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Address.Create("Rua A", "1", null, "Centro", "Campinas", "XX", "13010050"));
    }

    [Theory]
    [InlineData("1301005")]
    [InlineData("130100500")]
    public void Create_WithInvalidZipCode_ShouldThrowDomainException(string zipCode)
    {
        Assert.Throws<DomainException>(() => Address.Create("Rua A", "1", null, "Centro", "Campinas", "SP", zipCode));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCity_ShouldThrowDomainException(string city)
    {
        Assert.Throws<DomainException>(() => Address.Create("Rua A", "1", null, "Centro", city, "SP", "13010050"));
    }

    [Fact]
    public void Create_WithMessyText_ShouldStoreNormalizedText()
    {
        var address = Address.Create(
            "Rua\u00A0Sa\u0303o\u0000 Bento", "12\u200B", "  ", "Ce\u0301u  Azul", "Sa\u0303o Paulo\u000D\u000A", "sp\u200B", "01001-000");

        Assert.Equal("Rua São Bento", address.Street);
        Assert.Equal("12", address.Number);
        Assert.Null(address.Complement);
        Assert.Equal("Céu Azul", address.Neighborhood);
        Assert.Equal("São Paulo", address.City);
        Assert.Equal("SP", address.State);
    }

    [Fact]
    public void Create_WithOnlyInvisibleCharactersInStreet_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Address.Create("\u200B\uFEFF\u0000", "1", null, "Centro", "Campinas", "SP", "13010050"));
    }

    [Fact]
    public void NormalizeZipCode_WithMask_ShouldReturnOnlyDigits()
    {
        Assert.Equal("01001000", Address.NormalizeZipCode(" 01.001-000 "));
    }

    [Fact]
    public void NormalizeZipCode_WithFullWidthDigits_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Address.NormalizeZipCode("\uFF10\uFF11\uFF10\uFF10\uFF11\uFF10\uFF10\uFF10"));
    }

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        var first = Address.Create("Rua A", "1", null, "Centro", "Campinas", "SP", "13010050");
        var second = Address.Create("Rua A", "1", null, "Centro", "Campinas", "sp", "13010-050");

        Assert.Equal(first, second);
    }
}
