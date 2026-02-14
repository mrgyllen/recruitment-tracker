using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class ImportDocumentConfiguration : IEntityTypeConfiguration<ImportDocument>
{
    public void Configure(EntityTypeBuilder<ImportDocument> builder)
    {
        builder.ToTable("ImportDocuments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.CandidateName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.BlobStorageUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(d => d.WorkdayCandidateId)
            .HasMaxLength(50);

        builder.Property(d => d.MatchStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(d => d.ImportSessionId)
            .HasDatabaseName("IX_ImportDocuments_ImportSessionId");

        builder.Ignore(d => d.DomainEvents);
    }
}
