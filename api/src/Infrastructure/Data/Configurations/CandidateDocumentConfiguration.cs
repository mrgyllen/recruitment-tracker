using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class CandidateDocumentConfiguration : IEntityTypeConfiguration<CandidateDocument>
{
    public void Configure(EntityTypeBuilder<CandidateDocument> builder)
    {
        builder.ToTable("CandidateDocuments");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.BlobStorageUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(d => d.WorkdayCandidateId)
            .HasMaxLength(50);

        builder.Property(d => d.DocumentSource)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(d => d.CandidateId)
            .HasDatabaseName("IX_CandidateDocuments_CandidateId");

        builder.Ignore(d => d.DomainEvents);
    }
}
