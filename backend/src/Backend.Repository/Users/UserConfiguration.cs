using Backend.Domain.Common;
using Backend.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Repository.Users;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Name).IsRequired().HasMaxLength(User.NameMaxLength);
        builder.Property(user => user.Email).IsRequired().HasMaxLength(EmailAddress.MaxLength);
        builder.Property(user => user.PasswordHash).IsRequired().HasMaxLength(User.PasswordHashMaxLength);
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(user => user.Email).IsUnique();

        // Garante no banco que existe no máximo um super admin.
        builder.HasIndex(user => user.Role).IsUnique().HasFilter($"role = '{nameof(UserRole.SuperAdmin)}'");

        builder.Ignore(user => user.DomainEvents);
    }
}
