using Backend.Domain.Common;
using Backend.Domain.Users;

namespace Backend.UnitTests.Users;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Curta1")]
    [InlineData("SomenteLetras")]
    [InlineData("1234567890")]
    public void EnsureIsValid_WithWeakPassword_ShouldThrowDomainException(string password)
    {
        Assert.Throws<DomainException>(() => PasswordPolicy.EnsureIsValid(password));
    }

    [Fact]
    public void EnsureIsValid_WithStrongPassword_ShouldNotThrow()
    {
        var exception = Record.Exception(() => PasswordPolicy.EnsureIsValid("SenhaForte123"));

        Assert.Null(exception);
    }
}
