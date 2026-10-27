using Backend.Application.Common;
using Backend.Application.Users;
using Backend.Domain.Abstractions;
using Backend.Domain.Users;
using Backend.Infrastructure.Security;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.Users;

public sealed class SuperAdminServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly SuperAdminService _superAdminService;

    public SuperAdminServiceTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns(TestData.PasswordHash);
        _superAdminService = new SuperAdminService(_userRepository, _unitOfWork, _passwordHasher, _timeProvider);
    }

    [Fact]
    public async Task EnsureCreatedAsync_WhenSuperAdminExists_ShouldNotCreateAnother()
    {
        _userRepository.ExistsSuperAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _superAdminService.EnsureCreatedAsync("Admin", "admin@corretora.com.br", TestData.ValidPassword);

        Assert.True(result.IsSuccess);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureCreatedAsync_WhenMissing_ShouldCreateSuperAdmin()
    {
        var result = await _superAdminService.EnsureCreatedAsync("Admin", "admin@corretora.com.br", TestData.ValidPassword);

        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(user => user.IsSuperAdmin), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureCreatedAsync_WithWeakPassword_ShouldReturnValidationError()
    {
        var result = await _superAdminService.EnsureCreatedAsync("Admin", "admin@corretora.com.br", "fraca");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
