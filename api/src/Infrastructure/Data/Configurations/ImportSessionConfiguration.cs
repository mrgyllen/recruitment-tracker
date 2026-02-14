using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class ImportSessionConfiguration : IEntityTypeConfiguration<ImportSession>
{
    public void Configure(EntityTypeBuilder<ImportSession> builder)
    {
        builder.ToTable("ImportSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.SourceFileName)
            .HasMaxLength(500);

        builder.Property(s => s.FailureReason)
            .HasMaxLength(2000);

        builder.HasIndex(s => s.RecruitmentId)
            .HasDatabaseName("IX_ImportSessions_RecruitmentId");

        builder.OwnsMany(s => s.RowResults, rowBuilder =>
        {
            rowBuilder.ToJson();
        });

        builder.Property(s => s.OriginalBundleBlobUrl)
            .HasMaxLength(2048);

        builder.HasMany(s => s.ImportDocuments)
            .WithOne()
            .HasForeignKey(d => d.ImportSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.ImportDocuments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.DomainEvents);
    }
}
