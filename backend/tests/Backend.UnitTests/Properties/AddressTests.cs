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
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        var first = Address.Create("Rua A", "1", null, "Centro", "Campinas", "SP", "13010050");
        var second = Address.Create("Rua A", "1", null, "Centro", "Campinas", "sp", "13010-050");

        Assert.Equal(first, second);
    }
}
