using Backend.Domain.Common;
using Backend.Domain.PropertyViews;
using Backend.Domain.SavedProperties;

namespace Backend.UnitTests.SavedProperties;

public sealed class SavedPropertyTests
{
    private static readonly string _visitorHash = new('a', PropertyView.VisitorHashLength);

    [Fact]
    public void Create_WithValidData_ShouldUseUtc()
    {
        var propertyId = Guid.CreateVersion7();
        var savedAt = new DateTimeOffset(2026, 1, 1, 22, 30, 0, TimeSpan.FromHours(-3));

        var savedProperty = SavedProperty.Create(propertyId, _visitorHash, savedAt);

        Assert.Equal(propertyId, savedProperty.PropertyId);
        Assert.Equal(_visitorHash, savedProperty.VisitorHash);
        Assert.Equal(savedAt, savedProperty.SavedAt);
        Assert.Equal(TimeSpan.Zero, savedProperty.SavedAt.Offset);
    }

    [Fact]
    public void Create_WithEmptyPropertyId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => SavedProperty.Create(Guid.Empty, _visitorHash, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("curto")]
    public void Create_WithInvalidVisitorHash_ShouldThrowDomainException(string visitorHash)
    {
        Assert.Throws<DomainException>(() => SavedProperty.Create(Guid.CreateVersion7(), visitorHash, DateTimeOffset.UtcNow));
    }
}
