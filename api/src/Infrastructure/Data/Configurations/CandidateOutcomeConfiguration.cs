using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class CandidateOutcomeConfiguration : IEntityTypeConfiguration<CandidateOutcome>
{
    public void Configure(EntityTypeBuilder<CandidateOutcome> builder)
    {
        builder.ToTable("CandidateOutcomes");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(o => new { o.CandidateId, o.WorkflowStepId })
            .HasDatabaseName("IX_CandidateOutcomes_CandidateId_WorkflowStepId");

        builder.Ignore(o => o.DomainEvents);
    }
}
