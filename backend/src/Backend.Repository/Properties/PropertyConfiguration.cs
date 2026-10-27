using Backend.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Repository.Properties;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    private const int EnumMaxLength = 20;

    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");
        builder.HasKey(property => property.Id);
        builder.Property(property => property.Id).ValueGeneratedNever();
        builder.Property(property => property.Title).IsRequired().HasMaxLength(Property.TitleMaxLength);
        builder.Property(property => property.Description).IsRequired().HasMaxLength(Property.DescriptionMaxLength);
        builder.Property(property => property.Type).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(property => property.Purpose).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(property => property.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(property => property.Price).HasPrecision(18, 2);
        builder.Property(property => property.CondominiumFee).HasPrecision(18, 2);
        builder.Property(property => property.PropertyTax).HasPrecision(18, 2);
        builder.Property(property => property.Area).HasPrecision(12, 2);

        builder.ComplexProperty(property => property.Address, address =>
        {
            address.Property(value => value.Street).IsRequired().HasMaxLength(Address.StreetMaxLength);
            address.Property(value => value.Number).HasMaxLength(Address.NumberMaxLength);
            address.Property(value => value.Complement).HasMaxLength(Address.ComplementMaxLength);
            address.Property(value => value.Neighborhood).IsRequired().HasMaxLength(Address.NeighborhoodMaxLength);
            address.Property(value => value.City).IsRequired().HasMaxLength(Address.CityMaxLength);
            address.Property(value => value.State).IsRequired().HasMaxLength(Address.StateLength).IsFixedLength();
            address.Property(value => value.ZipCode).IsRequired().HasMaxLength(Address.ZipCodeLength).IsFixedLength();
        });

        builder.Ignore(property => property.Images);
        builder.PrimitiveCollection<List<string>>("_images").HasColumnName("images").IsRequired();

        builder.HasIndex(property => new { property.Status, property.CreatedAt });
        builder.HasIndex(property => property.SponsoredUntil);
        builder.Ignore(property => property.DomainEvents);
    }
}
