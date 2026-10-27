using Backend.Application.Common;
using Backend.Application.Users;
using Backend.Application.Users.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Users;
using Backend.Infrastructure.Security;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.Users;

public sealed class UserPasswordServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now.AddDays(1));
    private readonly UserPasswordService _userPasswordService;

    public UserPasswordServiceTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("novo-hash");
        _userPasswordService = new UserPasswordService(_userRepository, _unitOfWork, _passwordHasher, _timeProvider);
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WithWrongCurrentPassword_ShouldReturnValidationError()
    {
        var user = TestData.Staff();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("errada", TestData.PasswordHash).Returns(false);

        var result = await _userPasswordService.ChangeOwnPasswordAsync(user.Id, new ChangeOwnPasswordRequest("errada", TestData.ValidPassword));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WithCorrectCurrentPassword_ShouldChangeHash()
    {
        var user = TestData.Staff();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("atual-123456", TestData.PasswordHash).Returns(true);

        var result = await _userPasswordService.ChangeOwnPasswordAsync(user.Id, new ChangeOwnPasswordRequest("atual-123456", TestData.ValidPassword));

        Assert.True(result.IsSuccess);
        Assert.Equal("novo-hash", user.PasswordHash);
        Assert.Equal(TestData.Now.AddDays(1), user.PasswordChangedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPasswordAsync_WithWeakPassword_ShouldReturnValidationError()
    {
        var user = TestData.Staff();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _userPasswordService.ResetPasswordAsync(user.Id, new ResetUserPasswordRequest("fraca"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var id = Guid.CreateVersion7();
        _userRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _userPasswordService.ResetPasswordAsync(id, new ResetUserPasswordRequest(TestData.ValidPassword));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
