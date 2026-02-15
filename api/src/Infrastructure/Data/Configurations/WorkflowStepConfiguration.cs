using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("WorkflowSteps");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.RecruitmentId)
            .HasDatabaseName("IX_WorkflowSteps_RecruitmentId");

        builder.HasIndex(s => new { s.RecruitmentId, s.Name })
            .IsUnique()
            .HasDatabaseName("UQ_WorkflowSteps_RecruitmentId_Name");

        builder.Ignore(s => s.DomainEvents);
    }
}
