using Backend.Infrastructure.Conversions;

namespace Backend.UnitTests.Infrastructure;

public sealed class HashConversionsTests
{
    [Fact]
    public void ToSha256Hex_WithKnownValue_ShouldReturnLowercaseHex()
    {
        var hash = "abc".ToSha256Hex();

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash);
    }
}
