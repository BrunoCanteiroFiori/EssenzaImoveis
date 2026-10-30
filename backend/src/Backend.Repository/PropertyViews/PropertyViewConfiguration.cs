using Backend.Domain.Properties;
using Backend.Domain.PropertyViews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Repository.PropertyViews;

public sealed class PropertyViewConfiguration : IEntityTypeConfiguration<PropertyView>
{
    public void Configure(EntityTypeBuilder<PropertyView> builder)
    {
        builder.ToTable("property_views");
        builder.HasKey(view => view.Id);
        builder.Property(view => view.Id).ValueGeneratedNever();
        builder.Property(view => view.VisitorHash).IsRequired().HasMaxLength(PropertyView.VisitorHashLength).IsFixedLength();
        builder.HasOne<Property>().WithMany().HasForeignKey(view => view.PropertyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(view => new { view.PropertyId, view.VisitorHash, view.ViewedOn }).IsUnique();
        builder.HasIndex(view => new { view.PropertyId, view.ViewedAt });
        builder.HasIndex(view => new { view.VisitorHash, view.ViewedAt });
        builder.Ignore(view => view.DomainEvents);
    }
}
