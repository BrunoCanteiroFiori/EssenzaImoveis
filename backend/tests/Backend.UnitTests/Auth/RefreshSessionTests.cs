using Backend.Domain.Auth;
using Backend.Domain.Common;
using Backend.UnitTests.Common;

namespace Backend.UnitTests.Auth;

public sealed class RefreshSessionTests
{
    private static readonly string _tokenHash = new('b', RefreshSession.TokenHashLength);

    [Fact]
    public void Create_WithValidData_ShouldExpireAfterLifetime()
    {
        var session = RefreshSession.Create(Guid.CreateVersion7(), _tokenHash, TestData.Now);

        Assert.Equal(TestData.Now.Add(RefreshSession.Lifetime), session.ExpiresAt);
        Assert.True(session.IsActive(TestData.Now));
        Assert.False(session.IsActive(session.ExpiresAt));
    }

    [Fact]
    public void Create_WithInvalidTokenHash_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => RefreshSession.Create(Guid.CreateVersion7(), "curto", TestData.Now));
    }

    [Fact]
    public void Revoke_Twice_ShouldKeepFirstRevocation()
    {
        var session = RefreshSession.Create(Guid.CreateVersion7(), _tokenHash, TestData.Now);

        session.Revoke(TestData.Now);
        session.Revoke(TestData.Now.AddHours(1));

        Assert.Equal(TestData.Now, session.RevokedAt);
        Assert.False(session.IsActive(TestData.Now));
    }

    [Fact]
    public void IsSuspiciousReuse_ShouldOnlyBeTrueAfterGracePeriod()
    {
        var session = RefreshSession.Create(Guid.CreateVersion7(), _tokenHash, TestData.Now);
        session.Revoke(TestData.Now);

        Assert.False(session.IsSuspiciousReuse(TestData.Now.AddSeconds(5)));
        Assert.True(session.IsSuspiciousReuse(TestData.Now.Add(RefreshSession.ReuseGracePeriod).AddSeconds(1)));
    }
}
