using Backend.Domain.AuditLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Repository.AuditLogs;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(auditLog => auditLog.Id);
        builder.Property(auditLog => auditLog.Id).ValueGeneratedNever();
        builder.Property(auditLog => auditLog.EntityName).IsRequired().HasMaxLength(AuditLog.EntityNameMaxLength);
        builder.Property(auditLog => auditLog.EntityId).IsRequired().HasMaxLength(AuditLog.EntityIdMaxLength);
        builder.Property(auditLog => auditLog.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(auditLog => auditLog.Changes).IsRequired().HasColumnType("jsonb");
        builder.Property(auditLog => auditLog.UserEmail).HasMaxLength(200);
        builder.HasIndex(auditLog => auditLog.OccurredAt);
        builder.HasIndex(auditLog => new { auditLog.EntityName, auditLog.EntityId });
        builder.HasIndex(auditLog => auditLog.UserId);
        builder.Ignore(auditLog => auditLog.DomainEvents);
    }
}
