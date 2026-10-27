using Backend.Domain.Common;
using Backend.Domain.ContactMessages;
using Backend.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Repository.ContactMessages;

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("contact_messages");
        builder.HasKey(contactMessage => contactMessage.Id);
        builder.Property(contactMessage => contactMessage.Id).ValueGeneratedNever();
        builder.Property(contactMessage => contactMessage.Name).IsRequired().HasMaxLength(ContactMessage.NameMaxLength);
        builder.Property(contactMessage => contactMessage.Email).HasMaxLength(EmailAddress.MaxLength);
        builder.Property(contactMessage => contactMessage.Phone).HasMaxLength(ContactMessage.PhoneMaxDigits);
        builder.Property(contactMessage => contactMessage.Message).IsRequired().HasMaxLength(ContactMessage.MessageMaxLength);
        builder.Property(contactMessage => contactMessage.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(contactMessage => contactMessage.InternalNotes).HasMaxLength(ContactMessage.InternalNotesMaxLength);
        builder.HasOne<Property>().WithMany().HasForeignKey(contactMessage => contactMessage.PropertyId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(contactMessage => new { contactMessage.Status, contactMessage.CreatedAt });
        builder.Ignore(contactMessage => contactMessage.DomainEvents);
    }
}
