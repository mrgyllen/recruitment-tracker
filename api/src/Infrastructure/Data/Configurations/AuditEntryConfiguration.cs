using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.ActionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Context)
            .HasMaxLength(4000);

        builder.HasIndex(a => new { a.RecruitmentId, a.PerformedAt })
            .HasDatabaseName("IX_AuditEntries_RecruitmentId_PerformedAt");

        builder.Ignore(a => a.DomainEvents);
    }
}
