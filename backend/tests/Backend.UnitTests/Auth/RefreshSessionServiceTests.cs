using Backend.Application.Auth;
using Backend.Domain.Abstractions;
using Backend.Domain.Auth;
using Backend.Infrastructure.Conversions;
using Backend.Infrastructure.Security;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.Auth;

public sealed class RefreshSessionServiceTests
{
    private const string Token = "token-aleatorio";

    private readonly IRefreshSessionRepository _refreshSessionRepository = Substitute.For<IRefreshSessionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISecureTokenGenerator _secureTokenGenerator = Substitute.For<ISecureTokenGenerator>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly RefreshSessionService _refreshSessionService;

    public RefreshSessionServiceTests()
    {
        _secureTokenGenerator.Generate(Arg.Any<int>()).Returns(Token);
        _refreshSessionService = new RefreshSessionService(_refreshSessionRepository, _unitOfWork, _secureTokenGenerator, _timeProvider);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistOnlyTokenHash()
    {
        var userId = Guid.CreateVersion7();

        var issued = await _refreshSessionService.CreateAsync(userId);

        Assert.Equal(Token, issued.Token);
        Assert.Equal(TestData.Now.Add(RefreshSession.Lifetime), issued.ExpiresAt);
        await _refreshSessionRepository.Received(1).AddAsync(
            Arg.Is<RefreshSession>(session => session.UserId == userId && session.TokenHash == Token.ToSha256Hex()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_WithUnknownToken_ShouldReturnInvalidSession()
    {
        var result = await _refreshSessionService.ValidateAsync("desconhecido");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidSession(), result.Error);
    }

    [Fact]
    public async Task ValidateAsync_WithActiveSession_ShouldReturnSession()
    {
        var session = RefreshSession.Create(Guid.CreateVersion7(), Token.ToSha256Hex(), TestData.Now);
        _refreshSessionRepository.GetByTokenHashAsync(Token.ToSha256Hex(), Arg.Any<CancellationToken>()).Returns(session);

        var result = await _refreshSessionService.ValidateAsync(Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(session, result.Value);
    }

    [Fact]
    public async Task ValidateAsync_WithReusedRevokedToken_ShouldRevokeAllUserSessions()
    {
        var userId = Guid.CreateVersion7();
        var reused = RefreshSession.Create(userId, Token.ToSha256Hex(), TestData.Now);
        reused.Revoke(TestData.Now);
        var other = RefreshSession.Create(userId, new string('c', RefreshSession.TokenHashLength), TestData.Now);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));
        _refreshSessionRepository.GetByTokenHashAsync(Token.ToSha256Hex(), Arg.Any<CancellationToken>()).Returns(reused);
        _refreshSessionRepository.GetActiveByUserIdAsync(userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([other]);

        var result = await _refreshSessionService.ValidateAsync(Token);

        Assert.True(result.IsFailure);
        Assert.True(other.IsRevoked);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateAsync_ShouldRevokeCurrentAndCreateNewSession()
    {
        var session = RefreshSession.Create(Guid.CreateVersion7(), new string('d', RefreshSession.TokenHashLength), TestData.Now);

        var issued = await _refreshSessionService.RotateAsync(session);

        Assert.True(session.IsRevoked);
        Assert.Equal(Token, issued.Token);
        await _refreshSessionRepository.Received(1).AddAsync(Arg.Any<RefreshSession>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeAsync_WithActiveSession_ShouldRevoke()
    {
        var session = RefreshSession.Create(Guid.CreateVersion7(), Token.ToSha256Hex(), TestData.Now);
        _refreshSessionRepository.GetByTokenHashAsync(Token.ToSha256Hex(), Arg.Any<CancellationToken>()).Returns(session);

        await _refreshSessionService.RevokeAsync(Token);

        Assert.True(session.IsRevoked);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
