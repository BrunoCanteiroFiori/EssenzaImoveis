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

public sealed class UserServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns(TestData.PasswordHash);
        _userService = new UserService(_userRepository, _unitOfWork, _passwordHasher, _timeProvider);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldPersistHashedUser()
    {
        var request = new CreateUserRequest("Maria", "maria@corretora.com.br", TestData.ValidPassword, [UserPermissionNames.WriteProperties]);

        var result = await _userService.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal([UserPermissionNames.ReadProperties, UserPermissionNames.WriteProperties], result.Value.Permissions);
        _passwordHasher.Received(1).Hash(TestData.ValidPassword);
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(user => user.PasswordHash == TestData.PasswordHash),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithEmailInUse_ShouldReturnConflict()
    {
        _userRepository.ExistsByEmailAsync("maria@corretora.com.br", null, Arg.Any<CancellationToken>()).Returns(true);
        var request = new CreateUserRequest("Maria", "Maria@Corretora.com.br", TestData.ValidPassword, []);

        var result = await _userService.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownPermission_ShouldReturnValidationError()
    {
        var request = new CreateUserRequest("Maria", "maria@corretora.com.br", TestData.ValidPassword, ["Tudo"]);

        var result = await _userService.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_WithWeakPassword_ShouldReturnValidationError()
    {
        var request = new CreateUserRequest("Maria", "maria@corretora.com.br", "123", []);

        var result = await _userService.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_OnSuperAdmin_ShouldKeepAllPermissions()
    {
        var superAdmin = TestData.SuperAdmin();
        _userRepository.GetByIdAsync(superAdmin.Id, Arg.Any<CancellationToken>()).Returns(superAdmin);

        var result = await _userService.UpdateAsync(superAdmin.Id, new UpdateUserRequest("Dona da Corretora", "dona@corretora.com.br", []));

        Assert.True(result.IsSuccess);
        Assert.Equal("Dona da Corretora", result.Value.Name);
        Assert.Equal(User.AllPermissions, superAdmin.Permissions);
    }

    [Fact]
    public async Task DeactivateAsync_OnSuperAdmin_ShouldReturnValidationError()
    {
        var superAdmin = TestData.SuperAdmin();
        _userRepository.GetByIdAsync(superAdmin.Id, Arg.Any<CancellationToken>()).Returns(superAdmin);

        var result = await _userService.DeactivateAsync(superAdmin.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_ShouldListSuperAdminFirst()
    {
        _userRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([TestData.Staff(), TestData.SuperAdmin()]);

        var users = await _userService.GetAllAsync();

        Assert.Equal(UserRole.SuperAdmin, users[0].Role);
    }
}
