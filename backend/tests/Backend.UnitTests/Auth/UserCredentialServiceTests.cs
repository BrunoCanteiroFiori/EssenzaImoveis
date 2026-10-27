using Backend.Application.Auth;
using Backend.Application.Auth.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Users;
using Backend.Infrastructure.Security;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.Auth;

public sealed class UserCredentialServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly UserCredentialService _userCredentialService;

    public UserCredentialServiceTests()
    {
        _userCredentialService = new UserCredentialService(_userRepository, _passwordHasher, _unitOfWork, _timeProvider);
    }

    [Fact]
    public async Task ValidateAsync_WithUnknownEmail_ShouldReturnInvalidCredentialsAndStillHash()
    {
        var result = await _userCredentialService.ValidateAsync(new LoginRequest("ninguem@corretora.com.br", TestData.ValidPassword));

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidCredentials(), result.Error);
        _passwordHasher.Received(1).Hash(TestData.ValidPassword);
    }

    [Fact]
    public async Task ValidateAsync_WithWrongPassword_ShouldRegisterFailedSignIn()
    {
        var user = TestData.Staff();
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _userCredentialService.ValidateAsync(new LoginRequest(user.Email, "errada"));

        Assert.True(result.IsFailure);
        Assert.Equal(1, user.FailedSignInAttempts);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_WhenLockedOut_ShouldReturnLockedOutWithoutCheckingPassword()
    {
        var user = TestData.Staff();
        for (var attempt = 0; attempt < User.MaxFailedSignInAttempts; attempt++)
        {
            user.RegisterFailedSignIn(TestData.Now);
        }

        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _userCredentialService.ValidateAsync(new LoginRequest(user.Email, TestData.ValidPassword));

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.LockedOut(), result.Error);
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ValidateAsync_WithInactiveUser_ShouldReturnInvalidCredentials()
    {
        var user = TestData.Staff();
        user.Deactivate(TestData.Now);
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(TestData.ValidPassword, TestData.PasswordHash).Returns(true);

        var result = await _userCredentialService.ValidateAsync(new LoginRequest(user.Email, TestData.ValidPassword));

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidCredentials(), result.Error);
    }

    [Fact]
    public async Task ValidateAsync_WithValidCredentials_ShouldReturnUserAndRegisterSignIn()
    {
        var user = TestData.Staff();
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(TestData.ValidPassword, TestData.PasswordHash).Returns(true);

        var result = await _userCredentialService.ValidateAsync(new LoginRequest(" MARIA@corretora.com.br ", TestData.ValidPassword));

        Assert.True(result.IsSuccess);
        Assert.Equal(user, result.Value);
        Assert.Equal(TestData.Now, user.LastSignInAt);
    }
}
