using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;
using Backend.Domain.SavedProperties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Repository.SavedProperties;

public sealed class SavedPropertyConfiguration : IEntityTypeConfiguration<SavedProperty>
{
    public void Configure(EntityTypeBuilder<SavedProperty> builder)
    {
        builder.ToTable("saved_properties");
        builder.HasKey(savedProperty => savedProperty.Id);
        builder.Property(savedProperty => savedProperty.Id).ValueGeneratedNever();
        builder.Property(savedProperty => savedProperty.VisitorHash).IsRequired().HasMaxLength(PropertyView.VisitorHashLength).IsFixedLength();
        builder.HasOne<Property>().WithMany().HasForeignKey(savedProperty => savedProperty.PropertyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(savedProperty => new { savedProperty.VisitorHash, savedProperty.PropertyId }).IsUnique();
        builder.HasIndex(savedProperty => savedProperty.PropertyId);
        builder.Ignore(savedProperty => savedProperty.DomainEvents);
    }
}
