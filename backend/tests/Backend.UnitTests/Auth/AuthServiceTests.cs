using System.Security.Claims;
using Backend.Application.Auth;
using Backend.Application.Auth.Dtos;
using Backend.Application.Common;
using Backend.Domain.Auth;
using Backend.Domain.Users;
using Backend.Infrastructure.Security;
using Backend.UnitTests.Common;
using NSubstitute;

namespace Backend.UnitTests.Auth;

public sealed class AuthServiceTests
{
    private const string RefreshToken = "refresh-token";

    private readonly IUserCredentialService _userCredentialService = Substitute.For<IUserCredentialService>();
    private readonly IRefreshSessionService _refreshSessionService = Substitute.For<IRefreshSessionService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _jwtTokenGenerator.Generate(Arg.Any<IEnumerable<Claim>>()).Returns(new JwtToken("access-token", TestData.Now.AddMinutes(15)));
        _refreshSessionService
            .CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new IssuedRefreshToken(RefreshToken, TestData.Now.AddDays(7)));
        _authService = new AuthService(_userCredentialService, _refreshSessionService, _userRepository, _jwtTokenGenerator);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ShouldReturnError()
    {
        _userCredentialService
            .ValidateAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<User>.Failure(AuthErrors.InvalidCredentials()));

        var result = await _authService.LoginAsync(new LoginRequest("maria@corretora.com.br", "errada"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        await _refreshSessionService.DidNotReceive().CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldIssueTokensWithPermissions()
    {
        var user = TestData.Staff(UserPermissions.WriteProperties);
        _userCredentialService.ValidateAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Result<User>.Success(user));

        var result = await _authService.LoginAsync(new LoginRequest(user.Email, TestData.ValidPassword));

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal(RefreshToken, result.Value.RefreshToken);
        _jwtTokenGenerator.Received(1).Generate(Arg.Is<IEnumerable<Claim>>(claims =>
            claims.Any(claim => claim.Type == AuthClaimTypes.Subject && claim.Value == user.Id.ToString())
            && claims.Count(claim => claim.Type == AuthClaimTypes.Permission) == 2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RefreshAsync_WithoutToken_ShouldReturnInvalidSession(string? refreshToken)
    {
        var result = await _authService.RefreshAsync(refreshToken);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidSession(), result.Error);
    }

    [Fact]
    public async Task RefreshAsync_WhenPasswordChangedAfterSession_ShouldRevokeAndReturnInvalidSession()
    {
        var user = TestData.Staff();
        var session = RefreshSession.Create(user.Id, new string('a', RefreshSession.TokenHashLength), TestData.Now);
        user.ChangePasswordHash("novo-hash", TestData.Now.AddHours(1));
        _refreshSessionService.ValidateAsync(RefreshToken, Arg.Any<CancellationToken>()).Returns(Result<RefreshSession>.Success(session));
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _authService.RefreshAsync(RefreshToken);

        Assert.True(result.IsFailure);
        await _refreshSessionService.Received(1).RevokeAsync(RefreshToken, Arg.Any<CancellationToken>());
        await _refreshSessionService.DidNotReceive().RotateAsync(Arg.Any<RefreshSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_WithValidSession_ShouldRotateToken()
    {
        var user = TestData.Staff();
        var session = RefreshSession.Create(user.Id, new string('a', RefreshSession.TokenHashLength), TestData.Now);
        _refreshSessionService.ValidateAsync(RefreshToken, Arg.Any<CancellationToken>()).Returns(Result<RefreshSession>.Success(session));
        _refreshSessionService.RotateAsync(session, Arg.Any<CancellationToken>()).Returns(new IssuedRefreshToken("novo-refresh", TestData.Now.AddDays(7)));
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _authService.RefreshAsync(RefreshToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("novo-refresh", result.Value.RefreshToken);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithInactiveUser_ShouldReturnInvalidSession()
    {
        var user = TestData.Staff();
        user.Deactivate(TestData.Now);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _authService.GetCurrentUserAsync(user.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }
}
