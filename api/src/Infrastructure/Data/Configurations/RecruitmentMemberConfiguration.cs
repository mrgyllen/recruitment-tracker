using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class RecruitmentMemberConfiguration : IEntityTypeConfiguration<RecruitmentMember>
{
    public void Configure(EntityTypeBuilder<RecruitmentMember> builder)
    {
        builder.ToTable("RecruitmentMembers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("IX_RecruitmentMembers_UserId");

        builder.HasIndex(m => new { m.RecruitmentId, m.UserId })
            .IsUnique()
            .HasDatabaseName("UQ_RecruitmentMembers_RecruitmentId_UserId");

        builder.Ignore(m => m.DomainEvents);
    }
}
