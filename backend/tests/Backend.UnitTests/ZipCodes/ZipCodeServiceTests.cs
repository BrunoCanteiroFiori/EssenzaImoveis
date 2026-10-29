using Backend.Application.Common;
using Backend.Application.ZipCodes;
using Backend.Infrastructure.ZipCodes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.UnitTests.ZipCodes;

public sealed class ZipCodeServiceTests
{
    private readonly IZipCodeClient _zipCodeClient = Substitute.For<IZipCodeClient>();
    private readonly ZipCodeService _zipCodeService;

    public ZipCodeServiceTests()
    {
        _zipCodeService = new ZipCodeService(_zipCodeClient);
    }

    [Fact]
    public async Task GetAddressAsync_WithMaskedZipCode_ShouldReturnNormalizedAddress()
    {
        _zipCodeClient.FindAsync("01001000", Arg.Any<CancellationToken>())
            .Returns(new ZipCodeAddress("01001000", " Praça  da Sé ", "Se\u0301", "Sa\u0303o Paulo", "sp"));

        var result = await _zipCodeService.GetAddressAsync("01001-000");

        Assert.True(result.IsSuccess);
        Assert.Equal("01001000", result.Value.ZipCode);
        Assert.Equal("Praça da Sé", result.Value.Street);
        Assert.Equal("Sé", result.Value.Neighborhood);
        Assert.Equal("São Paulo", result.Value.City);
        Assert.Equal("SP", result.Value.State);
    }

    [Fact]
    public async Task GetAddressAsync_WithCityWideZipCode_ShouldReturnNullStreetAndNeighborhood()
    {
        _zipCodeClient.FindAsync("13480000", Arg.Any<CancellationToken>())
            .Returns(new ZipCodeAddress("13480000", string.Empty, string.Empty, "Limeira", "SP"));

        var result = await _zipCodeService.GetAddressAsync("13480000");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Street);
        Assert.Null(result.Value.Neighborhood);
        Assert.Equal("Limeira", result.Value.City);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0100100")]
    [InlineData("010010000")]
    public async Task GetAddressAsync_WithInvalidZipCode_ShouldReturnValidationErrorWithoutCallingClient(string zipCode)
    {
        var result = await _zipCodeService.GetAddressAsync(zipCode);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _zipCodeClient.DidNotReceive().FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAddressAsync_WhenZipCodeDoesNotExist_ShouldReturnNotFound()
    {
        _zipCodeClient.FindAsync("99999999", Arg.Any<CancellationToken>()).Returns((ZipCodeAddress?)null);

        var result = await _zipCodeService.GetAddressAsync("99999-999");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetAddressAsync_WhenServiceIsUnavailable_ShouldReturnUnavailable()
    {
        _zipCodeClient.FindAsync("01001000", Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("Fora do ar."));

        var result = await _zipCodeService.GetAddressAsync("01001000");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
    }
}
