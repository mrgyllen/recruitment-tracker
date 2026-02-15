using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("Candidates");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.FullName)
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .HasMaxLength(254);

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(c => c.Location)
            .HasMaxLength(200);

        // Relationship to Recruitment (no navigation property on Candidate — cross-aggregate reference uses ID only)
        builder.HasOne<Recruitment>()
            .WithMany()
            .HasForeignKey(c => c.RecruitmentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(c => c.RecruitmentId)
            .HasDatabaseName("IX_Candidates_RecruitmentId");

        builder.HasIndex(c => new { c.RecruitmentId, c.Email })
            .IsUnique()
            .HasDatabaseName("UQ_Candidates_RecruitmentId_Email")
            .HasFilter("[Email] IS NOT NULL"); // Allow multiple anonymized candidates

        builder.Property(c => c.CurrentWorkflowStepId);
        builder.Property(c => c.IsCompleted).HasDefaultValue(false);

        builder.HasMany(c => c.Outcomes)
            .WithOne()
            .HasForeignKey(o => o.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Documents)
            .WithOne()
            .HasForeignKey(d => d.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.DomainEvents);

        builder.Navigation(c => c.Outcomes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
