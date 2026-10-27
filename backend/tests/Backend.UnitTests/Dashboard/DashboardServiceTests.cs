using Backend.Application.Dashboard;
using Backend.Domain.ContactMessages;
using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.Dashboard;

public sealed class DashboardServiceTests
{
    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly IPropertyViewRepository _propertyViewRepository = Substitute.For<IPropertyViewRepository>();
    private readonly IContactMessageRepository _contactMessageRepository = Substitute.For<IContactMessageRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _dashboardService = new DashboardService(_propertyRepository, _propertyViewRepository, _contactMessageRepository, _timeProvider);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldAggregateCounts()
    {
        var hottest = TestData.Property();
        _propertyRepository.CountByStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<PropertyStatus, int> { [PropertyStatus.Available] = 7, [PropertyStatus.Sold] = 2 });
        _propertyRepository.CountSponsoredAsync(TestData.Now, Arg.Any<CancellationToken>()).Returns(3);
        _contactMessageRepository.CountByStatusAsync(ContactMessageStatus.New, Arg.Any<CancellationToken>()).Returns(4);
        _propertyViewRepository.CountSinceAsync(TestData.Now - PropertyTemperature.Window, Arg.Any<CancellationToken>()).Returns(120);
        _propertyRepository.GetHottestAsync(Arg.Any<DateTimeOffset>(), DashboardService.HottestLimit, Arg.Any<CancellationToken>()).Returns([hottest]);
        _propertyViewRepository
            .CountByPropertySinceAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [hottest.Id] = 40 });

        var summary = await _dashboardService.GetSummaryAsync();

        Assert.Equal(7, summary.AvailableProperties);
        Assert.Equal(2, summary.SoldProperties);
        Assert.Equal(0, summary.RentedProperties);
        Assert.Equal(3, summary.SponsoredProperties);
        Assert.Equal(4, summary.NewMessages);
        Assert.Equal(120, summary.RecentViews);
        Assert.Equal(TemperatureLevel.Hot, Assert.Single(summary.HottestProperties).Temperature.Level);
    }
}
