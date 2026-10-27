using Backend.Domain.Common;
using Backend.Domain.Users;
using Backend.UnitTests.Common;

namespace Backend.UnitTests.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateActiveStaff()
    {
        var user = User.Create(" Maria ", " Maria@Corretora.com.br ", TestData.PasswordHash, UserPermissions.ManageMessages, TestData.Now);

        Assert.Equal("Maria", user.Name);
        Assert.Equal("maria@corretora.com.br", user.Email);
        Assert.Equal(UserRole.Staff, user.Role);
        Assert.True(user.IsActive);
        Assert.False(user.HasPermission(UserPermissions.ReadProperties));
    }

    [Fact]
    public void Create_WithWritePermission_ShouldAlsoGrantRead()
    {
        var user = TestData.Staff(UserPermissions.WriteProperties);

        Assert.True(user.HasPermission(UserPermissions.ReadProperties));
        Assert.True(user.HasPermission(UserPermissions.WriteProperties));
        Assert.False(user.HasPermission(UserPermissions.DeleteProperties));
    }

    [Fact]
    public void Create_WithUnknownPermission_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => TestData.Staff((UserPermissions)64));
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => User.Create("Maria", "maria@", TestData.PasswordHash, UserPermissions.None, TestData.Now));
    }

    [Fact]
    public void CreateSuperAdmin_ShouldHaveAllPermissions()
    {
        var user = TestData.SuperAdmin();

        Assert.True(user.IsSuperAdmin);
        Assert.Equal(User.AllPermissions, user.Permissions);
    }

    [Fact]
    public void ChangePermissions_OnSuperAdmin_ShouldThrowDomainException()
    {
        var user = TestData.SuperAdmin();

        Assert.Throws<DomainException>(() => user.ChangePermissions(UserPermissions.ReadProperties, TestData.Now));
    }

    [Fact]
    public void Deactivate_OnSuperAdmin_ShouldThrowDomainException()
    {
        var user = TestData.SuperAdmin();

        Assert.Throws<DomainException>(() => user.Deactivate(TestData.Now));
    }

    [Fact]
    public void Deactivate_OnStaff_ShouldBlockSignIn()
    {
        var user = TestData.Staff();

        user.Deactivate(TestData.Now);

        Assert.False(user.IsActive);
        Assert.False(user.CanSignIn(TestData.Now));
    }

    [Fact]
    public void RegisterFailedSignIn_AtMaxAttempts_ShouldLockOut()
    {
        var user = TestData.Staff();

        for (var attempt = 0; attempt < User.MaxFailedSignInAttempts; attempt++)
        {
            user.RegisterFailedSignIn(TestData.Now);
        }

        Assert.True(user.IsLockedOut(TestData.Now));
        Assert.False(user.IsLockedOut(TestData.Now.Add(User.LockoutDuration).AddSeconds(1)));
    }

    [Fact]
    public void RegisterSuccessfulSignIn_ShouldResetAttemptsAndSetLastSignIn()
    {
        var user = TestData.Staff();
        user.RegisterFailedSignIn(TestData.Now);

        user.RegisterSuccessfulSignIn(TestData.Now);

        Assert.Equal(0, user.FailedSignInAttempts);
        Assert.Equal(TestData.Now, user.LastSignInAt);
    }

    [Fact]
    public void ChangePasswordHash_ShouldRejectSessionsCreatedBefore()
    {
        var user = TestData.Staff();
        var changedAt = TestData.Now.AddDays(1);

        user.ChangePasswordHash("novo-hash", changedAt);

        Assert.Equal("novo-hash", user.PasswordHash);
        Assert.False(user.AcceptsSessionCreatedAt(TestData.Now));
        Assert.True(user.AcceptsSessionCreatedAt(changedAt));
    }
}
