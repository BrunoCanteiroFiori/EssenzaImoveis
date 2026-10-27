using Backend.Infrastructure.Security;

namespace Backend.UnitTests.Infrastructure;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _passwordHasher = new();

    [Fact]
    public void Verify_WithSamePassword_ShouldReturnTrue()
    {
        var hash = _passwordHasher.Hash("SenhaForte123");

        Assert.StartsWith("pbkdf2-sha256$", hash, StringComparison.Ordinal);
        Assert.True(_passwordHasher.Verify("SenhaForte123", hash));
        Assert.False(_passwordHasher.Verify("SenhaErrada123", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ShouldUseDifferentSalts()
    {
        var first = _passwordHasher.Hash("SenhaForte123");
        var second = _passwordHasher.Hash("SenhaForte123");

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto-qualquer")]
    [InlineData("pbkdf2-sha256$abc$salt$hash")]
    [InlineData("pbkdf2-sha256$1000$***$***")]
    public void Verify_WithMalformedHash_ShouldReturnFalse(string passwordHash)
    {
        Assert.False(_passwordHasher.Verify("SenhaForte123", passwordHash));
    }
}
