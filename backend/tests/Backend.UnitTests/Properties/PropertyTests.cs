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
        Assert.Empty(property.Images);
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
    public void AddImage_WithNewImage_ShouldBecomeCoverWhenFirst()
    {
        var property = TestData.Property();

        property.AddImage("foto-1.jpg", TestData.Now);
        property.AddImage("foto-2.jpg", TestData.Now);

        Assert.Equal(["foto-1.jpg", "foto-2.jpg"], property.Images);
        Assert.Equal("foto-1.jpg", property.CoverImage);
    }

    [Fact]
    public void AddImage_WithDuplicateImage_ShouldThrowDomainException()
    {
        var property = TestData.Property();
        property.AddImage("foto-1.jpg", TestData.Now);

        Assert.Throws<DomainException>(() => property.AddImage("foto-1.jpg", TestData.Now));
    }

    [Fact]
    public void AddImage_AboveMaxImages_ShouldThrowDomainException()
    {
        var property = TestData.Property();
        for (var index = 0; index < Property.MaxImages; index++)
        {
            property.AddImage($"foto-{index}.jpg", TestData.Now);
        }

        Assert.Throws<DomainException>(() => property.AddImage("extra.jpg", TestData.Now));
    }

    [Fact]
    public void RemoveImage_WithUnknownImage_ShouldThrowDomainException()
    {
        var property = TestData.Property();

        Assert.Throws<DomainException>(() => property.RemoveImage("inexistente.jpg", TestData.Now));
    }

    [Fact]
    public void RemoveImage_WithExistingImage_ShouldRemoveIt()
    {
        var property = TestData.Property();
        property.AddImage("foto-1.jpg", TestData.Now);

        property.RemoveImage("foto-1.jpg", TestData.Now);

        Assert.Empty(property.Images);
        Assert.Null(property.CoverImage);
    }

    [Fact]
    public void ReorderImages_WithSameImages_ShouldChangeCover()
    {
        var property = TestData.Property();
        property.AddImage("foto-1.jpg", TestData.Now);
        property.AddImage("foto-2.jpg", TestData.Now);

        property.ReorderImages(["foto-2.jpg", "foto-1.jpg"], TestData.Now);

        Assert.Equal("foto-2.jpg", property.CoverImage);
    }

    [Fact]
    public void ReorderImages_WithDifferentImages_ShouldThrowDomainException()
    {
        var property = TestData.Property();
        property.AddImage("foto-1.jpg", TestData.Now);
        property.AddImage("foto-2.jpg", TestData.Now);

        Assert.Throws<DomainException>(() => property.ReorderImages(["foto-1.jpg", "outra.jpg"], TestData.Now));
    }
}
