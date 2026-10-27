using Backend.Domain.Common;

namespace Backend.UnitTests.Common;

public sealed class EmailAddressTests
{
    [Fact]
    public void Normalize_WithValidEmail_ShouldTrimAndLowercase()
    {
        var email = EmailAddress.Normalize("  Pessoa@Exemplo.COM ");

        Assert.Equal("pessoa@exemplo.com", email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sem-arroba")]
    [InlineData("pessoa@dominio")]
    [InlineData("Nome <pessoa@exemplo.com>")]
    public void Normalize_WithInvalidEmail_ShouldThrowDomainException(string email)
    {
        Assert.Throws<DomainException>(() => EmailAddress.Normalize(email));
    }
}
