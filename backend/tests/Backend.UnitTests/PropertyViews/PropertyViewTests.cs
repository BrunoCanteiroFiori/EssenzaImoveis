using Backend.Domain.Common;
using Backend.Domain.PropertyViews;

namespace Backend.UnitTests.PropertyViews;

public sealed class PropertyViewTests
{
    private static readonly string _visitorHash = new('a', PropertyView.VisitorHashLength);

    [Fact]
    public void Create_WithValidData_ShouldUseUtcDay()
    {
        var propertyId = Guid.CreateVersion7();
        var viewedAt = new DateTimeOffset(2026, 1, 1, 22, 30, 0, TimeSpan.FromHours(-3));

        var view = PropertyView.Create(propertyId, _visitorHash, viewedAt);

        Assert.Equal(propertyId, view.PropertyId);
        Assert.Equal(new DateOnly(2026, 1, 2), view.ViewedOn);
        Assert.Equal(TimeSpan.Zero, view.ViewedAt.Offset);
    }

    [Fact]
    public void Create_WithEmptyPropertyId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => PropertyView.Create(Guid.Empty, _visitorHash, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("curto")]
    public void Create_WithInvalidVisitorHash_ShouldThrowDomainException(string visitorHash)
    {
        Assert.Throws<DomainException>(() => PropertyView.Create(Guid.CreateVersion7(), visitorHash, DateTimeOffset.UtcNow));
    }
}
