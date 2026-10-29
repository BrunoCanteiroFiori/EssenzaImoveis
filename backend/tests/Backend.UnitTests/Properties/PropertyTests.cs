using Backend.Domain.Common;
using Backend.Domain.Properties;
using Backend.UnitTests.Common;

namespace Backend.UnitTests.Properties;

public sealed class PropertyTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateAvailableProperty()
    {
        var property = TestData.Property();

        Assert.NotEqual(Guid.Empty, property.Id);
        Assert.Equal("Casa com quintal", property.Title);
        Assert.Equal(PropertyStatus.Available, property.Status);
        Assert.Equal(TestData.Now, property.CreatedAt);
        Assert.True(property.IsPubliclyVisible);
        Assert.Empty(property.Media);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_ShouldThrowDomainException(string title)
    {
        Assert.Throws<DomainException>(() => TestData.Property(title: title));
    }

    [Fact]
    public void Create_WithMessyTitleAndDescription_ShouldStoreNormalizedText()
    {
        var property = Property.Create(
            " Casa\u00A0em  Sa\u0303o Paulo\u0000 ",
            "Sala ampla.\r\n\r\n\r\nCozinha\u200B planejada.  ",
            PropertyType.House,
            PropertyPurpose.Sale,
            100_000m,
            null,
            null,
            100m,
            2,
            1,
            1,
            TestData.Address(),
            TestData.Now);

        Assert.Equal("Casa em São Paulo", property.Title);
        Assert.Equal("Sala ampla.\n\nCozinha planejada.", property.Description);
    }

    [Fact]
    public void Create_WithTitleAboveMaxLength_ShouldThrowDomainException()
    {
        var title = new string('a', Property.TitleMaxLength + 1);

        Assert.Throws<DomainException>(() => TestData.Property(title: title));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithPriceNotPositive_ShouldThrowDomainException(decimal price)
    {
        Assert.Throws<DomainException>(() => Property.Create(
            "Casa", "Descrição", PropertyType.House, PropertyPurpose.Sale, price, null, null, 100m, 2, 1, 1, TestData.Address(), TestData.Now));
    }

    [Fact]
    public void Create_LandWithBedrooms_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Property.Create(
            "Terreno", "Descrição", PropertyType.Land, PropertyPurpose.Sale, 100_000m, null, null, 300m, 2, 0, 0, TestData.Address(), TestData.Now));
    }

    [Fact]
    public void Create_WithZeroCondominiumFee_ShouldStoreNull()
    {
        var property = Property.Create(
            "Casa", "Descrição", PropertyType.House, PropertyPurpose.Sale, 100_000m, 0m, 0m, 100m, 2, 1, 1, TestData.Address(), TestData.Now);

        Assert.Null(property.CondominiumFee);
        Assert.Null(property.PropertyTax);
    }

    [Fact]
    public void Update_WithValidData_ShouldChangePropertyAndUpdatedAt()
    {
        var property = TestData.Property();
        var later = TestData.Now.AddDays(1);

        property.Update(
            "Casa reformada", "Nova descrição", PropertyType.House, PropertyPurpose.Rent, 3_000m, 500m, null, 160m, 4, 3, 2, TestData.Address(), later);

        Assert.Equal("Casa reformada", property.Title);
        Assert.Equal(PropertyPurpose.Rent, property.Purpose);
        Assert.Equal(4, property.Bedrooms);
        Assert.Equal(later, property.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_SoldOnRentProperty_ShouldThrowDomainException()
    {
        var property = TestData.Property(PropertyPurpose.Rent);

        Assert.Throws<DomainException>(() => property.ChangeStatus(PropertyStatus.Sold, TestData.Now));
    }

    [Fact]
    public void ChangeStatus_RentedOnSaleProperty_ShouldThrowDomainException()
    {
        var property = TestData.Property(PropertyPurpose.Sale);

        Assert.Throws<DomainException>(() => property.ChangeStatus(PropertyStatus.Rented, TestData.Now));
    }

    [Fact]
    public void ChangeStatus_ToSold_ShouldEndSponsorshipAndHideProperty()
    {
        var property = TestData.Property();
        property.Sponsor(TestData.Now.AddDays(7), TestData.Now);

        property.ChangeStatus(PropertyStatus.Sold, TestData.Now);

        Assert.Equal(PropertyStatus.Sold, property.Status);
        Assert.Null(property.SponsoredUntil);
        Assert.False(property.IsPubliclyVisible);
    }

    [Fact]
    public void Update_ChangingPurposeOfSoldProperty_ShouldThrowDomainException()
    {
        var property = TestData.Property(PropertyPurpose.Sale);
        property.ChangeStatus(PropertyStatus.Sold, TestData.Now);

        Assert.Throws<DomainException>(() => property.Update(
            "Casa", "Descrição", PropertyType.House, PropertyPurpose.Rent, 3_000m, null, null, 100m, 2, 1, 1, TestData.Address(), TestData.Now));
    }

    [Fact]
    public void Sponsor_WithFutureDate_ShouldBeSponsoredUntilExpiration()
    {
        var property = TestData.Property();
        var until = TestData.Now.AddDays(7);

        property.Sponsor(until, TestData.Now);

        Assert.True(property.IsSponsored(TestData.Now.AddDays(6)));
        Assert.False(property.IsSponsored(TestData.Now.AddDays(8)));
    }

    [Fact]
    public void Sponsor_WithPastDate_ShouldThrowDomainException()
    {
        var property = TestData.Property();

        Assert.Throws<DomainException>(() => property.Sponsor(TestData.Now.AddDays(-1), TestData.Now));
    }

    [Fact]
    public void Sponsor_WhenNotAvailable_ShouldThrowDomainException()
    {
        var property = TestData.Property();
        property.ChangeStatus(PropertyStatus.Inactive, TestData.Now);

        Assert.Throws<DomainException>(() => property.Sponsor(TestData.Now.AddDays(7), TestData.Now));
    }

    [Fact]
    public void RemoveSponsorship_WhenSponsored_ShouldClearSponsorship()
    {
        var property = TestData.Property();
        property.Sponsor(TestData.Now.AddDays(7), TestData.Now);

        property.RemoveSponsorship(TestData.Now);

        Assert.False(property.IsSponsored(TestData.Now));
    }

    [Fact]
    public void AddMedia_WithPhotos_ShouldAppendAndUseFirstAsCover()
    {
        var property = TestData.Property();

        property.AddMedia("foto-1.jpg", TestData.Now);
        property.AddMedia("foto-2.jpg", TestData.Now);

        Assert.Equal(["foto-1.jpg", "foto-2.jpg"], property.Media);
        Assert.Equal("foto-1.jpg", property.CoverImage);
    }

    [Fact]
    public void AddMedia_WithVideos_ShouldInsertAfterLeadingVideosAndBeforePhotos()
    {
        var property = TestData.Property();
        property.AddMedia("foto-1.jpg", TestData.Now);

        property.AddMedia("video-1.mp4", TestData.Now);
        property.AddMedia("video-2.webm", TestData.Now);

        Assert.Equal(["video-1.mp4", "video-2.webm", "foto-1.jpg"], property.Media);
        Assert.Equal("foto-1.jpg", property.CoverImage);
    }

    [Fact]
    public void AddMedia_WithVideoAfterReorderPuttingPhotoFirst_ShouldInsertVideoAtStart()
    {
        var property = TestData.Property();
        property.AddMedia("foto-1.jpg", TestData.Now);
        property.AddMedia("video-1.mp4", TestData.Now);
        property.ReorderMedia(["foto-1.jpg", "video-1.mp4"], TestData.Now);

        property.AddMedia("video-2.mp4", TestData.Now);

        Assert.Equal(["video-2.mp4", "foto-1.jpg", "video-1.mp4"], property.Media);
    }

    [Fact]
    public void AddMedia_WithDuplicateFile_ShouldThrowDomainException()
    {
        var property = TestData.Property();
        property.AddMedia("foto-1.jpg", TestData.Now);

        Assert.Throws<DomainException>(() => property.AddMedia("foto-1.jpg", TestData.Now));
    }

    [Fact]
    public void AddMedia_AboveMaxMedia_ShouldThrowDomainException()
    {
        var property = TestData.Property();
        for (var index = 0; index < Property.MaxMedia; index++)
        {
            property.AddMedia($"foto-{index}.jpg", TestData.Now);
        }

        Assert.Throws<DomainException>(() => property.AddMedia("extra.jpg", TestData.Now));
    }

    [Fact]
    public void AddMedia_AboveMaxVideos_ShouldThrowDomainException()
    {
        var property = TestData.Property();
        for (var index = 0; index < Property.MaxVideos; index++)
        {
            property.AddMedia($"video-{index}.mp4", TestData.Now);
        }

        Assert.Throws<DomainException>(() => property.AddMedia("extra.webm", TestData.Now));
    }

    [Fact]
    public void CoverImage_WithOnlyVideos_ShouldBeNull()
    {
        var property = TestData.Property();

        property.AddMedia("video-1.mp4", TestData.Now);

        Assert.Null(property.CoverImage);
    }

    [Theory]
    [InlineData("foto.jpg", PropertyMediaType.Photo)]
    [InlineData("foto.webp", PropertyMediaType.Photo)]
    [InlineData("video.mp4", PropertyMediaType.Video)]
    [InlineData("VIDEO.WEBM", PropertyMediaType.Video)]
    public void GetMediaType_ShouldUseFileExtension(string fileName, PropertyMediaType expected)
    {
        Assert.Equal(expected, Property.GetMediaType(fileName));
    }

    [Fact]
    public void RemoveMedia_WithUnknownFile_ShouldThrowDomainException()
    {
        var property = TestData.Property();

        Assert.Throws<DomainException>(() => property.RemoveMedia("inexistente.jpg", TestData.Now));
    }

    [Fact]
    public void RemoveMedia_WithExistingFile_ShouldRemoveIt()
    {
        var property = TestData.Property();
        property.AddMedia("foto-1.jpg", TestData.Now);

        property.RemoveMedia("foto-1.jpg", TestData.Now);

        Assert.Empty(property.Media);
        Assert.Null(property.CoverImage);
    }

    [Fact]
    public void ReorderMedia_WithSameFiles_ShouldChangeOrderAndCover()
    {
        var property = TestData.Property();
        property.AddMedia("foto-1.jpg", TestData.Now);
        property.AddMedia("foto-2.jpg", TestData.Now);
        property.AddMedia("video-1.mp4", TestData.Now);

        property.ReorderMedia(["foto-2.jpg", "video-1.mp4", "foto-1.jpg"], TestData.Now);

        Assert.Equal(["foto-2.jpg", "video-1.mp4", "foto-1.jpg"], property.Media);
        Assert.Equal("foto-2.jpg", property.CoverImage);
    }

    [Fact]
    public void ReorderMedia_WithDifferentFiles_ShouldThrowDomainException()
    {
        var property = TestData.Property();
        property.AddMedia("foto-1.jpg", TestData.Now);
        property.AddMedia("foto-2.jpg", TestData.Now);

        Assert.Throws<DomainException>(() => property.ReorderMedia(["foto-1.jpg", "outra.jpg"], TestData.Now));
    }
}
